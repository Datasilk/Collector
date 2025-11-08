import { Api } from '@/api/Api';

const JournalFiles = (args) => Api({...args, useToken:true}).endpoints(({api}) => {
    const apiPath = '/api/journals/files';
    return {
        // Journal Files
        add: (file) => api.post(`${apiPath}/add`, file),
        getById: (id) => api.get(`${apiPath}/${id}`),
        getByModuleId: (entryId, moduleId) => api.get(`${apiPath}/entry/${entryId}/module/${moduleId}`),
        getAllByEntryId: (entryId) => api.get(`${apiPath}/entry/${entryId}`),
        getAllByJournalId: (journalId) => api.get(`${apiPath}/journal/${journalId}`),
        update: (file) => api.post(`${apiPath}/update`, file),
        delete: (entryId, moduleId) => api.post(`${apiPath}/delete/${entryId}/${moduleId}`),
        deleteAllByEntryId: (entryId) => api.post(`${apiPath}/delete/entry/${entryId}`)
    };
});

export { JournalFiles };
