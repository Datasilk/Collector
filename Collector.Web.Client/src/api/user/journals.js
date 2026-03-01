import { Api } from '@/api/Api';

const Journals = (args) => Api({...args, useToken:true}).endpoints(({api}) => {
    const apiPath = '/api/journals';
    return {
        // Journal Categories
        getCategories: (sort = null, search = null) => {
            return api.post(`${apiPath}/categories`, { Sort: sort, Search: search });
        },
        filterCategories: (filter) => api.post(`${apiPath}/categories/filter`, filter),
        addCategory: (category) => api.post(`${apiPath}/categories/add`, category),
        renameCategory: (id, title) => api.post(`${apiPath}/categories/rename`, { Id: id, Title: title }),
        changeCategoryColor: (id, color) => api.post(`${apiPath}/categories/change-color`, { Id: id, Color: color }),
        archiveCategory: (id) => api.get(`${apiPath}/categories/archive/${id}`),
        unarchiveCategory: (id) => api.get(`${apiPath}/categories/unarchive/${id}`),
        
        // Journals
        getJournals: () => api.get(apiPath),
        getJournal: (id) => api.get(`${apiPath}/${id}`),
        getOrCreateCustomModulesJournal: () => api.get(`${apiPath}/custom-modules-journal`),
        addJournal: (journal) => api.post(`${apiPath}/add`, journal),
        renameJournal: (id, title) => api.post(`${apiPath}/rename`, { Id: id, Title: title }),
        changeJournalColor: (id, color) => api.post(`${apiPath}/change-color`, { Id: id, Color: color }),
        changeJournalCategory: (id, categoryId) => api.post(`${apiPath}/change-category`, { Id: id, CategoryId: categoryId }),
        archiveJournal: (id) => api.get(`${apiPath}/archive/${id}`),
        unarchiveJournal: (id) => api.get(`${apiPath}/unarchive/${id}`),
        updateJournalEntryId: (journalId, entryId) => api.post(`${apiPath}/update-entry-id`, { JournalId: journalId, EntryId: entryId }),
        
        // Journal Entries
        getEntries: (journalId) => api.get(`${apiPath}/${journalId}/entries`),
        filterEntries: (journalId, filter) => api.post(`${apiPath}/${journalId}/entries/filter`, filter),
        getEntry: (id) => api.get(`${apiPath}/entries/${id}`),
        getEntryContent: (id) => api.get(`${apiPath}/entries/${id}/content`),
        addEntry: (entry) => api.post(`${apiPath}/entries/add`, entry),
        renameEntry: (id, title) => api.post(`${apiPath}/entries/rename`, { Id: id, Title: title }),
        updateEntryDescription: (id, description) => api.post(`${apiPath}/entries/update-description`, { Id: id, Description: description }),
        updateEntryContent: (id, content) => api.post(`${apiPath}/entries/update-entry`, { Id: id, Content: content }),
        updateEntryCreated: (id, created) => api.post(`${apiPath}/entries/update-created`, { Id: id, Created: created }),
        setEntryParent: (id, parentEntryId) => api.post(`${apiPath}/entries/set-parent`, { Id: id, ParentEntryId: parentEntryId }),
        archiveEntry: (id) => api.get(`${apiPath}/entries/archive/${id}`),
        unarchiveEntry: (id) => api.get(`${apiPath}/entries/unarchive/${id}`),
        publishEntry: (id) => api.get(`${apiPath}/entries/publish/${id}`),
        modifyEntry: (id) => api.get(`${apiPath}/entries/modify/${id}`),
        moveEntry: (entryId, targetJournalId) => api.post(`${apiPath}/entry/move`, { EntryId: entryId, TargetJournalId: targetJournalId }),
        setEntryEncrypted: (id, isSet) => api.post(`${apiPath}/entries/set-encrypted`, { Id: id, IsSet: isSet }),
        setEntryPublished: (id, isSet) => api.post(`${apiPath}/entries/set-published`, { Id: id, IsSet: isSet }),
        setEntryChapter: (id, chapterId) => api.post(`${apiPath}/entries/set-chapter`, { Id: id, ChapterId: chapterId }),
        updateEntryThumbnail: (id, thumbnail, thumbnailModuleId = null, sourceEntryId = null) => api.post(`${apiPath}/entries/update-thumbnail`, { Id: id, Thumbnail: thumbnail, ThumbnailModuleId: thumbnailModuleId, SourceEntryId: sourceEntryId }),
       
        // Journal Settings
        getJournalSettings: (journalId) => api.get(`${apiPath}/settings/${journalId}`),
        updateJournalSettings: (journalId, settings) => api.post(`${apiPath}/settings/update`, { JournalId: journalId, Css: settings.css }),
        
        // Journal Chapters
        addChapter: (journalId, chapter) => api.post(`${apiPath}/${journalId}/chapters/add`, chapter),
        getChapters: (journalId) => api.get(`${apiPath}/${journalId}/chapters`),
        getChapter: (journalId, chapterId) => api.get(`${apiPath}/${journalId}/chapters/${chapterId}`),
        renameChapter: (journalId, chapterId, title) => api.post(`${apiPath}/chapters/rename`, { JournalId: journalId, ChapterId: chapterId, Title: title }),
        updateChapterDescription: (journalId, chapterId, description) => api.post(`${apiPath}/chapters/update-description`, { JournalId: journalId, ChapterId: chapterId, Description: description }),
        changeChapterColor: (journalId, chapterId, color) => api.post(`${apiPath}/chapters/change-color`, { JournalId: journalId, ChapterId: chapterId, Color: color }),
        changeChapterIcon: (journalId, chapterId, icon) => api.post(`${apiPath}/chapters/change-icon`, { JournalId: journalId, ChapterId: chapterId, Icon: icon }),
        updateChapterSort: (journalId, chapterId, sort) => api.post(`${apiPath}/chapters/update-sort`, { JournalId: journalId, ChapterId: chapterId, Sort: sort }),
        deleteChapter: (journalId, chapterId) => api.post(`${apiPath}/chapters/delete`, { JournalId: journalId, ChapterId: chapterId })
    };
});

export { Journals }
