using System;
using System.Collections.Generic;
using Collector.Data.Entities;
using Collector.Data.Enums;

namespace Collector.Data.Interfaces
{
    public interface IFeedsRepository
    {
        int Add(FeedDocType doctype, int categoryId, string title, string url, string domain, string filter = "", int checkIntervals = 720);
        Feed GetInfo(int feedId);
        void LogCheckedLinks(int feedId, int count);
        void UpdateLastChecked(int feedId);
        List<Feed> GetList();
        List<FeedWithLog> GetListWithLogs(int days = 7, DateTime? dateStart = null);
        void AddCategory(string title);
        List<FeedCategory> GetCategories();
        List<Feed> Check(int feedId = 0);
    }
}
