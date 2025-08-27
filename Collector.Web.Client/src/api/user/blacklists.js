import { Api } from '@/api/Api';

const Blacklists = (args) => Api({...args, useToken:true}).endpoints(({api}) => {
    const apiPath = '/api/blacklists';
    return {
        // Domains
        getDomainsList: () => api.get(`${apiPath}/domains`),
        
        addDomain: (domain) => api.post(`${apiPath}/domains`, { domain }),
        
        removeDomain: (domain) => api.delete(`${apiPath}/domains`, { 
            data: { domain } 
        }),
        
        checkDomain: (domain) => api.get(`${apiPath}/domains/check/${domain}`),
        
        checkAllDomains: (domains) => api.post(`${apiPath}/domains/check-all`, { domains }),
        
        // Wildcards
        getWildcardsList: () => api.get(`${apiPath}/wildcards`),
        
        addWildcard: (domain) => api.post(`${apiPath}/wildcards`, { domain }),
        
        removeWildcard: (domain) => api.delete(`${apiPath}/wildcards`, { 
            data: { domain } 
        })
    };
});

export { Blacklists }
