using Collector.Data.Entities;
using System;
using System.Collections.Generic;

namespace Collector.Data.Interfaces
{
    public interface IJournalSnapshotsRepository
    {
        int Add(JournalSnapshot snapshot);
        JournalSnapshot GetById(int snapshotId);
        List<JournalSnapshot> GetAllByEntryId(Guid entryId);
        void Delete(int snapshotId);
    }
}
