import { Api } from '@/api/Api';

const Whitelists = (args) => Api({...args, useToken:true}).endpoints(({api}) => {
    const apiPath = '/api/whitelists';
    return {
        // Get all whitelisted domains
        getDomains: () => {
            return api.get(`${apiPath}/domains`);
        },
        
        // Check if a domain is whitelisted
        checkDomain: (domain) => {
            const queryParams = new URLSearchParams({ domain });
            return api.get(`${apiPath}/check?${queryParams.toString()}`);
        },
        
        // Add a domain to whitelist
        addDomain: (domain) => {
            return api.post(`${apiPath}/domain`, { domain });
        },
        
        // Remove a domain from whitelist
        removeDomain: (domain) => {
            return api.delete(`${apiPath}/domain`, { data: { domain } });
        }
    };
});

export { Whitelists }
