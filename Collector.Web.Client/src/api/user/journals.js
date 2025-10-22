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
        moveEntry: (entryId, targetJournalId) => api.post(`${apiPath}/entry/move`, { EntryId: entryId, TargetJournalId: targetJournalId }),
        setEntryEncrypted: (id, isSet) => api.post(`${apiPath}/entries/set-encrypted`, { Id: id, IsSet: isSet }),
        setEntryPublished: (id, isSet) => api.post(`${apiPath}/entries/set-published`, { Id: id, IsSet: isSet }),
        
        // Modules
        addModule: (module) => api.post(`${apiPath}/modules/add`, module),
        getModulesByJournal: (journalId) => api.get(`${apiPath}/modules/journal/${journalId}`),
        getModulesByEntry: (entryId) => api.get(`${apiPath}/modules/entry/${entryId}`),
        updateModule: (module) => api.post(`${apiPath}/modules/update`, module),
        deleteModule: (journalId, entryId, moduleId) => api.post(`${apiPath}/modules/delete`, { JournalId: journalId, EntryId: entryId, ModuleId: moduleId }),
        resortModules: (journalId, modules) => api.post(`${apiPath}/modules/resort`, { JournalId: journalId, Modules: modules }),
        
        // Journal Settings
        getJournalSettings: (journalId) => api.get(`${apiPath}/settings/${journalId}`),
        updateJournalSettings: (journalId, settings) => api.post(`${apiPath}/settings/update`, { JournalId: journalId, Css: settings.css })
    };
});

export { Journals }
