using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Collector.API.Models
{
    public class SubjectCreateModel
    {
        public int ParentId { get; set; } = 0;
        public int GrammarType { get; set; } = 0;
        public int Score { get; set; } = 0;
        
        [Required]
        [MaxLength(50)]
        public string Title { get; set; }
        
        [MaxLength(500)]
        public string Breadcrumb { get; set; }
    }

    public class SubjectMoveModel
    {
        [Required]
        public int SubjectId { get; set; }
        
        [Required]
        public int NewParentId { get; set; }
    }

    public class SubjectListRequestModel
    {
        public string SubjectIds { get; set; } = "";
        public int ParentId { get; set; } = -1;
    }

    public class SubjectByTitleRequestModel
    {
        [Required]
        [MaxLength(50)]
        public string Title { get; set; }
        
        [MaxLength(500)]
        public string Breadcrumb { get; set; }
    }
}
