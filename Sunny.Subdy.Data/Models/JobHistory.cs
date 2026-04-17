namespace Sunny.Subdy.Data.Models
{
    using System;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Threading;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public class JobHistory : INotifyPropertyChanged, IThrottledNotify
    {
        private Guid _id = Guid.NewGuid();
        private string? _uid = string.Empty;
        private string? _platform = string.Empty;
        private string? _method = string.Empty;
        private string? _description = string.Empty;
        private string? _status = string.Empty;
        private string? _idJob = string.Empty;
        private string? _idObject =string.Empty;
        private string? _coin = string.Empty;
        private string? _service = string.Empty;

        private string? _dateTime = string.Empty;

        [NotMapped]
        public int STT { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            if (propertyName == null) return;
            ThrottledPropertyNotifier.MarkDirty(this, propertyName);
        }

        public Guid Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string? Uid
        {
            get => _uid;
            set { _uid = value; OnPropertyChanged(); }
        }
        public string? Platform
        {
            get => _platform;
            set { _platform = value; OnPropertyChanged(); }
        }
        public string? Method
        {
            get => _method;
            set { _method = value; OnPropertyChanged(); }
        }
        public string? Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }
        public string? Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }
        public string? IdJob
        {
            get => _idJob;
            set { _idJob = value; OnPropertyChanged(); }
        }
        public string? IdObject
        {
            get => _idObject;
            set { _idObject = value; OnPropertyChanged(); }
        }
        public string? Coin
        {
            get => _coin;
            set { _coin = value; OnPropertyChanged(); }
        }
        public string? Service
        {
            get => _service;
            set { _service = value; OnPropertyChanged(); }
        }
        public string? DateTime
        {
            get => _dateTime;
            set { _dateTime = value; OnPropertyChanged(); }
        }

    }

}
