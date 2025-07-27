import { Api } from '@/api/Api';

const Users = (args) => Api({...args, useToken:true}).endpoints(({api}) => {
    const apiPath = '/api/admin/users';
    return {
		getUsers: () => api.get(`${apiPath}/get-all`),
        getUsersFiltered: ({ fullName, role, sort }) => {
            return api.get(`${apiPath}/get-all-filtered?fullName=${fullName}&role=${role}&sort=${sort}`);
        },
		getRoles: () => api.get(`${apiPath}/get-roles`),
		getById: (userId) => api.get(`${apiPath}/get/${userId}`),
        delete: (userId) => api.get(`${apiPath}/delete/${userId}`),
        edit: (user) => api.post(`${apiPath}/edit`, user),
        lockUser: (userId, lock) => {
            return api.post(`${apiPath}/update-lock`, {
                UserId: userId,
                lockUser: lock
            });
        },
        add: (user) => api.post(`${apiPath}/add`, user),
        resetPassword: (user) => api.post(`${apiPath}/reset-password`, user),
        updateEmail: (user) => api.post(`${apiPath}/update-email`, user)
	};
});

export { Users }