import { Api } from '@/api/Api';

const Whitelists = (args) => Api({...args, useToken:true}).endpoints(({api}) => {
    const apiPath = '/api/admin/whitelists';
    return {
        // Get paged list of whitelisted domains
        getWhitelists: (filters) => {
            const queryParams = new URLSearchParams({
                search: filters.search || '',
                status: filters.status || 0,
                sort: filters.sort || 'Name ASC',
                start: filters.start || 1,
                length: filters.length || 100
            });
            return api.get(`${apiPath}/list?${queryParams.toString()}`);
        },

        // Add a new domain to the whitelist
        createWhitelist: (domain) => {
            return api.post(apiPath, { domain });
        },

        // Remove a domain from the whitelist
        deleteWhitelist: (domain) => {
            return api.delete(`${apiPath}/${encodeURIComponent(domain)}`);
        }
    };
});

export { Whitelists }
