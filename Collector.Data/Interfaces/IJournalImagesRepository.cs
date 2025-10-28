using Collector.Data.Entities;
using System;
using System.Collections.Generic;

namespace Collector.Data.Interfaces
{
    public interface IJournalImagesRepository
    {
        int Add(JournalImage image);
        JournalImage GetById(int id);
        JournalImage GetByModuleId(Guid entryId, string moduleId);
        List<JournalImage> GetAllByEntryId(Guid entryId);
        List<JournalImage> GetAllByJournalId(int journalId);
        void Update(JournalImage image);
        void Delete(int id);
        void DeleteByModuleId(Guid entryId, string moduleId);
        void DeleteAllByEntryId(Guid entryId);
    }
}
