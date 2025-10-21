namespace Collector.Data.Entities
{
    public class JournalModule
    {
        public int JournalId { get; set; }
        public Guid JournalEntryId { get; set; }
        public string ModuleId { get; set; }
        public int Sort { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string Json { get; set; }
    }
}
