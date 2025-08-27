import { Api } from '@/api/Api';

const Subjects = (args) => Api({...args, useToken:true}).endpoints(({api}) => {
    const apiPath = '/api/subjects';
    return {
        // Get subject by ID
        getSubject: (id) => {
            return api.get(`${apiPath}/${id}`);
        },
        
        // Get subject by title and breadcrumb
        getSubjectByTitle: (title, breadcrumb = '') => {
            const queryParams = new URLSearchParams({ 
                title,
                ...(breadcrumb ? { breadcrumb } : {})
            });
            return api.get(`${apiPath}/by-title?${queryParams.toString()}`);
        },
        
        // Get list of subjects by IDs and/or parent ID
        getSubjects: (subjectIds = '', parentId = -1) => {
            const queryParams = new URLSearchParams({
                subjectIds,
                parentId: parentId.toString()
            });
            return api.get(`${apiPath}/list?${queryParams.toString()}`);
        },
        
        // Get subjects by parent ID
        getSubjectsByParent: (parentId) => {
            return api.get(`${apiPath}/by-parent/${parentId}`);
        },
        
        // Create a new subject
        createSubject: (subject) => {
            return api.post(apiPath, subject);
        },
        
        // Move a subject to a new parent
        moveSubject: (subjectId, newParentId) => {
            return api.put(`${apiPath}/move`, { subjectId, newParentId });
        }
    };
});

export { Subjects }
