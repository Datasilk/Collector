namespace Collector.Data.Entities
{
    public class Article
    {
        public int articleId { get; set; }
        public int? feedId { get; set; }
        public short? subjects { get; set; }
        public int? subjectId { get; set; }
        public int domainId { get; set; }
        public short? score { get; set; }
        public short? images { get; set; }
        public double? filesize { get; set; }
        public int? linkcount { get; set; }
        public int? linkwordcount { get; set; }
        public int? wordcount { get; set; }
        public short? sentencecount { get; set; }
        public short? paragraphcount { get; set; }
        public short? importantcount { get; set; }
        public short? analyzecount { get; set; }
        public short? yearstart { get; set; }
        public short? yearend { get; set; }
        public string years { get; set; }
        public DateTime? datecreated { get; set; }
        public DateTime? datepublished { get; set; }
        public short? relavance { get; set; }
        public short? importance { get; set; }
        public short? fiction { get; set; }
        public string domain { get; set; }
        public string url { get; set; }
        public string title { get; set; }
        public string summary { get; set; }
        public double? analyzed { get; set; }
        public int visited { get; set; }
        public bool? cached { get; set; }
        public bool? active { get; set; }
        public bool? deleted { get; set; }
    }

    public class ArticleDetails : Article
    {
        public int bugsopen;
        public int bugsresolved;
        public string breadcrumb;
        public string hierarchy;
        public string subjectTitle;
    }

    public class ArticleDate
    {
        public int articleId { get; set; }
        public DateTime? date { get; set; }
        public bool? hasyear { get; set; }
        public bool? hasmonth { get; set; }
        public bool? hasday { get; set; }
    }

    public class ArticleSentence
    {
        public int articleId { get; set; }
        public short? index { get; set; }
        public string sentence { get; set; }
    }

    public class ArticleSubject
    {
        public int subjectId { get; set; }
        public int? articleId { get; set; }
        public short? score { get; set; }
        public DateTime? datecreated { get; set; }
        public DateTime? datepublished { get; set; }
    }

    public class ArticleWord
    {
        public int articleId { get; set; }
        public int wordId { get; set; }
        public int? count { get; set; }
    }
}
