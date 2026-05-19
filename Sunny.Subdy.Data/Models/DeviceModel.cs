using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using static Sunny.Subdy.Data.AppDbContext;

namespace Sunny.Subdy.Data.Models
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public class DeviceModel : INotifyPropertyChanged, IThrottledNotify
    {
        [SqlKey]
        public int Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged(nameof(Id));
                }
            }
        }

        private string _status;
        private string _os;
        private string _name;
        private bool _check;
        private int _color;
        private int _id;
        private string _serial;
        private bool _isScrcpy;
        private bool _isControl;
        private string _state;
        private string _model;
        private string _nameFolder;
        private bool _isSelectControl;
        private bool _isLive;
        private bool _isAdbOnline;
        private bool _hasInternet = false; // default false: row khoá cho tới khi DeviceHealthCheckService probe xong + xác nhận có mạng
        private bool _isRowEnabled = false; // UI: false → row disabled (xám + read-only). Mặc định khoá, chỉ mở khi IsLive && HasInternet
        private int _index;

        // Cache PropertyChangedEventArgs to avoid allocations
        private static readonly Dictionary<string, PropertyChangedEventArgs> _eventArgsCache = new()
    {
        { nameof(Id), new PropertyChangedEventArgs(nameof(Id)) },
        { nameof(Status), new PropertyChangedEventArgs(nameof(Status)) },
        { nameof(State), new PropertyChangedEventArgs(nameof(State)) },
        { nameof(Serial), new PropertyChangedEventArgs(nameof(Serial)) },
        { nameof(OS), new PropertyChangedEventArgs(nameof(OS)) },
        { nameof(NameDevice), new PropertyChangedEventArgs(nameof(NameDevice)) },
        { nameof(Checked), new PropertyChangedEventArgs(nameof(Checked)) },
        { nameof(TypeColor), new PropertyChangedEventArgs(nameof(TypeColor)) },
        { nameof(IsScrcpy), new PropertyChangedEventArgs(nameof(IsScrcpy)) },
        { nameof(IsControl), new PropertyChangedEventArgs(nameof(IsControl)) },
        { nameof(IsSelectControl), new PropertyChangedEventArgs(nameof(IsSelectControl)) },
        { nameof(Model), new PropertyChangedEventArgs(nameof(Model)) },
        { nameof(NameFolder), new PropertyChangedEventArgs(nameof(NameFolder)) },
        { nameof(IsLive), new PropertyChangedEventArgs(nameof(IsLive)) },
        { nameof(IsAdbOnline), new PropertyChangedEventArgs(nameof(IsAdbOnline)) },
        { nameof(HasInternet), new PropertyChangedEventArgs(nameof(HasInternet)) },
        { nameof(IsRowEnabled), new PropertyChangedEventArgs(nameof(IsRowEnabled)) },
        { nameof(Index), new PropertyChangedEventArgs(nameof(Index)) }
    };

        public DeviceModel()
        {
        }

        public string Model
        {
            get => _model;
            set
            {
                if (_model != value)
                {
                    _model = value;
                    OnPropertyChanged(nameof(Model));
                }
            }
        }

        [NotMapped]
        public int RotationAngle { get; set; } = 0;

        [NotMapped]
        public bool IsControl
        {
            get => _isControl;
            set
            {
                if (_isControl != value)
                {
                    _isControl = value;
                    OnPropertyChanged(nameof(IsControl));
                }
            }
        }

        [NotMapped]
        public bool IsSelectControl
        {
            get => _isSelectControl;
            set
            {
                if (_isSelectControl != value)
                {
                    _isSelectControl = value;
                    OnPropertyChanged(nameof(IsSelectControl));
                }
            }
        }

        public int Port { get; set; }

        public bool IsScrcpy
        {
            get => _isScrcpy;
            set
            {
                if (_isScrcpy != value)
                {
                    _isScrcpy = value;
                    OnPropertyChanged(nameof(IsScrcpy));
                }
            }
        }

        public string NameFolder
        {
            get => _nameFolder;
            set
            {
                if (_nameFolder != value)
                {
                    _nameFolder = value;
                    OnPropertyChanged(nameof(NameFolder));
                }
            }
        }

        public string State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged(nameof(State));
                }
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public string Serial
        {
            get => _serial;
            set
            {
                if (_serial != value)
                {
                    _serial = value;
                    OnPropertyChanged(nameof(Serial));
                }
            }
        }

        public string OS
        {
            get => _os;
            set
            {
                if (_os != value)
                {
                    _os = value;
                    OnPropertyChanged(nameof(OS));
                }
            }
        }

        public string NameDevice
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(NameDevice));
                }
            }
        }

        public bool Checked
        {
            get => _check;
            set
            {
                if (_check != value)
                {
                    _check = value;
                    OnPropertyChanged(nameof(Checked));
                }
            }
        }

        /// <summary>IsLive = ATX/Appium connected successfully</summary>
        [NotMapped]
        public bool IsLive
        {
            get => _isLive;
            set
            {
                if (_isLive != value)
                {
                    _isLive = value;
                    OnPropertyChanged(nameof(IsLive));
                    RecomputeRowEnabled();
                }
            }
        }

        /// <summary>IsAdbOnline = device visible in 'adb devices'. Không tính vào IsRowEnabled.</summary>
        [NotMapped]
        public bool IsAdbOnline
        {
            get => _isAdbOnline;
            set
            {
                if (_isAdbOnline != value)
                {
                    _isAdbOnline = value;
                    OnPropertyChanged(nameof(IsAdbOnline));
                }
            }
        }

        /// <summary>HasInternet = device ping 8.8.8.8 / google.com thành công (DeviceHealthCheckService).</summary>
        [NotMapped]
        public bool HasInternet
        {
            get => _hasInternet;
            set
            {
                if (_hasInternet != value)
                {
                    _hasInternet = value;
                    OnPropertyChanged(nameof(HasInternet));
                    RecomputeRowEnabled();
                }
            }
        }

        // IsRowEnabled = chỉ bật khi cả Live (ATX/Appium connected) + HasInternet (ping ok).
        // ADB online không tính vào điều kiện vì IsLive đã yêu cầu ATX up trên thiết bị
        // (kéo theo ADB phải online). Tự recompute khi 1 trong 2 nguồn đổi để UI/checkbox
        // luôn nhất quán mà không cần caller nhớ gọi lại bằng tay sau mỗi probe nền.
        private void RecomputeRowEnabled()
        {
            IsRowEnabled = _isLive && _hasInternet;
        }

        /// <summary>
        /// IsRowEnabled = false khi device không đạt điều kiện (mất internet hoặc ATX fail).
        /// UI dùng để vẽ row mờ + đặt ReadOnly để user không tick checkbox.
        /// </summary>
        [NotMapped]
        public bool IsRowEnabled
        {
            get => _isRowEnabled;
            set
            {
                if (_isRowEnabled != value)
                {
                    _isRowEnabled = value;
                    OnPropertyChanged(nameof(IsRowEnabled));
                }
            }
        }

        [NotMapped]
        public int Index
        {
            get => _index;
            set
            {
                if (_index != value)
                {
                    _index = value;
                    OnPropertyChanged(nameof(Index));
                }
            }
        }

        [NotMapped]
        public int TypeColor
        {
            get => _color;
            set
            {
                if (_color != value)
                {
                    _color = value;
                    OnPropertyChanged(nameof(TypeColor));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            if (!_eventArgsCache.TryGetValue(propertyName, out var eventArgs))
            {
                eventArgs = new PropertyChangedEventArgs(propertyName);
            }
            PropertyChanged?.Invoke(this, eventArgs);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            // Throttled: just mark dirty, the shared timer will fire the event on UI thread
            ThrottledPropertyNotifier.MarkDirty(this, propertyName);
        }
    }
}
