using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using static Sunny.Subdy.Data.AppDbContext;

namespace Sunny.Subdy.Data.Models
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public class DeviceModel : INotifyPropertyChanged
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

        protected void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler == null) return;

            var context = SynchronizationContext.Current;
            if (context != null)
            {
                context.Post(_ => handler(this, new PropertyChangedEventArgs(propertyName)), null);
            }
            else
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
