// import { checkAuth } from '@/api/account/auth';
import { UseAxios } from '@/api/Axios';

const appName = import.meta.env.VITE_APP_NAME.toLowerCase();
const key = 'session:user:' + appName;
const defaultUser = { fullname: 'Anonymous User', email: '' };

const getUser = () => {
    return localStorage.getItem(key) ? JSON.parse(localStorage.getItem(key)) : defaultUser;
};

const userContext = (user, setState) => {

    const setUser = (value) => {
        if (value != null) {
            const newUser = {
                ...value,
                isAdmin: value.roles?.some(a => a == 'admin') ?? false,
                isManager: value.roles?.some(a => a == 'manager') ?? false
            };
            setState(newUser);
            localStorage.setItem(key, JSON.stringify(newUser));
        } else {
            localStorage.removeItem(key);
            setState(null); 
        }
    }

    const checkUser = (role) => {
        const api = UseAxios({ user, setUser, useToken: user.isAdmin || user.isManager });
        let hasRole = false;
        switch (role) {
            case 'admin':
                hasRole = user.isAdmin;
                break;
            case 'manager':
                hasRole = user.isManager;
                break;
        }
        return api({ url: `api/auth/check-auth`, method: `GET` }).then(response => {
            if (!response.data.success || !hasRole) {
                localStorage.removeItem(key);
                setState(defaultUser);
            }
            return response;
        });
    }

    const hasRole = (role) =>  role == 'admin' && user.isAdmin || role == 'manager' && user.isManager || role == 'user';

    return { user, setUser, checkUser, hasRole };
}

export {
    getUser,
    userContext
}