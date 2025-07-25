namespace Collector.Auth.Models
{
    public class JwtTokenSettings
    {
        public string ExpiryMins { get; set; }
        public string RefreshExpiryMins { get; set; }
        public string Secret { get; set; }
        public string ValidIssuer { get; set; }
        public string ValidAudience { get; set; }
        public bool UseRollingRefreshTokens { get; set; }
    }
}
