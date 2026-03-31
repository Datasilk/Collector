import { useState, useEffect } from 'react';
//components
import Input from '@/components/forms/input';
import TextArea from '@/components/forms/textarea';
import Modal from '@/components/ui/modal';
import Icon from '@/components/ui/icon';
//context
import { useSession } from '@/context/session';
//api
import { JournalChecklists } from '@/api/user/journalChecklists';

export default function ChecklistSettingsModal({ checklistId, title, description, items, module, onUpdate, onSaved, onLoad }) {
    const [tempTitle, setTempTitle] = useState(title || '');
    const [tempDescription, setTempDescription] = useState(description || '');
    const [isSaving, setIsSaving] = useState(false);
    const [showLoadSection, setShowLoadSection] = useState(false);
    const [searchText, setSearchText] = useState('');
    const [checklists, setChecklists] = useState([]);
    const [isLoading, setIsLoading] = useState(false);
    const [searchTimer, setSearchTimer] = useState(null);
    const [showConfirmation, setShowConfirmation] = useState(false);
    const [selectedChecklist, setSelectedChecklist] = useState(null);

    const session = useSession();
    const {
        updateChecklistTitle,
        updateChecklistDescription,
        filterChecklists,
        getChecklist
    } = JournalChecklists(session);

    const handleClose = () => {
        session.hideModal();
    };

    const handleSave = async () => {
        if (!checklistId || isSaving) return;

        setIsSaving(true);
        try {
            await updateChecklistTitle(checklistId, tempTitle);
            await updateChecklistDescription(checklistId, tempDescription);

            if (onUpdate && module) {
                onUpdate({
                    ...module,
                    checklist: {
                        items: items || [],
                        title: tempTitle,
                        description: tempDescription
                    }
                });
            }

            if (onSaved) {
                onSaved(tempTitle, tempDescription);
            }

            session.hideModal();
        } catch (err) {
            console.error('Error saving checklist settings:', err);
        } finally {
            setIsSaving(false);
        }
    };

    const handleTitleChange = (e) => {
        setTempTitle(e.target.value);
    };

    const handleDescriptionInput = (e) => {
        setTempDescription(e.target.value);
    };

    const handleLoadButtonClick = () => {
        setShowLoadSection(true);
        if (checklists.length === 0) {
            loadChecklists('');
        }
    };

    const loadChecklists = async (search) => {
        setIsLoading(true);
        try {
            const response = await filterChecklists(search, 5);
            if (response.data.success) {
                setChecklists(response.data.data || []);
            }
        } catch (err) {
            console.error('Error loading checklists:', err);
        } finally {
            setIsLoading(false);
        }
    };

    const handleSearchInput = (e) => {
        const value = e.target.value;
        setSearchText(value);
        
        if (searchTimer) {
            clearTimeout(searchTimer);
        }
        
        const timer = setTimeout(() => {
            loadChecklists(value);
        }, 300);
        
        setSearchTimer(timer);
    };

    const handleLoadChecklist = async (id) => {
        try {
            const response = await getChecklist(id);
            if (response.data.success && response.data.data) {
                const checklist = response.data.data;
                setSelectedChecklist(checklist);
                setShowConfirmation(true);
            }
        } catch (err) {
            console.error('Error loading checklist:', err);
        }
    };

    const handleConfirmLoad = () => {
        if (selectedChecklist && onSaved) {
            onSaved(selectedChecklist.title, selectedChecklist.description, selectedChecklist.items, selectedChecklist.id);
        }
        session.hideModal();
    };

    const handleCancelLoad = () => {
        setShowConfirmation(false);
        setSelectedChecklist(null);
    };

    const formatDate = (dateString) => {
        if (!dateString) return '';
        const date = new Date(dateString);
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    };

    return (
        <Modal title="Checklist Settings" onClose={handleClose}>
            {!showConfirmation ? (
                <>
                    <Input
                        label="Title"
                        name="checklist-title"
                        value={tempTitle}
                        onChange={handleTitleChange}
                        placeholder="Enter checklist title"
                    />
                    <TextArea
                        label="Description"
                        name="checklist-description"
                        defaultValue={tempDescription}
                        onInput={handleDescriptionInput}
                        placeholder="Enter checklist description"
                        rows={3}
                        autoResize={true}
                    />
                    
                    {!showLoadSection && (
                        <div className="tool-bar checklist-load-button-container">
                            <button onClick={handleLoadButtonClick}>Load an existing checklist</button>
                        </div>
                    )}
                    
                    {showLoadSection && (
                        <div className="checklist-load-section">
                            <div className="checklist-search-container">
                                <Icon name="search" />
                                <Input
                                    name="checklist-search"
                                    value={searchText}
                                    onInput={handleSearchInput}
                                    placeholder="Search checklists..."
                                />
                            </div>
                            
                            {isLoading ? (
                                <div className="checklist-loading-container">Loading...</div>
                            ) : (
                                <table className="spreadsheet">
                                    <thead>
                                        <tr>
                                            <th>Title</th>
                                            <th>Created</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {checklists.length === 0 ? (
                                            <tr>
                                                <td colSpan="2">No checklists found</td>
                                            </tr>
                                        ) : (
                                            checklists.map((checklist) => (
                                                <tr key={checklist.id} onClick={() => handleLoadChecklist(checklist.id)}>
                                                    <td>{checklist.title || checklist.entryTitle || 'Untitled'}</td>
                                                    <td>{formatDate(checklist.created)}</td>
                                                </tr>
                                            ))
                                        )}
                                    </tbody>
                                </table>
                            )}
                        </div>
                    )}
                    
                    <div className="buttons">
                        <button onClick={handleSave}>{isSaving ? 'Saving...' : 'Save'}</button>
                        <button className="cancel" onClick={handleClose}>Cancel</button>
                    </div>
                </>
            ) : (
                <>
                    <p>Do you want to replace the current checklist with the items below?</p>
                    {selectedChecklist && (
                        <div>
                            {selectedChecklist.items && selectedChecklist.items.length > 0 ? (
                                <div>
                                    {selectedChecklist.items.slice(0, 10).map((item, index) => (
                                        <div key={item.id || index} className="checklist-confirmation-item">
                                            {item.title || 'Untitled item'}
                                        </div>
                                    ))}
                                    {selectedChecklist.items.length > 10 && (
                                        <div className="checklist-confirmation-more">
                                            ...and {selectedChecklist.items.length - 10} more items
                                        </div>
                                    )}
                                </div>
                            ) : (
                                <p className="checklist-confirmation-empty">No items in this checklist</p>
                            )}
                        </div>
                    )}
                    <div className="buttons">
                        <button onClick={handleConfirmLoad}>Yes</button>
                        <button className="cancel" onClick={handleCancelLoad}>Cancel</button>
                    </div>
                </>
            )}
        </Modal>
    );
}
