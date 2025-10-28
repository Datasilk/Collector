using Collector.Common.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Collector.Data.Interfaces
{
    public interface IJournalVideosRepository
    {
        Task<JournalVideo> GetById(int id);
        Task<JournalVideo> GetByModuleId(string moduleId);
        Task<JournalVideo> GetByUrl(string url);
        Task<List<JournalVideo>> GetByEntryId(Guid entryId);
        Task<List<JournalVideo>> GetByJournalId(int journalId);
        Task<int> Add(JournalVideo video);
        Task<bool> Update(JournalVideo video);
        Task<bool> UpdateTitle(int id, string title);
        Task<bool> UpdateDescription(int id, string description);
        Task<bool> UpdateDownloaded(int id, bool downloaded, string filename, int duration, int width, int height);
        Task<bool> Delete(int id);
        Task<bool> DeleteByModuleId(Guid entryId, string moduleId);
    }
}
