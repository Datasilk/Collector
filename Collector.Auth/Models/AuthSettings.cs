namespace Collector.Auth.Models
{
    public class AuthSettings
    {
        public string CentralAuthKey { get; set; } = "";
        public string Domain { get; set; } = "";
        public JwtTokenSettings JWT { get; set; } = new JwtTokenSettings();
    }
}
