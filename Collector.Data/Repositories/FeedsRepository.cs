using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Collector.Data.Entities;
using Collector.Data.Enums;
using Collector.Data.Interfaces;
using Dapper;

namespace Collector.Data.Repositories
{
    public class FeedsRepository : IFeedsRepository
    {
        private readonly IDbConnection _db;

        public FeedsRepository(IDbConnection db)
        {
            _db = db;
        }

        public int Add(FeedDocType doctype, int categoryId, string title, string url, string domain, string filter = "", int checkIntervals = 720)
        {
            return _db.ExecuteScalar<int>("Feed_Add", new { doctype = (int)doctype, categoryId, title, url, domain, filter, checkIntervals }, commandType: CommandType.StoredProcedure);
        }

        public Feed GetInfo(int feedId)
        {
            return _db.Query<Feed>("Feed_GetInfo", new { feedId }, commandType: CommandType.StoredProcedure).FirstOrDefault();
        }

        public void LogCheckedLinks(int feedId, int count)
        {
            _db.Execute("FeedCheckedLog_Add", new { feedId, count }, commandType: CommandType.StoredProcedure);
        }

        public void UpdateLastChecked(int feedId)
        {
            _db.Execute("Feed_Checked", new { feedId }, commandType: CommandType.StoredProcedure);
        }

        public List<Feed> GetList()
        {
            return _db.Query<Feed>("Feeds_GetList", commandType: CommandType.StoredProcedure).ToList();
        }

        public List<FeedWithLog> GetListWithLogs(int days = 7, DateTime? dateStart = null)
        {
            return _db.Query<FeedWithLog>("Feeds_GetListWithLogs",
                new { days, dateStart = dateStart ?? DateTime.Now.AddDays(-7) }, commandType: CommandType.StoredProcedure).ToList();
        }

        public void AddCategory(string title)
        {
            _db.Execute("Feeds_Category_Add", new { title }, commandType: CommandType.StoredProcedure);
        }

        public List<FeedCategory> GetCategories()
        {
            return _db.Query<FeedCategory>("Feeds_Categories_GetList", commandType: CommandType.StoredProcedure).ToList();
        }

        public List<Feed> Check(int feedId = 0)
        {
            return _db.Query<Feed>("Feeds_Check", new { feedId }, commandType: CommandType.StoredProcedure).ToList();
        }
    }
}
