using System.Net.Mail;
using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Options;
using Collector.Auth.Models;

namespace Collector.Auth.Services
{
	public interface IAuthEmailService
	{
		void SendEmail(MailMessage msg);
	}

	public class AuthEmailService : IAuthEmailService
	{
		private EmailSettings _settings;

		public AuthEmailService(IOptions<EmailSettings> settings)
		{
			_settings = settings.Value;
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
			var body = response.Body.ReadAsStringAsync().Result;
		}
	}
}
