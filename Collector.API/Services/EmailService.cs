using Collector.Auth.Models;
using Collector.Auth.Services;
using Collector.Data.Entities.Auth;
using System.Net;
using System.Net.Mail;
using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Options;

namespace Collector.API.Services
{
	public interface IEmailService
	{
		void SendNewUserEmail(AppUser user);
        void SendNewAdminUserEmail(AppUser user);
        void SendResetPasswordEmail(AppUser user);
    }

	public class EmailService : IEmailService
	{
        readonly IAuthService _authService;
        private EmailSettings _settings;
        private string _domain;

        public EmailService(
            IAuthService authService, 
            IOptions<EmailSettings> settings
            )
		{
            _authService = authService;
            _settings = settings.Value;
            _domain = _authService.Settings().Domain;
        }


        public void SendEmail(MailMessage msg)
        {
            msg.Bcc.Add(_settings.TrackingEmail);
            if (msg.From == null)
                msg.From = new MailAddress(_settings.DefaultFromEmail, _settings.DefaultFromName);

            var api = _settings.SendGridApiKey;
            var client = new SendGridClient(api);
            var sendgridMessage = msg.GetSendGridMessage();
            var response = client.SendEmailAsync(sendgridMessage).Result;
            if(response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new Exception("Cannot send email via SendGrid");
            }
            //var body = response.Body.ReadAsStringAsync().Result;
        }

        public void SendNewUserEmail(AppUser user)
        {
            var url = $"{_domain}/activate/{user.PasswordResetHash}";

            using (var msg = new MailMessage())
            {
                msg.To.Add(user.Email);
                msg.Subject = "Activate your Collector account and start generating sales presentations today!";
                msg.IsBodyHtml = true;

                var fullName = WebUtility.HtmlEncode(user.Email);
                var email = WebUtility.HtmlEncode(user.Email);

                msg.Body = $@"
                <p>You have a new account for <strong>{WebUtility.HtmlEncode(_domain)}</strong>.</p>
                <br />
                <p>
                    Please follow the link below to activate your account.
                    <br />
                    <a href=""{url}""><b>Activate My Account</b></a>
                </p>
                <br/>

                <p>You can optionally copy the link below into your web browser to activate your account<br />
                <span style=""word-wrap: break-word;""><b>{url}</b></span><br /></p>
            ";

                SendEmail(msg);
            }
        }

        public void SendResetPasswordEmail(AppUser user)
        {
            var url = $"{_domain}/create-password/{user.PasswordResetHash}";

            using (var msg = new MailMessage())
            {
                msg.To.Add(user.Email);
                msg.Subject = "Reset your Collector password!";
                msg.IsBodyHtml = true;

                var fullName = WebUtility.HtmlEncode(user.Email);
                var email = WebUtility.HtmlEncode(user.Email);

                msg.Body = $@"
                <p>Your password needs to be reset for <strong>{WebUtility.HtmlEncode(_domain)}</strong>.</p>
                <br />
                <p>
                    Please follow the link below to reset your password.
                    <br />
                    <a href=""{url}""><b>Reset Password</b></a>
                </p>
                <br/>

                <p>You can optionally copy the link below into your web browser to update your password.<br />
                <span style=""word-wrap: break-word;""><b>{url}</b></span><br /></p>
            ";

                SendEmail(msg);
            }
        }

        public void SendNewAdminUserEmail(AppUser user)
        {
            var url = $"{_domain}/activate/{user.PasswordResetHash}";

            using (var msg = new MailMessage())
            {
                msg.To.Add(user.Email);
                msg.Subject = "Activate your Collector account and start generating sales presentations today!";
                msg.IsBodyHtml = true;

                var fullName = WebUtility.HtmlEncode(user.Email);
                var email = WebUtility.HtmlEncode(user.Email);

                msg.Body = $@"
                <p>You have a new account for <strong>{WebUtility.HtmlEncode(_domain)}</strong>.</p>
                <br />
                <p>
                    Please follow the link below to activate your account.
                    <br />
                    <a href=""{url}""><b>Activate My Account</b></a>
                </p>
                <br/>

                <p>You can optionally copy the link below into your web browser to activate your account<br />
                <span style=""word-wrap: break-word;""><b>{url}</b></span><br /></p>
            ";

                SendEmail(msg);
            }
        }
    }
}
