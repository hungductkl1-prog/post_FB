using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace AutoAndroid
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public class DeviceModel : INotifyPropertyChanged
    {
        private string _status;
        private string _os;
        private string _name;
        private bool _check;
        private int _color;
        private int _index;
        private string _serial;
        private bool _isScrcpy;

        public int Port { get; set; }
        public int PortScrcpy { get; set; }

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
