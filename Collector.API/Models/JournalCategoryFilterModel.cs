namespace Collector.API.Models
{
    public class JournalCategoryFilterModel
    {
        public int? Sort { get; set; }
        public string Search { get; set; }
        public bool IncludeArchived { get; set; }
    }
}
