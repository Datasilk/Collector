import { Api } from '@/api/Api';

const Videos = (args) => Api({...args, useToken:true}).endpoints(({api}) => {
    return {
        uploadVideo: (journalId, entryId, moduleId, file, onProgress) => {
            const formData = new FormData();
            formData.append('file', file);
            
            console.log('Upload params:', { journalId, entryId, moduleId });
            console.log('FormData file:', file);
            
            return api.post(`/video-upload/${journalId}/${entryId}/${moduleId}`, formData, {
                headers: {
                    'Content-Type': undefined
                },
                transformRequest: [(data) => data],
                onUploadProgress: (progressEvent) => {
                    if (onProgress && progressEvent.total) {
                        const percentCompleted = Math.round((100 / progressEvent.total) * progressEvent.loaded);
                        onProgress(percentCompleted, progressEvent);
                    }
                }
            });
        },
        deleteVideo: (entryId, moduleId, deleteFiles = false) => {
            return api.post('/video-delete', { EntryId: entryId, ModuleId: moduleId, DeleteFiles: deleteFiles });
        }
    };
});

export { Videos }
