namespace Sunny.Subd.Core.Models
{
    public enum SubdyEnum
    {
        Stop,
        None,
        CP_282,
        CP_956,
        LogOut,
        Error,
        Captcha,
        Block,
        JobFail,
        Success,
        EmailExist,
        DIE,
    }
    public class SubdyExtension : Exception
    {
        public SubdyExtension(SubdyEnum subdyEnum, string message)
          : base(message)
        {
            SubdyEnum = subdyEnum;
            Message = message;
        }
        public SubdyEnum SubdyEnum { get; set; }
        public string Message { get; set; }
    }
}
