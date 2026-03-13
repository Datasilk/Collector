import { Api } from '@/api/Api';

const Ollama = (args) => Api({...args, useToken:true}).endpoints(({api}) => {
    const apiPath = '/api/admin/ollama';
    return {
		getAll: () => api.get(`${apiPath}/get-all`),
		getActive: () => api.get(`${apiPath}/get-active`),
		listAvailable: () => api.get(`${apiPath}/list-available`),
		add: (model) => api.post(`${apiPath}/add`, model),
		update: (model) => api.post(`${apiPath}/update`, model),
		setActive: (id) => api.post(`${apiPath}/set-active`, { id }),
		delete: (id) => api.post(`${apiPath}/delete`, { id })
	};
});

export { Ollama }
