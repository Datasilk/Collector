namespace Collector.Data.Entities
{
    public class CleanDownload
    {
        public int totalArticles { get; set; }
        public int totalDownloads { get; set; }
        public List<Article> articles { get; set; }
        public List<DownloadQueue> downloads { get; set; }
    }
}
