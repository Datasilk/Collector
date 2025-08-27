import { Api } from '@/api/Api';

const Articles = (args) => Api({...args, useToken:true}).endpoints(({api}) => {
    const apiPath = '/api/articles';
    return {
        getArticles: (params = {}) => {
            // Convert dates to ISO strings
            const payload = { ...params };
            
            if (payload.dateStart instanceof Date) {
                payload.dateStart = payload.dateStart.toISOString();
            }
            
            if (payload.dateEnd instanceof Date) {
                payload.dateEnd = payload.dateEnd.toISOString();
            }
            
            // Ensure subjectIds is an array
            if (!payload.subjectIds) {
                payload.subjectIds = [];
            }
            
            return api.post(`${apiPath}/get-articles`, payload);
        },
        
        getArticle: (id) => api.get(`${apiPath}/${id}`),
        
        getArticleByUrl: (url) => {
            const queryParams = new URLSearchParams({ url });
            return api.get(`${apiPath}/url?${queryParams.toString()}`);
        },
        
        articleExists: (url) => {
            const queryParams = new URLSearchParams({ url });
            return api.get(`${apiPath}/exists?${queryParams.toString()}`);
        },
        
        createArticle: (article) => api.post(apiPath, article),
        
        updateArticle: (article) => api.put(apiPath, article),
        
        updateArticleUrl: (data) => api.put(`${apiPath}/url`, data),
        
        updateArticleCache: (articleId, cached) => api.put(`${apiPath}/cache`, { articleId, cached }),
        
        markArticleVisited: (id) => api.put(`${apiPath}/visited/${id}`),
        
        removeArticle: (id) => api.delete(`${apiPath}/${id}`),
        
        cleanArticle: (id) => api.delete(`${apiPath}/clean/${id}`),
        
        // Dates, sentences, subjects, words, etc
        addArticleDate: (data) => {
            const payload = {
                ...data,
                date: data.date instanceof Date ? data.date.toISOString() : data.date
            };
            return api.post(`${apiPath}/date`, payload);
        },
        
        addArticleSentence: (data) => api.post(`${apiPath}/sentence`, data),
        
        removeArticleSentences: (articleId) => api.delete(`${apiPath}/sentences/${articleId}`),
        
        addArticleSubject: (data) => {
            const payload = { ...data };
            if (data.datePublished instanceof Date) {
                payload.datePublished = data.datePublished.toISOString();
            }
            return api.post(`${apiPath}/subject`, payload);
        },
        
        removeArticleSubjects: (articleId, subjectId = 0) => {
            const queryParams = subjectId ? new URLSearchParams({ subjectId }) : '';
            const url = queryParams ? 
                `${apiPath}/subjects/${articleId}?${queryParams.toString()}` : 
                `${apiPath}/subjects/${articleId}`;
            return api.delete(url);
        },
        
        addArticleWord: (data) => api.post(`${apiPath}/word`, data),
        
        removeArticleWords: (articleId, word = '') => {
            const queryParams = word ? new URLSearchParams({ word }) : '';
            const url = queryParams ? 
                `${apiPath}/words/${articleId}?${queryParams.toString()}` : 
                `${apiPath}/words/${articleId}`;
            return api.delete(url);
        }
    };
});

export { Articles }
