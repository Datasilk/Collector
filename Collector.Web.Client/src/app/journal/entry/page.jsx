import React, { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
//css
import './page.css';
//components
import Icon from '@/components/ui/icon';
import Input from '@/components/forms/input';
//context
import { useSession } from '@/context/session';
//api
import { Journals } from '@/api/user/journals';
//modules
import modules from './modules';
import CKEditorModule from './modules/ckeditor';

/**
 * <summary>Journal Entry Page</summary>
 * <description>Displays and allows editing of a specific journal entry</description>
 */
export default function JournalEntryPage() {
    //context
    const { journalId, entryId } = useParams();
    const navigate = useNavigate();
    const session = useSession();

    //state
    const [entry, setEntry] = useState(null);
    const [journal, setJournal] = useState(null);
    const [editedTitle, setEditedTitle] = useState('');
    const [editedDescription, setEditedDescription] = useState('');
    const [isTitleEditing, setIsTitleEditing] = useState(false);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [saveStatus, setSaveStatus] = useState(null);
    const [entryJson, setEntryJson] = useState({ modules: [] });
    const [showModuleDropdown, setShowModuleDropdown] = useState(false);

    // refs
    const dropdownRef = useRef(null);
    const dropdownButtonRef = useRef(null);
    const titleInputRef = useRef(null);

    //effect
    useEffect(() => {
        fetchEntryDetails();
    }, [journalId, entryId, navigate, session]);

    useEffect(() => {
        if (isTitleEditing && titleInputRef.current) {
            titleInputRef.current.focus();
        }
    }, [isTitleEditing]);

    useEffect(() => {
        const handleClickOutside = (event) => {
            if (showModuleDropdown &&
                dropdownRef.current &&
                !dropdownRef.current.contains(event.target) &&
                dropdownButtonRef.current &&
                !dropdownButtonRef.current.contains(event.target)) {
                setShowModuleDropdown(false);
            }
        };

        document.addEventListener('mousedown', handleClickOutside);

        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
        };
    }, [showModuleDropdown]);

    //actions
    const fetchEntryDetails = async () => {
        try {
            setLoading(true);
            const api = Journals(session);
            const journalResponse = await api.getJournal(journalId);

            if (!journalResponse.data.success) {
                setError(journalResponse.data.message || 'Failed to load journal details');
                setLoading(false);
                return;
            }

            const journalData = journalResponse.data.data;
            setJournal(journalData);

            const isNewEntry = entryId === 'new';

            if (isNewEntry) {
                // Create a new entry template
                const newEntry = {
                    id: 0,
                    journalId: parseInt(journalId),
                    title: '',
                    description: '',
                    created: new Date().toISOString(),
                    status: 0
                };

                setEntry(newEntry);
                setEditedTitle(newEntry.title);
                setEditedDescription(newEntry.description);
                setIsTitleEditing(true); // Automatically show title editor for new entries
                setEntryJson({ modules: [] });
            } else {
                // Get existing entry data
                const entryResponse = await api.getEntry(entryId);

                if (!entryResponse.data.success) {
                    setError(entryResponse.data.message || 'Failed to load entry details');
                    setLoading(false);
                    return;
                }

                const entryData = entryResponse.data.data;
                setEntry(entryData);
                setEditedTitle(entryData.title);
                setEditedDescription(entryData.description);

                // Fetch entry content (JSON data)
                try {
                    const contentResponse = await api.getEntryContent(entryId);
                    if (contentResponse.data.success && contentResponse.data.data) {
                        try {
                            const contentJson = JSON.parse(contentResponse.data.data);
                            setEntryJson(contentJson || { modules: [] });
                        } catch (parseErr) {
                            console.error('Error parsing entry content JSON:', parseErr);
                            setEntryJson({ modules: [] });
                        }
                    } else {
                        setEntryJson({ modules: [] });
                    }
                } catch (contentErr) {
                    console.error('Error fetching entry content:', contentErr);
                    setEntryJson({ modules: [] });
                }
            }

            setLoading(false);
        } catch (err) {
            console.error('Error fetching entry details:', err);
            setError('Failed to load entry details. Please try again later.');
            setLoading(false);
        }
    };

    const handleTitleEdit = () => {
        setIsTitleEditing(true);
    };

    const handleTitleKeyDown = (e) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            updateEntryTitle();
        } else if (e.key === 'Escape') {
            e.preventDefault();
            setIsTitleEditing(false);
            setEditedTitle(entry.title);
        }
    };

    const handleTitleBlur = () => {
        updateEntryTitle();
    };

    const updateEntryTitle = async () => {
        if (editedTitle.trim() === '') return;

        if (entryId !== 'new' && entry.title === editedTitle.trim()) {
            setIsTitleEditing(false);
            return;
        }

        setSaveStatus('saving');
        const api = Journals(session);

        try {
            if (entryId === 'new') {
                // For new entries, create the entry first
                const newEntry = {
                    journalId: parseInt(journalId),
                    title: editedTitle.trim(),
                    description: editedDescription
                };

                const response = await api.addEntry(newEntry);

                if (!response.data.success) {
                    throw new Error(response.data.message || 'Failed to create entry');
                }

                const createdEntry = response.data.data;
                navigate(`/journal/${journalId}/entry/${createdEntry.id}`, { replace: true });
                return;
            } else {
                // Update the entry title for existing entries
                await api.renameEntry(entry.id, editedTitle.trim());
                const updatedEntry = {
                    ...entry,
                    title: editedTitle.trim(),
                    modified: new Date().toISOString()
                };

                setEntry(updatedEntry);
                setSaveStatus('saved');

                // Clear the "saved" status after a few seconds
                setTimeout(() => {
                    setSaveStatus(null);
                }, 3000);
            }
        } catch (err) {
            console.error('Error saving title:', err);
            setSaveStatus('error');
        } finally {
            setIsTitleEditing(false);
        }
    };

    const handleBackToJournal = () => {
        navigate(`/journal/${journalId}`);
    };

    const handleUpdatedModule = (module) => {
        const modules = entryJson.modules;
        const index = modules.findIndex(a => a.id == module.id);
        if (index > -1) {
            modules[index] = module;
            setEntryJson({ ...entryJson, modules });
            saveEntryContent({ ...entryJson, modules });
        }
    };

    const formatDate = (dateString) => {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'long',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    };

    const getStatusText = (status) => {
        switch (status) {
            case 0: return 'Active';
            case 1: return 'Published';
            case 2: return 'Archived';
            default: return 'Unknown';
        }
    };

    const getStatusClass = (status) => {
        switch (status) {
            case 0: return 'status-active';
            case 1: return 'status-published';
            case 2: return 'status-archived';
            default: return '';
        }
    };

    const getSaveStatusMessage = () => {
        switch (saveStatus) {
            case 'saving': return 'Saving...';
            case 'saved': return 'Saved successfully';
            case 'archived': return 'Entry archived';
            case 'unarchived': return 'Entry unarchived';
            case 'published': return 'Entry published';
            case 'error': return 'Error saving changes';
            default: return null;
        }
    };

    const generateRandomId = () => {
        return Math.floor(Math.random() * 1000000);
    };

    const addModule = (type) => {

        const newModule = {
            id: generateRandomId(),
            type: type
        };

        setEntryJson(prev => ({
            ...prev,
            modules: [...prev.modules, newModule]
        }));

        setShowModuleDropdown(false);
    };

    const removeModule = (moduleId) => {
        const updatedEntryJson = {
            ...entryJson,
            modules: entryJson.modules.filter(module => module.id !== moduleId)
        };
        setEntryJson(updatedEntryJson);
        saveEntryContent(updatedEntryJson);
    };

    // Save entry content to the server
    const saveEntryContent = async (json) => {
        if (!entry || !entry.id || entry.id === 0) return;

        setSaveStatus('saving');
        try {
            const api = Journals(session);
            const contentString = JSON.stringify(json);

            const response = await api.updateEntryContent(entry.id, contentString);

            if (!response.data.success) {
                throw new Error(response.data.message || 'Failed to save entry content');
            }

            setSaveStatus('saved');

            // Clear the "saved" status after a few seconds
            setTimeout(() => {
                setSaveStatus(null);
            }, 3000);

        } catch (err) {
            console.error('Error saving entry content:', err);
            setSaveStatus('error');
        }
    };

    // Render loading state
    if (loading) {
        return (
            <div className="journal-entry-page loading">
                <div className="loading-spinner">
                    <Icon name="loading" />
                    <p>Loading entry...</p>
                </div>
            </div>
        );
    }

    // Render error state
    if (error) {
        return (
            <div className="journal-entry-page error">
                <div className="error-message">
                    <Icon name="error" />
                    <p>{error}</p>
                    <button className="btn primary" onClick={() => window.location.reload()}>Retry</button>
                </div>
            </div>
        );
    }

    // Render entry not found
    if (!entry) {
        return (
            <div className="journal-entry-page not-found">
                <div className="not-found-message">
                    <Icon name="warning" />
                    <h2>Entry Not Found</h2>
                    <p>The journal entry you're looking for doesn't exist or you don't have permission to view it.</p>
                    <button className="btn primary" onClick={handleBackToJournal}>Back to Journal</button>
                </div>
            </div>
        );
    }

    // Render entry
    return (
        <div className="journal-entry-page">
            <div className="entry-header tool-bar">
                <button className="back-button" onClick={handleBackToJournal}>
                    <Icon name="arrow_back" /> Back to {journal?.title || 'Journal'}
                </button>

                <div className="right-side entry-status-badge">
                    <span className={`status-indicator ${getStatusClass(entry.status)}`}>
                        {getStatusText(entry.status)}
                    </span>
                </div>

                {saveStatus && (
                    <div className={`right-side save-status-message ${saveStatus === 'error' ? 'error' : 'success'}`}>
                        {getSaveStatusMessage()}
                    </div>
                )}
            </div>

            <div className="entry-content">
                <div className="entry-title-section">
                    {isTitleEditing ? (
                        <Input
                            name="entry-title"
                            value={editedTitle}
                            onInput={(e) => setEditedTitle(e.target.value)}
                            onKeyDown={handleTitleKeyDown}
                            onBlur={handleTitleBlur}
                            placeholder="Enter entry title"
                            ref={titleInputRef}
                            buttons={
                                <span className="tool-bar">
                                    <button className="icon" onClick={() => updateEntryTitle()}>
                                        <Icon name="check" />
                                    </button>
                                    <button className="icon icon-close" onClick={() => {
                                        setIsTitleEditing(false);
                                        setEditedTitle(entry.title);
                                    }}>
                                        <Icon name="close" />
                                    </button>
                                </span>
                            }
                        />
                    ) : (
                        <div className="entry-title-display tool-bar">
                            <h1>{entry.title || 'Untitled Entry'}</h1>
                            <button className="icon" onClick={handleTitleEdit}>
                                <Icon name="edit" />
                            </button>
                        </div>
                    )}
                </div>

                <div className="entry-metadata">
                    <div className="created-date">
                        <Icon name="calendar_today" />
                        <span>Created: {formatDate(entry.created)}</span>
                    </div>
                    {entry.modified && entry.modified !== entry.created && (
                        <div className="modified-date">
                            <Icon name="update" />
                            <span>Modified: {formatDate(entry.modified)}</span>
                        </div>
                    )}
                </div>
                {entry.description && (
                    <div className="entry-description">
                        <p>{entry.description}</p>
                    </div>
                )}

                <div className="tool-bar add-module-container">
                    <div className="right-side">
                        <button
                            ref={dropdownButtonRef}
                            onClick={() => setShowModuleDropdown(!showModuleDropdown)}
                        >
                            <Icon name="add" /> Add Content
                        </button>

                        {showModuleDropdown && (
                            <div
                                className="module-dropdown"
                                ref={dropdownRef}
                            >
                                {modules.map(module => (
                                    <div
                                        key={module.id}
                                        className="module-option"
                                        onClick={() => addModule(module.type)}
                                    >
                                        <Icon name={module.icon} />
                                        <span>{module.name}</span>
                                    </div>
                                ))}
                            </div>
                        )}
                    </div>
                </div>

                <div className="entry-modules">
                    {entryJson.modules.map((module) => {
                        if (!module.type) return;
                        const moduleType = modules.find(m => m.type === module.type);
                        const ModuleComponent = moduleType?.module;
                        return (
                            <div key={'module-' + module.id} className={`entry module-${module.type?.replace(' ', '-') ?? ''} module-id-${module.id}`}>
                                <div className="module-tab-container">
                                    <div className="module-tab">
                                        <div className="module-type">{moduleType?.name}</div>
                                        <div className="box">
                                            <div className="tool-bar vertical">
                                                <button className="icon" onClick={() => removeModule(module.id)}>
                                                    <Icon name="delete" />
                                                </button>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                                <ModuleComponent module={module} onUpdate={handleUpdatedModule} />
                            </div>
                        )
                    })}
                </div>
            </div>
        </div>
    );
}
