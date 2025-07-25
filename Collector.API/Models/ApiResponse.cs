namespace Collector.API.Models
{
    public class ApiResponse
    {
        public bool success { get; set; } = false;
        public string message { get; set; } = "";
        public object data { get; set; } = null;
    }
}
