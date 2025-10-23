using Collector.Common.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Collector.Data.Interfaces
{
    public interface IJournalCheckListItemsRepository
    {
        Task<JournalCheckListItem> GetById(int id);
        Task<List<JournalCheckListItem>> GetByCheckListId(int checkListId);
        Task<int> Add(JournalCheckListItem item);
        Task<bool> Update(JournalCheckListItem item);
        Task<bool> UpdateTitle(int id, string title);
        Task<bool> UpdateIcon(int id, int icon);
        Task<bool> UpdateStatus(int id, int status);
        Task<bool> Delete(int id);
        Task<bool> ResortItems(List<JournalCheckListItem> items);
    }
}
