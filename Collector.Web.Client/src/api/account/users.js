import { Api } from '@/api/Api';

const Users = (args) => Api({...args, useToken:true}).endpoints(({api}) => {
    const apiPath = '/api/users';
    return {
        addUser: (user) => api.post(`${apiPath}/add`, user),
        getUsers: () => api.get(`${apiPath}/get-all-manager`),
        getById: (userId) => api.get(`${apiPath}/get/${userId}`),
        getMyInfo: () => api.get(`${apiPath}/my-info`),
        edit: (user) => api.post(`${apiPath}/edit`, user),
        resetPassword: (user) => api.post(`${apiPath}/reset-password`, user),
        updateEmail: (user) => api.post(`${apiPath}/update-email`, user)
    };
});

export { Users }