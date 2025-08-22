namespace Collector.API.Models
{
    public class JournalModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Color { get; set; }
        public int? CategoryId { get; set; }
        public int? ThemeId { get; set; }
    }
}
