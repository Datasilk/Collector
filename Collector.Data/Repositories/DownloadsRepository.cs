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
            _dbConnection.Execute("EXEC Download_Update @qId=@qId, @status=@status", new { qId, status = (int)status });
        }

        public void UpdateUrl(long qId, string url, string domain)
        {
            _dbConnection.Execute("EXEC Download_UpdateUrl @qId=@qId, @url=@url, @domain=@domain", new { qId, url, domain });
        }

        public int AddQueueItems(string[] urls, string domain, int parentId = 0, int feedId = 0)
        {
            var count = _dbConnection.ExecuteScalar<int>("EXEC DownloadQueue_BulkAdd @urls=@urls, @domain=@domain, @parentId=@parentId, @feedId=@feedId", 
                new { urls = string.Join(",", urls), domain, parentId, feedId });
            return count;
        }

        public Int64 AddQueueItem(string url, string domain, int parentId = 0, int feedId = 0)
        {
            return _dbConnection.ExecuteScalar<Int64>("EXEC DownloadQueue_Add @url=@url, @domain=@domain, @parentId=@parentId, @feedId=@feedId", 
                new { url, domain, parentId, feedId });
        }

        public DownloadQueue CheckQueue(int feedId = 0, string domain = "", int domaindelay = 60, QueueSort sort = QueueSort.Newest)
        {
            try
            {
                using var gridReader = _dbConnection.QueryMultiple("EXEC DownloadQueue_Check @domaindelay=@domaindelay, @domain=@domain, @feedId=@feedId, @sort=@sort", 
                    new { domaindelay, domain, feedId, sort = (int)sort });

                var queue = gridReader.ReadFirstOrDefault<DownloadQueue>();
                if (queue != null)
                {
                    queue.downloadRules = gridReader.Read<DownloadRule>().ToList();
                }
                return queue;
            }
            catch (Exception ex)
            {
                // Handle exception as needed - could log here
                return null;
            }
        }

        public int Count()
        {
            return _dbConnection.ExecuteScalar<int>("EXEC Downloads_GetCount");
        }

        public void Delete(long qid)
        {
            _dbConnection.Execute("EXEC Download_Delete @qid=@qid", new { qid });
        }

        public void Move(long qid)
        {
            _dbConnection.Execute("EXEC DownloadQueue_Move @qid=@qid", new { qid });
        }

        public void Archive(long qid)
        {
            _dbConnection.Execute("EXEC DownloadQueue_Archive @qid=@qid", new { qid });
        }

        public void MoveArchived()
        {
            _dbConnection.Execute("EXEC DownloadQueue_MoveArchived");
        }
    }
}
