import { Api } from '@/api/Api';

const Downloads = (args) => Api({...args, useToken:true}).endpoints(({api}) => {
    const apiPath = '/api/downloads';
    return {
        // Get count of items in the download queue
        getCount: () => api.get(`${apiPath}/count`),
        
        // Check the download queue for the next item to process
        checkQueue: (params = {}) => {
            const payload = {
                feedId: params.feedId || 0,
                domain: params.domain || '',
                domainDelay: params.domainDelay || 60,
                sort: params.sort || 0 // QueueSort.Newest
            };
            return api.post(`${apiPath}/check`, payload);
        },
        
        // Add a single item to the download queue
        addQueueItem: (url, domain, parentId = 0, feedId = 0) => {
            return api.post(`${apiPath}/add`, {
                url,
                domain,
                parentId,
                feedId
            });
        },
        
        // Add multiple items to the download queue
        addQueueItems: (urls, domain, parentId = 0, feedId = 0) => {
            return api.post(`${apiPath}/add-bulk`, {
                urls,
                domain,
                parentId,
                feedId
            });
        },
        
        // Update the status of a queue item
        updateQueueItemStatus: (queueId, status) => {
            return api.put(`${apiPath}/update-status`, {
                queueId,
                status
            });
        },
        
        // Update the URL of a queue item
        updateUrl: (queueId, url, domain) => {
            return api.put(`${apiPath}/update-url`, {
                queueId,
                url,
                domain
            });
        },
        
        // Delete a queue item
        deleteQueueItem: (queueId) => api.delete(`${apiPath}/${queueId}`),
        
        // Move a queue item to the downloads table
        moveQueueItem: (queueId) => api.post(`${apiPath}/${queueId}/move`),
        
        // Archive a queue item
        archiveQueueItem: (queueId) => api.post(`${apiPath}/${queueId}/archive`),
        
        // Move all archived queue items to the downloads table
        moveArchived: () => api.post(`${apiPath}/move-archived`)
    };
});

export { Downloads }
