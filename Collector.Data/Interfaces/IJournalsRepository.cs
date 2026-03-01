using Collector.Data.Entities;
using System.Collections.Generic;

namespace Collector.Data.Interfaces
{
    public interface IJournalsRepository
    {
        int Add(Journal journal);
        List<Journal> GetAllByUserId(Guid appUserId);
        Journal GetById(int journalId);
        Journal GetByUserIdAndStatus(Guid appUserId, int status);
        void Rename(int journalId, string title);
        void ChangeColor(int journalId, string color);
        void ChangeCategory(int journalId, int categoryId);
        void Archive(int journalId);
        void Unarchive(int journalId);
        void UpdateEntryId(int journalId, Guid? entryId);
    }
}
