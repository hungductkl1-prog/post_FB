using Sunny.Subdy.Common.Models;

namespace Sunny.Subd.Core.Email
{
    public class EmailService
    {
        private string _site;
        public EmailService(string site)
        {
            _site = site;
        }
        public async Task<string> GetCode(string email, string token)
        {
            string code =string.Empty;
            switch (_site)
            {
                case RegistrationType.Domain_TheLoi:
                    {
                        return await MailTheLoiService.GetOTP(email, token);
                    }
                case RegistrationType.Domain_Getnada:
                    {
                        return await GetnadaService.GetCode(email);
                    }
                case RegistrationType.Domain_TempMail:
                    {
                        return await TempMailService.GetCode(email);
                    }
                default:
                    {
                        throw new NotSupportedException($"Email service '{_site}' is not supported.");
                    }
            }
        }
        public async Task<string> GetEmail(string token)
        {
            string code = string.Empty;
            switch (_site)
            {
                case RegistrationType.Domain_TheLoi:
                    {
                        return await MailTheLoiService.GetEmail(token);
                    }
                case RegistrationType.Domain_Getnada:
                    {
                        return await GetnadaService.GetEmail();
                    }
                case RegistrationType.Domain_TempMail:
                    {
                        return await TempMailService.GetEmail();
                    }
                default:
                    {
                        throw new NotSupportedException($"Email service '{_site}' is not supported.");
                    }
            }
        }
    }
}
