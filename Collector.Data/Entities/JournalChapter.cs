namespace Collector.Data.Entities
{
    public class JournalChapter
    {
        public int ChapterId { get; set; }
        public int JournalId { get; set; }
        public string Title { get; set; }
        public int Sort { get; set; }
        public int Icon { get; set; }
        public int Color { get; set; }
        public string Description { get; set; }
    }
}
