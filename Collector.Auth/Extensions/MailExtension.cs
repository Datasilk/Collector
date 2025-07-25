using System.Net.Mail;

namespace SendGrid.Helpers.Mail
{
	public static partial class MailExtension
	{
		public static EmailAddress GetSendGridAddress(this MailAddress address)
		{
			// SendGrid Server-Side API is currently bugged, and messes up when the name has a comma or a semicolon in it
			return String.IsNullOrWhiteSpace(address.DisplayName) ?
				new EmailAddress(address.Address) :
				new EmailAddress(address.Address, address.DisplayName.Replace(",", "").Replace(";", ""));
		}
        public static EmailAddress GetSendGridFromAddress(this MailAddress address)
        {
            // SendGrid Server-Side API is currently bugged, and messes up when the name has a comma or a semicolon in it
            return String.IsNullOrWhiteSpace(address.DisplayName) ?
                new EmailAddress(address.Address) :
                new EmailAddress(address.Address, address.DisplayName.Replace(";", ""));
        }

        public static Attachment GetSendGridAttachment(this System.Net.Mail.Attachment attachment)
		{
			using (var stream = new MemoryStream())
			{
				try
				{
					attachment.ContentStream.CopyTo(stream);
					return new Attachment()
					{
						Disposition = "attachment",
						Type = attachment.ContentType.MediaType,
						Filename = attachment.Name,
						ContentId = attachment.ContentId,
						Content = Convert.ToBase64String(stream.ToArray())
					};
				}
				finally
				{
					stream.Close();
				}
			}
		}

		public static SendGridMessage GetSendGridMessage(this MailMessage message)
		{
			var msg = new SendGridMessage();

			msg.From = message.From.GetSendGridFromAddress();

			foreach (var a in message.To)
			{
				msg.AddTo(a.GetSendGridAddress());
			}
			foreach (var a in message.CC)
			{
				if (!message.To.Contains(a))
					msg.AddCc(a.GetSendGridAddress());
			}
			foreach (var a in message.Bcc)
			{
				if (!message.To.Contains(a) && !message.To.Contains(a))
					msg.AddBcc(a.GetSendGridAddress());
			}

			if (!String.IsNullOrWhiteSpace(message.Subject))
			{
				msg.Subject = message.Subject;
			}
			if (!String.IsNullOrWhiteSpace(message.Body))
			{
				msg.HtmlContent = message.Body;
			}

			foreach (var attachment in message.Attachments)
			{
				msg.AddAttachment(attachment.GetSendGridAttachment());
			}

			return msg;
		}
	}
}
