namespace Collector.API.Models
{
    public class UserFilterModel
    {
        public string FullName { get; set; } = "";
        public int Role { get; set; } = 0;
        public int? RadioStationId { get; set; } = null;
        public string Sort { get; set; } = "Email ASC";
        public int Start { get; set; } = 0;
        public int Length { get; set; } = 10;
    }
}
