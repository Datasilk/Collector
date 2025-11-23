using Collector.Data.Entities;
using System;
using System.Collections.Generic;

namespace Collector.Data.Interfaces
{
    public interface IJournalEntryTagsRepository
    {
        void Add(JournalEntryTag entryTag);
        void Remove(int tagId, Guid journalEntryId);
        void RemoveByEntry(Guid journalEntryId);
        List<JournalEntryTag> GetByEntryId(Guid journalEntryId);
        List<int> GetTagIdsByEntry(Guid journalEntryId);
        List<JournalEntryTag> GetByTagId(int tagId);
    }
}
