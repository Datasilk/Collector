import { Api } from '@/api/Api';

const JournalImages = (args) => Api({...args, useToken:true}).endpoints(({api}) => {
    const apiPath = '/api/journals/images';
    return {
        // Journal Images
        add: (image) => api.post(`${apiPath}/add`, image),
        getById: (id) => api.get(`${apiPath}/${id}`),
        getByModuleId: (entryId, moduleId) => api.get(`${apiPath}/entry/${entryId}/module/${moduleId}`),
        getAllByEntryId: (entryId) => api.get(`${apiPath}/entry/${entryId}`),
        getAllByJournalId: (journalId) => api.get(`${apiPath}/journal/${journalId}`),
        update: (image) => api.post(`${apiPath}/update`, image),
        delete: (entryId, moduleId) => api.post(`${apiPath}/delete/${entryId}/${moduleId}`),
        deleteByModuleId: (entryId, moduleId) => api.post(`${apiPath}/delete/entry/${entryId}/module/${moduleId}`),
        deleteAllByEntryId: (entryId) => api.post(`${apiPath}/delete/entry/${entryId}`)
    };
});

export { JournalImages };
