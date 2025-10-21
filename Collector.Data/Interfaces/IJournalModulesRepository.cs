using Collector.Data.Entities;
using System.Collections.Generic;

namespace Collector.Data.Interfaces
{
    public interface IJournalModulesRepository
    {
        int Add(JournalModule module);
        List<JournalModule> GetAllByJournalId(int journalId);
        List<JournalModule> GetAllByEntryId(Guid entryId);
        JournalModule GetById(int journalId, Guid entryId, string moduleId);
        void Update(JournalModule module);
        void Delete(int journalId, Guid entryId, string moduleId);
        void DeleteAllByEntryId(Guid entryId);
        void ResortModules(int journalId, List<JournalModule> modules);
    }
}
