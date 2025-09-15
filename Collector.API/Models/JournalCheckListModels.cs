using System;
using System.Collections.Generic;

namespace Collector.API.Models
{
    public class JournalCheckListModel
    {
        public int Id { get; set; }
        public Guid AppUserId { get; set; }
        public Guid EntryId { get; set; }
        public int? ThemeId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Created { get; set; }
        public int Status { get; set; }
        public List<JournalCheckListItemModel> Items { get; set; }
    }

    public class JournalCheckListItemModel
    {
        public int Id { get; set; }
        public int CheckListId { get; set; }
        public string Title { get; set; }
        public int Icon { get; set; }
        public DateTime Created { get; set; }
        public int Status { get; set; }
    }

    public class AddCheckListModel
    {
        public Guid EntryId { get; set; }
        public int? ThemeId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }

    public class UpdateCheckListModel
    {
        public int Id { get; set; }
        public int? ThemeId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }

    public class UpdateCheckListTitleModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
    }

    public class UpdateCheckListDescriptionModel
    {
        public int Id { get; set; }
        public string Description { get; set; }
    }

    public class AddCheckListItemModel
    {
        public int CheckListId { get; set; }
        public string Title { get; set; }
        public int Icon { get; set; }
    }

    public class UpdateCheckListItemModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int Icon { get; set; }
    }

    public class UpdateCheckListItemTitleModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
    }

    public class UpdateCheckListItemIconModel
    {
        public int Id { get; set; }
        public int Icon { get; set; }
    }

    public class UpdateStatusModel
    {
        public int Id { get; set; }
        public int Status { get; set; }
    }
}
