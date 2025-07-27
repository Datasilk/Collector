import { UseAxios } from '@/api/Axios';

const Api = (args) => {
    //args should be useSession() or at the very least, {user, setUser} from useSession()
    const api = UseAxios(args);

    const cache = {
        get: (key, args, getApiResponse) => {
            const sessionKey = 'api:cache:' + key;
            let cache = [];
            if (sessionStorage.getItem(sessionKey)) {
                cache = JSON.parse(sessionStorage.getItem(sessionKey));
                const result = cache.filter(a => {
                    return Object.keys(args).every(key =>
                        a.args[key] === args[key]
                    ) && a.response != null;
                });
                if (result.length > 0) return new Promise((resolve) =>
                    resolve({ data: { success: true, data: result[0].response } })
                );
            }

            const result = getApiResponse();
            result.then((response) => {
                if (response.data && response.data.success && response.data.data != null) {
                    cache.push({ response: response.data.data, args: args });
                    sessionStorage.setItem(sessionKey, JSON.stringify(cache));
                }
            });
            return result;
        },
        invalidate: (key) => {
            const sessionKey = 'api:cache:' + key;
            sessionStorage.removeItem(sessionKey);
        },
        invalidateArgs: (key, args) => {
            const sessionKey = 'api:cache:' + key;
            let cache = [];
            if (sessionStorage.getItem(sessionKey)) {
                cache = JSON.parse(sessionStorage.getItem(sessionKey));
                cache = cache.filter(a => {
                    // remove any cache items that match all args properties
                    return !Object.keys(args).every(key =>
                        a.args[key] === args[key]
                    ) && a.response != null;
                });
                sessionStorage.setItem(sessionKey, JSON.stringify(cache));
            }
        },
        invalidateAll: () => {
            Object.keys(sessionStorage).forEach(key => {
                if (key.startsWith('api:cache:')) {
                    sessionStorage.removeItem(key);
                }
            });
        }
    }

    return { endpoints: (callback) => callback({ api, cache, ...args }) };
}

export { Api }