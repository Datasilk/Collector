using Collector.Data.Entities;
using Collector.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace Collector.API.Models
{
    public class DomainFilterModel
    {
        public int[] SubjectIds { get; set; }
        public DomainFilterType Type { get; set; } = DomainFilterType.All;
        public DomainType DomainType { get; set; } = DomainType.unused;
        public DomainType DomainType2 { get; set; } = DomainType.unused;
        public DomainSort Sort { get; set; } = DomainSort.Alphabetical;
        public string Lang { get; set; } = "";
        public string Search { get; set; } = "";
        public int Start { get; set; } = 1;
        public int Length { get; set; } = 50;
        public int ParentId { get; set; } = -1;
    }

    public class AddDomainModel
    {
        [Required]
        public string Domain { get; set; }
        public string Title { get; set; } = "";
        public int ParentId { get; set; } = 0;
        public int Type { get; set; } = 0;
    }

    public class UpdateDomainInfoModel
    {
        [Required]
        public int DomainId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Lang { get; set; }
    }

    public class UpdateDomainTypeModel
    {
        [Required]
        public int DomainId { get; set; }
        [Required]
        public DomainType Type { get; set; }
    }

    public class UpdateDomainHttpsWwwModel
    {
        [Required]
        public int DomainId { get; set; }
        public bool Https { get; set; }
        public bool Www { get; set; }
    }

    public class DomainStatusModel
    {
        [Required]
        public int DomainId { get; set; }
        public bool Status { get; set; }
    }

    public class AnalyzerRuleModel
    {
        [Required]
        public int DomainId { get; set; }
        [Required]
        public string Selector { get; set; }
        public bool Rule { get; set; }
    }

    public class DownloadRuleModel
    {
        [Required]
        public int DomainId { get; set; }
        public bool Rule { get; set; }
        [Required]
        public string Url { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
    }

    public class DomainCollectionModel
    {
        [Required]
        public int ColGroupId { get; set; }
        [Required]
        public string Name { get; set; }
        public string Search { get; set; } = "";
        public int SubjectId { get; set; } = 0;
        public DomainFilterType FilterType { get; set; } = DomainFilterType.All;
        public DomainType Type { get; set; } = DomainType.unused;
        public DomainSort Sort { get; set; } = DomainSort.Alphabetical;
        public string Lang { get; set; } = "";
    }

    public class CollectionGroupModel
    {
        [Required]
        public string Name { get; set; }
    }

    public class DomainTypeMatchModel
    {
        [Required]
        public List<DomainTypeMatchPart> Parts { get; set; }
        [Required]
        public int Type { get; set; }
        public int Type2 { get; set; }
        public int Threshold { get; set; }
        public int Rank { get; set; }
    }
}
