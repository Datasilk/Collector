import { Api } from '@/api/Api';

const LLMs = (args) => Api({...args, useToken:true}).endpoints(({api}) => {
    const apiPath = '/api/llms';
    return {
        getAvailable: () => api.get(`${apiPath}/available`)
    };
});

export { LLMs };
