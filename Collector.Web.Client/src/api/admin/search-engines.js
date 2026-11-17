import { Api } from '@/api/Api';

const SearchEngines = (args) => Api({...args, useToken:true}).endpoints(({api}) => {
    const apiPath = '/api/admin/search-engines';
    return {
		getAll: () => api.get(`${apiPath}/get-all`),
		getById: (id) => api.get(`${apiPath}/get-by-id?id=${id}`),
		add: (engine) => api.post(`${apiPath}/add`, engine),
		update: (engine) => api.post(`${apiPath}/update`, engine),
		setEnabled: (id, enabled) => api.post(`${apiPath}/set-enabled`, { id, enabled }),
		reorder: (ids) => api.post(`${apiPath}/reorder`, { ids }),
		delete: (id) => api.post(`${apiPath}/delete`, { id })
	};
});

export { SearchEngines }
