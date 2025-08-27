import { Api } from '@/api/Api';

const Feeds = (args) => Api({...args, useToken:true}).endpoints(({api}) => {
    const apiPath = '/api/feeds';
    return {
        // Feed Management
        getList: () => api.get(`${apiPath}`),
        
        getFeed: (feedId) => api.get(`${apiPath}/${feedId}`),
        
        addFeed: (feed) => api.post(`${apiPath}/add`, {
            docType: feed.docType,
            categoryId: feed.categoryId,
            title: feed.title,
            url: feed.url,
            domain: feed.domain,
            filter: feed.filter || '',
            checkIntervals: feed.checkIntervals || 720
        }),
        
        logCheckedLinks: (feedId, count) => api.post(`${apiPath}/log-checked`, {
            feedId,
            count
        }),
        
        updateLastChecked: (feedId) => api.post(`${apiPath}/${feedId}/update-last-checked`),
        
        getListWithLogs: (days = 7, dateStart = null) => api.post(`${apiPath}/with-logs`, {
            days,
            dateStart
        }),
        
        // Categories
        getCategories: () => api.get(`${apiPath}/categories`),
        
        addCategory: (title) => api.post(`${apiPath}/categories/add`, { title }),
        
        // Feed Checking
        check: () => api.get(`${apiPath}/check`),
        
        checkFeed: (feedId) => api.get(`${apiPath}/check/${feedId}`)
    };
});

export { Feeds }
