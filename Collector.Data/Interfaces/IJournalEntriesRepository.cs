using Collector.Data.Entities;
using System;
using System.Collections.Generic;

namespace Collector.Data.Interfaces
{
    public interface IJournalEntriesRepository
    {
        Guid Add(JournalEntry journalEntry);
        JournalEntry GetById(Guid journalEntryId);
        List<JournalEntry> GetAllByJournalId(int journalId);
        JournalEntryFilterResult Filter(int journalId, string search, string sort, int start, int length, List<int> tagIds);
        void Rename(Guid journalEntryId, string title);
        void UpdateDescription(Guid journalEntryId, string description);
        void Archive(Guid journalEntryId);
        void Unarchive(Guid journalEntryId);
        void Publish(Guid journalEntryId);
        void Modify(Guid journalEntryId);
        void UpdateJournalId(Guid journalEntryId, int journalId);
        void UpdateLastModified(Guid journalEntryId);
        void UpdateCreated(Guid journalEntryId, DateTime created);
        void SetEncrypted(Guid journalEntryId, bool encrypted);
        void SetPublished(Guid journalEntryId, bool isPublished);
        void SetChapter(Guid journalEntryId, int? chapterId);
        void UpdateThumbnail(Guid journalEntryId, string thumbnail);
    }
}
