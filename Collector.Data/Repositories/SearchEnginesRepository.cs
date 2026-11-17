using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Collector.Data.Entities;
using Collector.Data.Interfaces;
using Dapper;

namespace Collector.Data.Repositories
{
    public class SearchEnginesRepository : ISearchEnginesRepository
    {
        private readonly IDbConnection _dbConnection;

        public SearchEnginesRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public int Add(SearchEngine engine)
        {
            return _dbConnection.ExecuteScalar<int>(@"
                INSERT INTO public.""SearchEngines""
                (""name"", ""url"", ""type"", ""method"", ""headervars"", ""requestbodyvars"", ""requestbodydatatype"", ""discoveredfrom"", ""sort"", ""enabled"", ""dateupdated"")
                VALUES (@Name, @Url, @Type, @Method, @HeaderVars, @RequestBodyVars, @RequestBodyDataType, @DiscoveredFrom, @Sort, @Enabled, CURRENT_TIMESTAMP)
                RETURNING ""searchEngineId""",
                engine);
        }

        public SearchEngine GetById(int searchEngineId)
        {
            return _dbConnection.QuerySingleOrDefault<SearchEngine>(@"
                SELECT * FROM public.""SearchEngines""
                WHERE ""searchEngineId"" = @searchEngineId",
                new { searchEngineId });
        }

        public List<SearchEngine> GetAll()
        {
            return _dbConnection.Query<SearchEngine>(@"
                SELECT * FROM public.""SearchEngines""
                ORDER BY ""sort"", ""searchEngineId""").ToList();
        }

        public List<SearchEngine> GetEnabled()
        {
            return _dbConnection.Query<SearchEngine>(@"
                SELECT * FROM public.""SearchEngines""
                WHERE ""enabled"" = TRUE
                ORDER BY ""sort"", ""searchEngineId""").ToList();
        }

        public bool ExistsForHostAndType(string host, string type)
        {
            if (string.IsNullOrWhiteSpace(host)) return false;
            return _dbConnection.ExecuteScalar<bool>(@"
                SELECT EXISTS(
                    SELECT 1 FROM public.""SearchEngines""
                    WHERE ""url"" ILIKE '%' || @host || '%'
                      AND ""type"" = @type)",
                new { host, type = string.IsNullOrWhiteSpace(type) ? "web" : type });
        }

        public void Update(SearchEngine engine)
        {
            _dbConnection.Execute(@"
                UPDATE public.""SearchEngines""
                SET ""name"" = @Name,
                    ""url"" = @Url,
                    ""type"" = @Type,
                    ""method"" = @Method,
                    ""headervars"" = @HeaderVars,
                    ""requestbodyvars"" = @RequestBodyVars,
                    ""requestbodydatatype"" = @RequestBodyDataType,
                    ""discoveredfrom"" = @DiscoveredFrom,
                    ""sort"" = @Sort,
                    ""enabled"" = @Enabled,
                    ""dateupdated"" = CURRENT_TIMESTAMP
                WHERE ""searchEngineId"" = @SearchEngineId",
                engine);
        }

        public void UpdateSort(List<int> searchEngineIds)
        {
            for (int i = 0; i < searchEngineIds.Count; i++)
            {
                _dbConnection.Execute(@"
                    UPDATE public.""SearchEngines""
                    SET ""sort"" = @sort,
                        ""dateupdated"" = CURRENT_TIMESTAMP
                    WHERE ""searchEngineId"" = @searchEngineId",
                    new { sort = i + 1, searchEngineId = searchEngineIds[i] });
            }
        }

        public void Delete(int searchEngineId)
        {
            _dbConnection.Execute(@"
                DELETE FROM public.""SearchEngines""
                WHERE ""searchEngineId"" = @searchEngineId",
                new { searchEngineId });
        }

        public void SetEnabled(int searchEngineId, bool enabled)
        {
            _dbConnection.Execute(@"
                UPDATE public.""SearchEngines""
                SET ""enabled"" = @enabled,
                    ""dateupdated"" = CURRENT_TIMESTAMP
                WHERE ""searchEngineId"" = @searchEngineId",
                new { searchEngineId, enabled });
        }
    }
}
