import { Api } from '@/api/Api';

const Chats = (args) => Api({...args, useToken:true}).endpoints(({api}) => {
    const apiPath = '/api/chats';
    return {
        list: (start = 0, length = 20) => api.get(`${apiPath}/list?start=${start}&length=${length}`),
        getHistory: (chatId) => api.get(`${apiPath}/${chatId}/history`)
    };
});

export { Chats };
