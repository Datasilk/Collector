using System.Collections.Generic;

namespace Collector.Common.Models
{
    public class YouTubeCommentsChapter
    {
        public string Title { get; set; }
        public List<int> CommentIndexes { get; set; }
    }

    public class CommentTranslation
    {
        public int Index { get; set; }
        public string Translation { get; set; }
    }

    public class YouTubeCommentsAnalysis
    {
        public List<YouTubeCommentsChapter> Chapters { get; set; }
        public List<CommentTranslation> Translations { get; set; }
    }
}
