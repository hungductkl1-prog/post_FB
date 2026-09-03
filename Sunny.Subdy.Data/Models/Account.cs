using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using static Sunny.Subdy.Data.AppDbContext;

namespace Sunny.Subdy.Data.Models;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class Account : INotifyPropertyChanged, IThrottledNotify
{
    [SqlKey]
    public Guid Id { get => _id; set => SetField(ref _id, value); }
    public string Uid { get => _uid; set => SetField(ref _uid, value); }
    public string TokenJob { get => _tokenJob; set => SetField(ref _tokenJob, value); }
    public string Password { get => _password; set => SetField(ref _password, value); }
    public string TowFA { get => _towFA; set => SetField(ref _towFA, value); }
    public string Cookie { get => _cookie; set => SetField(ref _cookie, value); }
    public string Token { get => _token; set => SetField(ref _token, value); }
    public string Proxy { get => _proxy; set => SetField(ref _proxy, value); }
    public string Email { get => _email; set => SetField(ref _email, value); }
    public string Phone { get => _phone; set => SetField(ref _phone, value); }
    public string UserAgent { get => _userAgent; set => SetField(ref _userAgent, value); }
    public string FullName { get => _fullName; set => SetField(ref _fullName, value); }
    public string State { get => _state; set => SetField(ref _state, value); }
    public string Status { get => _status; set => SetField(ref _status, value); }
    public string Result { get => _result; set => SetField(ref _result, value); }
    public string Serial { get => _serial; set => SetField(ref _serial, value); }
    public string IP { get => _ip; set => SetField(ref _ip, value); }
    public string UserName { get => _userName; set => SetField(ref _userName, value); }
    public string NameFolder { get => _nameFolder; set => SetField(ref _nameFolder, value); }
    
    public string Gender { get => _gender; set => SetField(ref _gender, value); }
    public string Friends { get => _friends; set => SetField(ref _friends, value); }
    public string Groups { get => _groups; set => SetField(ref _groups, value); }
    public string Follow { get => _follow; set => SetField(ref _follow, value); }
    public string Birthday { get => _birthday; set => SetField(ref _birthday, value); }
    public string Bio { get => _bio; set => SetField(ref _bio, value); }
    public string PagePro5 { get => _pagePro5; set => SetField(ref _pagePro5, value); }
    public string DateCreate { get => _dateCreate; set => SetField(ref _dateCreate, value); }
    public string Avatar { get => _avatar; set => SetField(ref _avatar, value); }
    public string Note { get => _note; set => SetField(ref _note, value); }
    public string DeviceInfo { get => _deviceInfo; set => SetField(ref _deviceInfo, value); }
    public string EmailAddress { get => _emailAddress; set => SetField(ref _emailAddress, value); }
    public string PassMail { get => _passMail; set => SetField(ref _passMail, value); }
    public string MailClientId { get => _mailClientId; set => SetField(ref _mailClientId, value); }
    public string MailRefreshToken { get => _mailRefreshToken; set => SetField(ref _mailRefreshToken, value); }
    public string PassPrivateEmailAddress { get => _passPrivateEmailAddress; set => SetField(ref _passPrivateEmailAddress, value); }
    public string MailRecovery { get => _mailRecovery; set => SetField(ref _mailRecovery, value); }
    public string Platformt { get => _platform; set => SetField(ref _platform, value); }
    public bool Checked { get => _checked; set => SetField(ref _checked, value); }
    public bool Running { get => _running; set => SetField(ref _running, value); }
    public bool IsView { get => _isView; set => SetField(ref _isView, value); }
    public int ColorType { get => _colorType; set => SetField(ref _colorType, value); }
    public string RecentInteraction { get => _recentInteraction; set => SetField(ref _recentInteraction, value); }
    public string Uid_Email { get; set; } = string.Empty;

    private Guid _id = Guid.NewGuid();
    private string _uid = "", _password = "", _tokenJob = "", _towFA = "", _cookie = "", _token = "", _proxy = "", _email = "",
                   _phone = "", _userAgent = "", _fullName = "", _state = "", _status = "", _result = "", _serial = "",
                   _ip = "", _userName = "", _nameFolder = "", _gender = "", _friends = "", _groups = "", _follow = "",
                   _birthday = "", _bio = "", _pagePro5 = "", _dateCreate = "", _avatar = "", _note = "", _deviceInfo = "",
                   _emailAddress = "", _passMail = "", _mailClientId = "", _mailRefreshToken = "", _passPrivateEmailAddress = "", _recentInteraction = "", _platform = "", _nameScript="", _mailRecovery="";

    private bool _checked = false, _running = false, _isView = true;
    private int _bulkLoadDepth;

    /// <summary>Tắt PropertyChanged khi load/ghi hàng loạt (50k+ account).</summary>
    public void BeginBulkLoad() => _bulkLoadDepth++;

    public void EndBulkLoad()
    {
        if (_bulkLoadDepth > 0) _bulkLoadDepth--;
    }

    /// <summary>Toggle checkbox không bắn PropertyChanged (tick hàng loạt).</summary>
    public void SetCheckedSilently(bool value) => _checked = value;

    private int _colorType = 0, _total=0;
    private string _jobToday = "0", _summary = "", _summary_Skip = "", _xuToday = "";
    private int _index = 0;
    public int JobTotal { get => _total; set => SetField(ref _total, value); }
    [NotMapped]
    public string JobToday { get => _jobToday; set => SetField(ref _jobToday, value); }
    [NotMapped]
    public string Summary { get => _summary; set => SetField(ref _summary, value); }
    [NotMapped]
    public string Summary_Skip { get => _summary_Skip; set => SetField(ref _summary_Skip, value); }
    [NotMapped]
    public string XuToday { get => _xuToday; set => SetField(ref _xuToday, value); }

    public string NameScript  { get => _nameScript; set => SetField(ref _nameScript, value); }

    [NotMapped]
    public int STT { get => _index; set => SetField(ref _index, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (propertyName == null || _bulkLoadDepth > 0) return;
        // Throttled: just mark dirty, the shared timer will fire the event on UI thread
        ThrottledPropertyNotifier.MarkDirty(this, propertyName);
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
