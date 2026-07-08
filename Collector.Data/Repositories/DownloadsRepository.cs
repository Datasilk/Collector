using Dapper;
using System.Data;
using Collector.Data.Entities;
using Collector.Data.Enums;
using Collector.Data.Interfaces;

namespace Collector.Data.Repositories
{
    public class DownloadsRepository : IDownloadsRepository
    {
        readonly IDbConnection _dbConnection;

        public DownloadsRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public void UpdateQueueItem(long qId, QueueStatus status = QueueStatus.downloaded)
        {
            _dbConnection.Execute("SELECT public.\"Download_Update\"(@qId, @status)", new { qId, status = (int)status });
        }

        public void UpdateUrl(long qId, string url, string domain)
        {
            _dbConnection.Execute("SELECT public.\"Download_UpdateUrl\"(@qId, @url, @domain)", new { qId, url, domain });
        }

        public void UpdateQueueType(long qId, Collector.Common.Enums.QueueFileType type)
        {
            _dbConnection.Execute("SELECT public.\"Download_UpdateType\"(@qId, @type::SMALLINT)", new { qId, type = (int)type });
        }

        public int AddQueueItems(string[] urls, string domain, int parentId = 0, int feedId = 0)
        {
            var count = _dbConnection.ExecuteScalar<int>("SELECT public.\"DownloadQueue_BulkAdd\"(@urls, @domain, @parentId, @feedId)", 
                new { urls = string.Join(",", urls), domain, parentId, feedId });
            return count;
        }

        public Int64 AddQueueItem(string url, string domain, int parentId = 0, int feedId = 0)
        {
            return _dbConnection.ExecuteScalar<Int64>("SELECT public.\"DownloadQueue_Add\"(@url, @domain, @parentId, @feedId)", 
                new { url, domain, parentId, feedId });
        }

        public DownloadQueue CheckQueue(int feedId = 0, string domain = "", int domaindelay = 60, QueueSort sort = QueueSort.Newest, long queueId = 0)
        {
            var queue = _dbConnection.QueryFirstOrDefault<DownloadQueue>("SELECT * FROM public.\"DownloadQueue_Check\"(@domaindelay, @domain, @feedId, @sort, @queueId)", 
                new { domaindelay, domain, feedId, sort = (int)sort, queueId });
            if (queue != null)
            {
                queue.downloadRules = _dbConnection.Query<DownloadRule>("SELECT * FROM public.\"DownloadQueue_Check_DownloadRules\"(@queueId)", new { queueId = queue.qid }).ToList();
            }
            return queue;
        }

        public int Count()
        {
            return _dbConnection.ExecuteScalar<int>("SELECT public.\"Downloads_GetCount\"()");
        }

        public IEnumerable<DownloadQueue> GetQueueList(string search, int status, string sort, int start, int length)
        {
            return _dbConnection.Query<DownloadQueue>("SELECT * FROM public.\"DownloadQueue_GetList\"(@search, @status, @sort, @start, @length)",
                new { search, status, sort, start, length });
        }

        public int GetQueueCount(string search, int status)
        {
            return _dbConnection.ExecuteScalar<int>("SELECT public.\"DownloadQueue_GetCount\"(@search, @status)", new { search, status });
        }

        public void Delete(long qid)
        {
            _dbConnection.Execute("SELECT public.\"Download_Delete\"(@qid)", new { qid });
        }

        public void Move(long qid)
        {
            _dbConnection.Execute("SELECT public.\"DownloadQueue_Move\"(@qid)", new { qid });
        }

        public void Archive(long qid)
        {
            _dbConnection.Execute("SELECT public.\"DownloadQueue_Archive\"(@qid)", new { qid });
        }

        public void MoveArchived()
        {
            _dbConnection.Execute("SELECT public.\"DownloadQueue_MoveArchived\"()");
        }
    }
}
