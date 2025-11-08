import { Api } from '@/api/Api';

const Files = (args) => Api({...args, useToken:true}).endpoints(({api}) => {
    return {
        upload: async (path, file) => {
            if (!path || !file) {
                return { data: { success: false, message: 'Path and file are required' } };
            }

            const formData = new FormData();
            formData.append('file', file);

            return api.post(`/file-upload/${path}`, formData, {
                headers: {
                    'Content-Type': 'multipart/form-data'
                }
            });
        }
    };
});

export { Files }
