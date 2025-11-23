using Collector.Data.Entities;
using System.Collections.Generic;

namespace Collector.Data.Interfaces
{
    public interface IJournalTagsRepository
    {
        int Add(JournalTag tag);
        JournalTag GetById(int id);
        JournalTag GetByJournalIdAndTag(int journalId, string tag);
        List<JournalTag> GetByJournalId(int journalId);
        List<JournalTagSummary> GetSummariesByJournalId(int journalId);
        List<JournalTag> Search(int journalId, string search, int limit);
        void Delete(int id);
    }
}
