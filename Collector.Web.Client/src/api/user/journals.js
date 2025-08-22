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
        addJournal: (journal) => api.post(`${apiPath}/add`, journal),
        renameJournal: (id, title) => api.post(`${apiPath}/rename`, { Id: id, Title: title }),
        changeJournalColor: (id, color) => api.post(`${apiPath}/change-color`, { Id: id, Color: color }),
        archiveJournal: (id) => api.get(`${apiPath}/archive/${id}`),
        unarchiveJournal: (id) => api.get(`${apiPath}/unarchive/${id}`),
        
        // Journal Entries
        getEntries: (journalId) => api.get(`${apiPath}/${journalId}/entries`),
        getEntry: (id) => api.get(`${apiPath}/entries/${id}`),
        getEntryContent: (id) => api.get(`${apiPath}/entries/${id}/content`),
        addEntry: (entry) => api.post(`${apiPath}/entries/add`, entry),
        renameEntry: (id, title) => api.post(`${apiPath}/entries/rename`, { Id: id, Title: title }),
        updateEntryDescription: (id, description) => api.post(`${apiPath}/entries/update-description`, { Id: id, Description: description }),
        updateEntryContent: (id, content) => api.post(`${apiPath}/entries/update-entry`, { Id: id, Content: content }),
        archiveEntry: (id) => api.get(`${apiPath}/entries/archive/${id}`),
        unarchiveEntry: (id) => api.get(`${apiPath}/entries/unarchive/${id}`),
        publishEntry: (id) => api.get(`${apiPath}/entries/publish/${id}`),
        modifyEntry: (id) => api.get(`${apiPath}/entries/modify/${id}`),
        moveEntry: (entryId, targetJournalId) => api.post(`${apiPath}/entry/move`, { EntryId: entryId, TargetJournalId: targetJournalId })
    };
});

export { Journals }
