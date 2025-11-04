import { Api } from '@/api/Api';

const JournalChecklists = (args) => Api({...args, useToken:true}).endpoints(({api}) => {
    const apiPath = '/api/journal-checklists';
    return {
        // Checklists
        getChecklist: (id) => api.get(`${apiPath}/${id}`),
        getChecklistsByEntry: (entryId) => api.get(`${apiPath}/entry/${entryId}`),
        addChecklist: (checklist) => api.post(`${apiPath}/add`, checklist),
        updateChecklist: (checklist) => api.post(`${apiPath}/update`, checklist),
        updateChecklistTitle: (id, title) => api.post(`${apiPath}/update-title`, { Id: id, Title: title }),
        updateChecklistDescription: (id, description) => api.post(`${apiPath}/update-description`, { Id: id, Description: description }),
        updateChecklistEntryId: (id, entryId) => api.post(`${apiPath}/update-entry-id`, { Id: id, EntryId: entryId }),
        updateChecklistStatus: (id, status) => api.post(`${apiPath}/update-status`, { Id: id, Status: status }),
        deleteChecklist: (id) => api.delete(`${apiPath}/${id}`),

        // Checklist Items
        getChecklistItem: (id) => api.get(`${apiPath}/items/${id}`),
        getChecklistItems: (checklistId) => api.get(`${apiPath}/${checklistId}/items`),
        addChecklistItem: (item) => api.post(`${apiPath}/items/add`, item),
        updateChecklistItem: (item) => api.post(`${apiPath}/items/update`, item),
        updateChecklistItemTitle: (id, title) => api.post(`${apiPath}/items/update-title`, { Id: id, Title: title }),
        updateChecklistItemIcon: (id, icon) => api.post(`${apiPath}/items/update-icon`, { Id: id, Icon: icon }),
        updateChecklistItemStatus: (id, status) => api.post(`${apiPath}/items/update-status`, { Id: id, Status: status }),
        deleteChecklistItem: (id) => api.delete(`${apiPath}/items/${id}`),
        resortChecklistItems: (items) => api.post(`${apiPath}/items/resort`, { Items: items })
    };
});

export { JournalChecklists }
