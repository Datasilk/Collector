using Collector.Data.Entities;
using Collector.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace Collector.API.Models
{
    public class ArticleListRequestModel
    {
        public int[] SubjectIds { get; set; } = Array.Empty<int>();
        public int FeedId { get; set; } = 0;
        public int DomainId { get; set; } = 0;
        public int Score { get; set; } = 0;
        public string Search { get; set; } = "";
        public ArticleIsActive IsActive { get; set; } = ArticleIsActive.Both;
        public bool IsDeleted { get; set; } = false;
        public int MinImages { get; set; } = 0;
        public DateTime? DateStart { get; set; } = null;
        public DateTime? DateEnd { get; set; } = null;
        public ArticleSortBy OrderBy { get; set; } = ArticleSortBy.BestScore;
        public int Start { get; set; } = 1;
        public int Length { get; set; } = 50;
        public bool BugsOnly { get; set; } = false;
    }

    public class ArticleCreateModel
    {
        [Required]
        public int? FeedId { get; set; }
        public short? Subjects { get; set; }
        public int? SubjectId { get; set; }
        public short? Score { get; set; }
        [Required]
        public string Domain { get; set; }
        [Required]
        public string Url { get; set; }
        [Required]
        public string Title { get; set; }
        public string Summary { get; set; }
        public double? Filesize { get; set; }
        public int? Linkcount { get; set; }
        public int? Linkwordcount { get; set; }
        public int? Wordcount { get; set; }
        public short? Sentencecount { get; set; }
        public short? Paragraphcount { get; set; }
        public short? Importantcount { get; set; }
        public short? Yearstart { get; set; }
        public short? Yearend { get; set; }
        public string Years { get; set; }
        public short? Images { get; set; }
        public DateTime? DatePublished { get; set; }
        public short? Relavance { get; set; }
        public short? Importance { get; set; }
        public short? Fiction { get; set; }
        public double? Analyzed { get; set; }
        public bool? Active { get; set; }
    }

    public class ArticleUpdateModel
    {
        [Required]
        public int ArticleId { get; set; }
        public short? Subjects { get; set; }
        public int? SubjectId { get; set; }
        public short? Score { get; set; }
        public string Domain { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public double? Filesize { get; set; }
        public int? Linkcount { get; set; }
        public int? Linkwordcount { get; set; }
        public int? Wordcount { get; set; }
        public short? Sentencecount { get; set; }
        public short? Paragraphcount { get; set; }
        public short? Importantcount { get; set; }
        public short? Yearstart { get; set; }
        public short? Yearend { get; set; }
        public string Years { get; set; }
        public short? Images { get; set; }
        public DateTime? DatePublished { get; set; }
        public short? Relavance { get; set; }
        public short? Importance { get; set; }
        public short? Fiction { get; set; }
        public double? Analyzed { get; set; }
    }

    public class ArticleUrlUpdateModel
    {
        [Required]
        public int ArticleId { get; set; }
        [Required]
        public string Url { get; set; }
        [Required]
        public string Domain { get; set; }
        public int ParentId { get; set; } = 0;
    }

    public class ArticleCacheUpdateModel
    {
        [Required]
        public int ArticleId { get; set; }
        [Required]
        public bool Cached { get; set; }
    }

    public class ArticleDateModel
    {
        [Required]
        public int ArticleId { get; set; }
        [Required]
        public DateTime Date { get; set; }
        public bool HasYear { get; set; } = true;
        public bool HasMonth { get; set; } = true;
        public bool HasDay { get; set; } = true;
    }

    public class ArticleSentenceModel
    {
        [Required]
        public int ArticleId { get; set; }
        [Required]
        public int Index { get; set; }
        [Required]
        public string Sentence { get; set; }
    }

    public class ArticleSubjectModel
    {
        [Required]
        public int ArticleId { get; set; }
        [Required]
        public int SubjectId { get; set; }
        public DateTime? DatePublished { get; set; }
        public int Score { get; set; } = 0;
    }

    public class ArticleWordModel
    {
        [Required]
        public int ArticleId { get; set; }
        [Required]
        public int WordId { get; set; }
        [Required]
        public int Count { get; set; }
    }

    public class ArticleListResponse
    {
        public List<ArticleDetails> Articles { get; set; }
        public int TotalCount { get; set; }
    }
}
