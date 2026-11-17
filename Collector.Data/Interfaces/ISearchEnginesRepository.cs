using System.Collections.Generic;
using Collector.Data.Entities;

namespace Collector.Data.Interfaces
{
    public interface ISearchEnginesRepository
    {
        int Add(SearchEngine engine);
        SearchEngine GetById(int searchEngineId);
        List<SearchEngine> GetAll();
        List<SearchEngine> GetEnabled();
        /// <summary>True when an engine already targets this host for the same content type (dedupe for auto-discovery).</summary>
        bool ExistsForHostAndType(string host, string type);
        void Update(SearchEngine engine);
        void UpdateSort(List<int> searchEngineIds);
        void Delete(int searchEngineId);
        void SetEnabled(int searchEngineId, bool enabled);
    }
}
