namespace Collector.Data.Entities
{
    public class Subject
    {
        public int subjectId { get; set; } // int
        public int? parentId { get; set; } // int
        public int? grammartype { get; set; } // int
        public int? score { get; set; } // int
        public bool? haswords { get; set; } // bit
        public string title { get; set; } // nvarchar(50)
        public string hierarchy { get; set; } // nvarchar(50)
        public string breadcrumb { get; set; } // nvarchar(500)
    }

}
