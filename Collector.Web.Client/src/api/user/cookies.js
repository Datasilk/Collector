import { Api } from '@/api/Api';

const Cookies = (args) => Api({...args, useToken:true}).endpoints(({api}) => {
    const apiPath = '/api/cookies';
    return {
        checkYouTubeCookies: () => {
            return api.get(`${apiPath}/youtube-cookies-exist`);
        },
        uploadCookies: (file) => {
            const formData = new FormData();
            formData.append('file', file);
            
            return api.post(`${apiPath}/upload-cookies`, formData, {
                headers: {
                    'Content-Type': undefined
                },
                transformRequest: [(data) => data]
            });
        },
        deleteCookies: () => {
            return api.post(`${apiPath}/delete-cookies`);
        }
    };
});

export { Cookies }
