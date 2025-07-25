namespace Collector.Auth.Models
{
	public class EmailSettings
	{
		public bool UseSendGrid { get; set; }
		public string SendGridApiKey { get; set; }
		public string DefaultFromEmail { get; set; }
		public string DefaultFromName { get; set; }
		public string TrackingEmail { get; set; }
	}
}
