namespace Collector.API.Models
{
    public class JournalTagSearchModel
    {
        public string Search { get; set; }
        public int Limit { get; set; } = 10;
    }

    public class JournalTagCreateModel
    {
        public string Tag { get; set; }
    }

    public class JournalEntryTagModel
    {
        public int TagId { get; set; }
    }
}
