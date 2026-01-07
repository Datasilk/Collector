import { Api } from '@/api/Api';

const Images = (args) => Api({...args, useToken:true}).endpoints(({api}) => {
    return {
        getImageUrl: (path) => {
            if (!path) return '';
            return `/image/${path}`;
        },

        upload: async (path, file) => {
            if (!path || !file) {
                return { data: { success: false, message: 'Path and file are required' } };
            }

            const formData = new FormData();
            formData.append('file', file);

            return api.post(`/image-upload/${path}`, formData, {
                headers: {
                    'Content-Type': 'multipart/form-data'
                }
            });
        },

        uploadBatch: async (path, files, { moduleId } = {}) => {
            if (!path || !files || files.length === 0) {
                return { data: { success: false, message: 'Path and files are required' } };
            }

            const formData = new FormData();
            files.forEach(file => formData.append('files', file));

            const query = moduleId ? `?moduleId=${encodeURIComponent(moduleId)}` : '';

            return api.post(`/image-upload/batch/${path}${query}`, formData, {
                headers: {
                    'Content-Type': 'multipart/form-data'
                }
            });
        }
    };
});

export { Images }
