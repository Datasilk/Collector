import { Api } from '@/api/Api';

const JournalSnapshots = (args) => Api({...args, useToken:true}).endpoints(({api}) => {
    const apiPath = '/api/journal-snapshots';
    return {
        getSnapshotsByEntry: (entryId) => api.get(`${apiPath}/entry/${entryId}`),
        getSnapshot: (id) => api.get(`${apiPath}/${id}`),
        createSnapshot: (entryId) => api.post(`${apiPath}/create`, { EntryId: entryId }),
        deleteSnapshot: (id) => api.get(`${apiPath}/delete/${id}`)
    };
});

export { JournalSnapshots }
