using Collector.Common.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Collector.Data.Interfaces
{
    public interface IJournalCheckListsRepository
    {
        Task<JournalCheckList> GetById(int id);
        Task<List<JournalCheckList>> GetByEntryId(Guid entryId);
        Task<int> Add(JournalCheckList checkList);
        Task<bool> Update(JournalCheckList checkList);
        Task<bool> UpdateTitle(int id, string title);
        Task<bool> UpdateDescription(int id, string description);
        Task<bool> UpdateStatus(int id, int status);
        Task<bool> Delete(int id);
    }
}
