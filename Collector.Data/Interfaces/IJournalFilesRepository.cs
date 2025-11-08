using Collector.Data.Entities;
using System;
using System.Collections.Generic;

namespace Collector.Data.Interfaces
{
    public interface IJournalFilesRepository
    {
        int Add(JournalFile file);
        JournalFile GetById(int id);
        JournalFile GetByModuleId(Guid entryId, string moduleId);
        List<JournalFile> GetAllByEntryId(Guid entryId);
        List<JournalFile> GetAllByJournalId(int journalId);
        void Update(JournalFile file);
        void Delete(int id);
        void DeleteByModuleId(Guid entryId, string moduleId);
        void DeleteAllByEntryId(Guid entryId);
    }
}
