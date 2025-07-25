namespace Collector.API.Models
{
    public class UpdateLockRequest
    {
        public Guid UserId { get; set; }
        public bool lockUser { get; set; }
    }
}
