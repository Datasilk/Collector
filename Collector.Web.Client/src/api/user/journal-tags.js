import { Api } from '@/api/Api';

const JournalTags = (args) => Api({ ...args, useToken: true }).endpoints(({ api }) => {
    const journalPath = '/api/journals';
    const entryPath = '/api/journal-entries';

    return {
        getTags: (journalId) => api.get(`${journalPath}/${journalId}/tags`),
        searchTags: (journalId, search, limit = 10) =>
            api.post(`${journalPath}/${journalId}/tags/search`, {
                Search: search,
                Limit: limit
            }),
        createOrGetTag: (journalId, tag) =>
            api.post(`${journalPath}/${journalId}/tags/create-or-get`, {
                Tag: tag
            }),
        getEntryTags: (entryId) => api.get(`${entryPath}/${entryId}/tags`),
        addTagToEntry: (entryId, tagId) =>
            api.post(`${entryPath}/${entryId}/tags/add`, {
                TagId: tagId
            }),
        removeTagFromEntry: (entryId, tagId) =>
            api.post(`${entryPath}/${entryId}/tags/remove`, {
                TagId: tagId
            })
    };
});

export { JournalTags };
