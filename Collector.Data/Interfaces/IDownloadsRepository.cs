using Collector.Data.Entities;
using Collector.Data.Enums;

namespace Collector.Data.Interfaces
{
    public interface IDownloadsRepository
    {
        void UpdateQueueItem(long qId, QueueStatus status = QueueStatus.downloaded);
        void UpdateUrl(long qId, string url, string domain);
        void UpdateQueueType(long qId, Collector.Common.Enums.QueueFileType type);
        int AddQueueItems(string[] urls, string domain, int parentId = 0, int feedId = 0);
        Int64 AddQueueItem(string url, string domain, int parentId = 0, int feedId = 0);
        DownloadQueue CheckQueue(int feedId = 0, string domain = "", int domaindelay = 60, QueueSort sort = QueueSort.Newest);
        int Count();
        void Delete(long qid);
        void Move(long qid);
        void Archive(long qid);
        void MoveArchived();
    }
}
