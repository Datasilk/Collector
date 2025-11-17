namespace Collector.Data.Entities
{
    public class Journal
    {
        public int Id { get; set; }
        public Guid AppUserId { get; set; }
        public int CategoryId { get; set; }
        public string Title { get; set; }
        public DateTime Created { get; set; }
        public int Status { get; set; }
        public string Color { get; set; }
        public Guid? EntryId { get; set; }
        public List<JournalModule> Modules { get; set; }
    }
}
