import { Api } from '@/api/Api';

const Domains = (args) => Api({...args, useToken:true}).endpoints(({api}) => {
    const apiPath = '/api/domains';
    return {
        // Domain Management
        getDomains: (filter) => api.post(`${apiPath}/list`, filter),
        getDomain: (id) => api.get(`${apiPath}/${id}`),
        getDomainInfo: (domain) => api.get(`${apiPath}/info/${domain}`),
        addDomain: (domain) => api.post(`${apiPath}/add`, domain),
        updateDomainInfo: (domainInfo) => api.post(`${apiPath}/update-info`, domainInfo),
        updateDomainType: (domainType) => api.post(`${apiPath}/update-type`, domainType),
        updateDomainType2: (domainType) => api.post(`${apiPath}/update-type2`, domainType),
        updateLanguage: (domainInfo) => api.post(`${apiPath}/update-language`, domainInfo),
        updateHttpsWww: (domainHttpsWww) => api.post(`${apiPath}/update-https-www`, domainHttpsWww),
        requireSubscription: (domainStatus) => api.post(`${apiPath}/require-subscription`, domainStatus),
        hasFreeContent: (domainStatus) => api.post(`${apiPath}/has-free-content`, domainStatus),
        isEmpty: (domainStatus) => api.post(`${apiPath}/is-empty`, domainStatus),
        isDeleted: (domainStatus) => api.post(`${apiPath}/is-deleted`, domainStatus),
        findDomainTitle: (id) => api.get(`${apiPath}/find-title/${id}`),
        findDescription: (id) => api.get(`${apiPath}/find-description/${id}`),
        getLinks: (id) => api.get(`${apiPath}/links/${id}`),
        deleteDomain: (id) => api.get(`${apiPath}/delete/${id}`),
        deleteAllArticles: (id) => api.get(`${apiPath}/delete-articles/${id}`),
        
        // Analyzer Rules
        getAnalyzerRules: (domainId) => api.get(`${apiPath}/analyzer-rules/${domainId}`),
        addAnalyzerRule: (rule) => api.post(`${apiPath}/analyzer-rules/add`, rule),
        removeAnalyzerRule: (ruleId) => api.get(`${apiPath}/analyzer-rules/remove/${ruleId}`),
        
        // Download Rules
        getDownloadRules: (domainId) => api.get(`${apiPath}/download-rules/${domainId}`),
        getDownloadRulesForDomains: (domains) => api.post(`${apiPath}/download-rules/for-domains`, domains),
        addDownloadRule: (rule) => api.post(`${apiPath}/download-rules/add`, rule),
        removeDownloadRule: (ruleId) => api.get(`${apiPath}/download-rules/remove/${ruleId}`),
        
        // Clean Downloads
        getDownloadsToClean: (domainId) => api.get(`${apiPath}/downloads-to-clean/${domainId}`),
        getTopDownloadsToClean: (domainId) => api.get(`${apiPath}/downloads-to-clean/${domainId}/top`),
        cleanDownloads: (domainId) => api.get(`${apiPath}/clean-downloads/${domainId}`),
        
        // Collections
        getCollections: () => api.get(`${apiPath}/collections`),
        addCollection: (collection) => api.post(`${apiPath}/collections/add`, collection),
        removeCollection: (colId) => api.get(`${apiPath}/collections/remove/${colId}`),
        
        // Collection Groups
        getCollectionGroups: () => api.get(`${apiPath}/collection-groups`),
        addCollectionGroup: (group) => api.post(`${apiPath}/collection-groups/add`, group),
        removeCollectionGroup: (groupId) => api.get(`${apiPath}/collection-groups/remove/${groupId}`),
        
        // Domain Type Matches
        getDomainTypeMatches: () => api.get(`${apiPath}/type-matches`),
        addDomainTypeMatch: (match) => api.post(`${apiPath}/type-matches/add`, match),
        removeDomainTypeMatch: (matchId) => api.get(`${apiPath}/type-matches/remove/${matchId}`)
    };
});

export { Domains }
