namespace Collector.API.Models
{
    public class JournalChapterModel
    {
        public int JournalId { get; set; }
        public int ChapterId { get; set; }
        public string Title { get; set; }
        public int Sort { get; set; }
        public int Icon { get; set; }
        public int Color { get; set; }
        public string Description { get; set; }
    }
}
