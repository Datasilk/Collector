using System.ComponentModel.DataAnnotations.Schema;

namespace Collector.Data.Entities
{
    public class JournalCategory
    {
        public int Id { get; set; }
        public Guid AppUserId { get; set; }
        public string Title { get; set; }
        public DateTime Created { get; set; }
        public int Status { get; set; }
        public string Color { get; set; }
        
        [NotMapped]
        public List<Journal> Journals { get; set; } = new List<Journal>();
    }
}
