import { Api } from '@/api/Api';

const Whitelists = (args) => Api({...args, useToken:true}).endpoints(({api}) => {
    const apiPath = '/api/admin/whitelists';
    return {
        // Get all whitelists
        getWhitelists: (filters) => {
            const queryParams = new URLSearchParams({
                search: filters.search || '',
                status: filters.status || 0,
                sort: filters.sort || 'Name ASC'
            });
            return api.get(`${apiPath}/list?${queryParams.toString()}`);
        },
        
        // Get whitelist by ID
        getWhitelist: (id) => {
            return api.get(`${apiPath}/${id}`);
        },
        
        // Create a new whitelist
        createWhitelist: (whitelist) => {
            return api.post(apiPath, whitelist);
        },
        
        // Update an existing whitelist
        updateWhitelist: (whitelist) => {
            return api.put(`${apiPath}/${whitelist.id}`, whitelist);
        },
        
        // Delete a whitelist
        deleteWhitelist: (id) => {
            return api.delete(`${apiPath}/${id}`);
        },
        
        // Add domain to whitelist
        addDomain: (whitelistId, domain) => {
            return api.post(`${apiPath}/${whitelistId}/domains`, { domain });
        },
        
        // Remove domain from whitelist
        removeDomain: (whitelistId, domain) => {
            return api.delete(`${apiPath}/${whitelistId}/domains`, { data: { domain } });
        }
    };
});

export { Whitelists }
