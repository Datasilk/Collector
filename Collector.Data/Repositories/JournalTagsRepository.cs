using Collector.Data.Entities;
using Collector.Data.Interfaces;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Collector.Data.Repositories
{
    public class JournalTagsRepository : IJournalTagsRepository
    {
        private readonly IDbConnection _dbConnection;

        public JournalTagsRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public int Add(JournalTag tag)
        {
            return _dbConnection.QuerySingle<int>(@"
                INSERT INTO public.""JournalTags"" (""JournalId"", ""Tag"")
                VALUES (@JournalId, @Tag)
                RETURNING ""Id""", tag);
        }

        public JournalTag GetById(int id)
        {
            return _dbConnection.QuerySingleOrDefault<JournalTag>(@"
                SELECT * FROM public.""JournalTags""
                WHERE ""Id"" = @Id", new { Id = id });
        }

        public JournalTag GetByJournalIdAndTag(int journalId, string tag)
        {
            return _dbConnection.QuerySingleOrDefault<JournalTag>(@"
                SELECT * FROM public.""JournalTags""
                WHERE ""JournalId"" = @JournalId AND ""Tag"" = @Tag",
                new { JournalId = journalId, Tag = tag });
        }

        public List<JournalTag> GetByJournalId(int journalId)
        {
            return _dbConnection.Query<JournalTag>(@"
                SELECT * FROM public.""JournalTags""
                WHERE ""JournalId"" = @JournalId
                ORDER BY ""Tag""",
                new { JournalId = journalId }).ToList();
        }

        public List<JournalTagSummary> GetSummariesByJournalId(int journalId)
        {
            return _dbConnection.Query<JournalTagSummary>(@"
                SELECT jt.""Id"", jt.""JournalId"", jt.""Tag"", COUNT(jet.""JournalEntryId"") AS EntryCount
                FROM public.""JournalTags"" jt
                LEFT JOIN public.""JournalEntryTags"" jet ON jet.""TagId"" = jt.""Id""
                WHERE jt.""JournalId"" = @JournalId
                GROUP BY jt.""Id"", jt.""JournalId"", jt.""Tag""
                ORDER BY jt.""Tag""",
                new { JournalId = journalId }).ToList();
        }

        public List<JournalTag> Search(int journalId, string search, int limit)
        {
            var normalizedSearch = (search ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(normalizedSearch)) return new List<JournalTag>();

            var maxResults = limit < 1 ? 10 : limit;

            return _dbConnection.Query<JournalTag>(@"
                SELECT *
                FROM public.""JournalTags""
                WHERE ""JournalId"" = @JournalId AND ""Tag"" LIKE @Search || '%'
                ORDER BY ""Tag""
                LIMIT @Limit",
                new { JournalId = journalId, Limit = maxResults, Search = normalizedSearch }).ToList();
        }

        public void Delete(int id)
        {
            _dbConnection.Execute(@"
                DELETE FROM public.""JournalTags""
                WHERE ""Id"" = @Id", new { Id = id });
        }
    }
}
