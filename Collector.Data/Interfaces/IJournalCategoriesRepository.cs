using Collector.Data.Entities;

namespace Collector.Data.Interfaces
{
    public interface IJournalCategoriesRepository
    {
        int Add(JournalCategory journalCategory);
        JournalCategory GetById(int journalCategoryId);
        List<JournalCategory> GetAllByUserId(Guid appUserId);
        List<JournalCategory> GetAllWithJournalsByUserId(Guid appUserId, int? sort = null, string search = null);
        void Rename(int journalCategoryId, string title);
        void ChangeColor(int journalCategoryId, string color);
        void Archive(int journalCategoryId);
        void Unarchive(int journalCategoryId);
    }
}
