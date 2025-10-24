using Collector.Data.Entities;

namespace Collector.Data.Interfaces
{
    public interface IJournalChaptersRepository
    {
        void Add(JournalChapter chapter);
        JournalChapter GetById(int journalId, int chapterId);
        List<JournalChapter> GetAllByJournalId(int journalId);
        void Rename(int journalId, int chapterId, string title);
        void UpdateDescription(int journalId, int chapterId, string description);
        void ChangeColor(int journalId, int chapterId, int color);
        void ChangeIcon(int journalId, int chapterId, int icon);
        void UpdateSort(int journalId, int chapterId, int sort);
        void Delete(int journalId, int chapterId);
    }
}
