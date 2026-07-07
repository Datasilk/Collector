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
            return _db.ExecuteScalar<int>("SELECT public.\"Feed_Add\"(@doctype, @categoryId, @title, @url, @domain, @filter, @checkIntervals)",
                new { doctype = (int)doctype, categoryId, title, url, domain, filter, checkIntervals });
        }

        public Feed GetInfo(int feedId)
        {
            return _db.Query<Feed>("SELECT * FROM public.\"Feed_GetInfo\"(@feedId)", new { feedId }).FirstOrDefault();
        }

        public void LogCheckedLinks(int feedId, int count)
        {
            _db.Execute("SELECT public.\"FeedCheckedLog_Add\"(@feedId, @count)", new { feedId, count });
        }

        public void UpdateLastChecked(int feedId)
        {
            _db.Execute("SELECT public.\"Feed_Checked\"(@feedId)", new { feedId });
        }

        public List<Feed> GetList()
        {
            return _db.Query<Feed>("SELECT * FROM public.\"Feeds_GetList\"()").ToList();
        }

        public List<FeedWithLog> GetListWithLogs(int days = 7, DateTime? dateStart = null)
        {
            return _db.Query<FeedWithLog>("SELECT * FROM public.\"Feeds_GetListWithLogs\"(@days, @dateStart)",
                new { days, dateStart = (dateStart ?? DateTime.Now.AddDays(-7)).Date }).ToList();
        }

        public void AddCategory(string title)
        {
            _db.Execute("SELECT public.\"Feeds_Category_Add\"(@title)", new { title });
        }

        public List<FeedCategory> GetCategories()
        {
            return _db.Query<FeedCategory>("SELECT * FROM public.\"Feeds_Categories_GetList\"()").ToList();
        }

        public List<Feed> Check(int feedId = 0)
        {
            return _db.Query<Feed>("SELECT * FROM public.\"Feeds_Check\"(@feedId)", new { feedId }).ToList();
        }

        public List<Feed> GetFilteredFeeds(int start, int length, string search, string sort)
        {
            int sortValue;
            switch (sort.ToLower())
            {
                case "url":
                    sortValue = 0; // Url ASC
                    break;
                case "url_desc":
                    sortValue = 1; // Url DESC
                    break;
                case "checkinterval":
                    sortValue = 2; // CheckIntervals ASC
                    break;
                case "checkinterval_desc":
                    sortValue = 3; // CheckIntervals DESC
                    break;
                case "title_desc":
                    sortValue = 5; // Title DESC
                    break;
                default:
                    sortValue = 4; // Title ASC
                    break;
            }
            return _db.Query<Feed>("SELECT * FROM public.\"Feeds_Filter\"(@start, @length, @search, @sort)",
                new { start, length, search, sort = sortValue }).ToList();
        }
    }
}
