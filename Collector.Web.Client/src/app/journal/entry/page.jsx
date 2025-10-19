import React, { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
//css
import './page.css';
//components
import Icon from '@/components/ui/icon';
import Input from '@/components/forms/input';
import ToggleSwitch from '@/components/ui/toggle-switch';
import Modal from '@/components/ui/modal';
//context
import { useSession } from '@/context/session';
//api
import { Journals } from '@/api/user/journals';
//modules
import modules from './modules';

const defaultEntryJson = {
    modules: [
        {
            type: 'text-editor',
            manuallyAdded: false,
            html: '<p>Type or paste your content here!</p>'
        }
    ]
};

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
    const [showTopModuleDropdown, setShowTopModuleDropdown] = useState(false);
    const [showBottomModuleDropdown, setShowBottomModuleDropdown] = useState(false);
    const [showModuleAboveDropdown, setShowModuleAboveDropdown] = useState(false);
    const [currentModuleId, setCurrentModuleId] = useState(null);
    const [isEditing, setIsEditing] = useState(false);
    const [showSettingsModal, setShowSettingsModal] = useState(false);
    const [settings, setSettings] = useState({ encrypted: false, published: false });
    const [settingsChanged, setSettingsChanged] = useState(false);

    // refs
    const entryRef = useRef(null);
    const entryJsonRef = useRef(null);
    const topDropdownRef = useRef(null);
    const bottomDropdownRef = useRef(null);
    const dropdownButtonRef = useRef(null);
    const bottomDropdownButtonRef = useRef(null);
    const moduleDropdownRef = useRef(null);
    const moduleDropdownButtonRef = useRef(null);
    const titleInputRef = useRef(null);

    //apis
    const { addEntry, renameEntry, setEntryEncrypted, setEntryPublished } = Journals(session);

    //effect
    useEffect(() => {
        if(!entry || entry.id != entryId) {
            fetchEntryDetails();
        }
    }, [journalId, entryId]);

    useEffect(() => {
        if (isTitleEditing && titleInputRef.current) {
            titleInputRef.current.focus();
        }
    }, [isTitleEditing]);

    useEffect(() => {
        const handleClickOutside = (event) => {
            // Check if click is outside the top dropdown and its button
            if (showTopModuleDropdown && topDropdownRef.current && !topDropdownRef.current.contains(event.target)) {
                const isOutsideTopButton = !dropdownButtonRef.current || !dropdownButtonRef.current.contains(event.target);
                if (isOutsideTopButton) {
                    setShowTopModuleDropdown(false);
                }
            }
            
            // Check if click is outside the bottom dropdown and its button
            if (showBottomModuleDropdown && bottomDropdownRef.current && !bottomDropdownRef.current.contains(event.target)) {
                const isOutsideBottomButton = !bottomDropdownButtonRef.current || !bottomDropdownButtonRef.current.contains(event.target);
                if (isOutsideBottomButton) {
                    setShowBottomModuleDropdown(false);
                }
            }

            if (showModuleAboveDropdown &&
                moduleDropdownRef.current &&
                !moduleDropdownRef.current.contains(event.target) &&
                moduleDropdownButtonRef.current &&
                !moduleDropdownButtonRef.current.contains(event.target)) {
                setShowModuleAboveDropdown(false);
            }
        };

        document.addEventListener('mousedown', handleClickOutside);

        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
        };
    }, [showTopModuleDropdown, showBottomModuleDropdown, showModuleAboveDropdown]);

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

            const isNewEntry = entryId === 'new' || entryId == null;

            if (isNewEntry) {
                // Create a new entry template
                const newEntry = {
                    id: 0,
                    journalId: parseInt(journalId),
                    title: '',
                    description: '',
                    created: new Date().toISOString(),
                    status: 1
                };

                setEntry(newEntry);
                entryRef.current = newEntry;
                setEditedTitle(newEntry.title);
                setEditedDescription(newEntry.description);
                setIsTitleEditing(true); // Automatically show title editor for new entries
                setIsEditing(true);
                const newEntryJson = {...defaultEntryJson, id: generateRandomId()};
                setEntryJson(newEntryJson);
                entryJsonRef.current = newEntryJson;
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
                entryRef.current = entryData;
                setEditedTitle(entryData.title);
                setEditedDescription(entryData.description);
                setSettings({
                    encrypted: entryData.encrypted,
                    published: entryData.status === 2
                });

                // Fetch entry content (JSON data)
                try {
                    const contentResponse = await api.getEntryContent(entryId);
                    if (contentResponse.data.success && contentResponse.data.data) {
                        try {
                            const contentJson = JSON.parse(contentResponse.data.data);
                            contentJson.modules.forEach(module => {
                                //remove unneccessary properties
                                delete module.manuallyAdded;
                            });
                            setEntryJson(contentJson || { modules: [] });
                            entryJsonRef.current = contentJson || { modules: [] };
                        } catch (parseErr) {
                            console.error('Error parsing entry content JSON:', parseErr);
                        }
                    }
                } catch (contentErr) {
                    console.error('Error fetching entry content:', contentErr);
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
            setIsTitleEditing(false);
            setIsEditing(false);
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
            //title is the same, do nothing
            setIsTitleEditing(false);
            return;
        }
        if(saveStatus == 'saving') return;

        setSaveStatus('saving');

        try {
            if (entryId === 'new') {
                // For new entries, create the entry first
                const newEntry = {
                    journalId: parseInt(journalId),
                    title: editedTitle.trim(),
                    description: editedDescription,
                    json: entryJson
                };

                const response = await addEntry(newEntry);

                if (!response.data.success) {
                    setSaveStatus('error');
                    throw new Error(response.data.message || 'Failed to create entry');
                }

                const createdEntry = response.data.data;
                navigate(`/journal/${journalId}/entry/${createdEntry.id}`, { replace: true });
                return;
            } else {
                // Update the entry title for existing entries
                await renameEntry(entry.id, editedTitle.trim());
                const updatedEntry = {
                    ...entry,
                    title: editedTitle.trim(),
                    modified: new Date().toISOString()
                };

                setEntry(updatedEntry);
                entryRef.current = updatedEntry;
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
        const modules = entryJsonRef.current.modules;
        const index = modules.findIndex(a => a.id == module.id);
        if (index > -1) {
            modules[index] = module;
            setEntryJson({ ...entryJsonRef.current, modules });
            entryJsonRef.current = { ...entryJsonRef.current, modules };
            saveEntryContent({ ...entryJsonRef.current, modules });
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

    const getStatusText = (entry) => {
        if (entry.encrypted && entry.status > 0) {
            return 'Private';
        }
        switch (entry.status) {
            case 0: return 'Deleted';
            case 1: return 'Active';
            case 2: return 'Published';
            default: return 'Unknown';
        }
    };

    const getStatusClass = (entry) => {
        if (entry.encrypted && entry.status > 0) {
            return 'status-private';
        }
        switch (entry.status) {
            case 0: return 'status-deleted';
            case 1: return 'status-active';
            case 2: return 'status-published';
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

    const addModule = (type, position = 'bottom') => {
        const newModuleId = generateRandomId();
        const newModule = {
            id: newModuleId,
            type: type,
            manuallyAdded: true
        };

        const newEntryJson = { ...entryJsonRef.current, modules: [...(entryJsonRef.current?.modules ?? []), newModule] };
        setEntryJson(newEntryJson);
        entryJsonRef.current = newEntryJson;

        // Close the appropriate dropdown based on which one was used
        if (position === 'top') {
            setShowTopModuleDropdown(false);
        } else {
            setShowBottomModuleDropdown(false);
        }
    };

    const addModuleAbove = (type) => {
        if (!currentModuleId) return;

        const newModuleId = generateRandomId();
        const newModule = {
            id: newModuleId,
            type: type,
            manuallyAdded: true
        };

        const moduleIndex = entryJsonRef.current.modules.findIndex(module => module.id === currentModuleId);
        if (moduleIndex === -1) return;

        const updatedModules = [...entryJsonRef.current.modules];
        updatedModules.splice(moduleIndex, 0, newModule);
        const updatedEntryJson = {
            ...entryJsonRef.current,
            modules: updatedModules
        };

        setEntryJson(updatedEntryJson);
        entryJsonRef.current = updatedEntryJson;
        saveEntryContent(updatedEntryJson);
        setShowModuleAboveDropdown(false);
    };

    const removeModule = (moduleId) => {
        const updatedEntryJson = {
            ...entryJsonRef.current,
            modules: entryJsonRef.current.modules.filter(module => module.id !== moduleId)
        };
        setEntryJson(updatedEntryJson);
        entryJsonRef.current = updatedEntryJson;
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
                    <Icon name="progress_activity" spin={true} />
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
    const handleOpenSettings = () => {
        setSettings({
            encrypted: entry.encrypted,
            published: entry.status === 2
        });
        setSettingsChanged(false);
        setShowSettingsModal(true);
    };

    const handleCloseSettings = () => {
        setShowSettingsModal(false);
    };

    const handleSettingChange = (setting, value) => {
        const newSettings = { ...settings, [setting]: value };

        // if encrypted is turned on, turn published off
        if (setting === 'encrypted' && value && newSettings.published) {
            newSettings.published = false;
        }

        setSettings(newSettings);
        setSettingsChanged(true);
    };

    const handleSaveChanges = async () => {
        if (!settingsChanged) return;

        setSaveStatus('saving');
        try {
            let promises = [];
            const originalPublished = entry.status === 2;

            if (settings.encrypted !== entry.encrypted) {
                promises.push(setEntryEncrypted(entry.id, settings.encrypted));
            }

            if (settings.published !== originalPublished) {
                promises.push(setEntryPublished(entry.id, settings.published));
            }

            await Promise.all(promises);

            // Manually update local entry state to reflect changes immediately
            const updatedEntry = { 
                ...entry, 
                encrypted: settings.encrypted,
                status: settings.published ? 2 : 1
            };
            setEntry(updatedEntry);
            entryRef.current = updatedEntry;

            setSaveStatus('saved');
            setTimeout(() => setSaveStatus(null), 3000);
            handleCloseSettings();

        } catch (err) {
            console.error('Error saving settings:', err);
            setSaveStatus('error');
        }
    };

    return (
        <div className="journal-entry-page">
            {showSettingsModal && (
                <Modal title="Entry Settings" onClose={handleCloseSettings}>
                    <div className="settings-modal-content">
                        <ToggleSwitch
                            name="encrypted"
                            label="Encrypted"
                            checked={settings.encrypted}
                            onChange={(isChecked) => handleSettingChange('encrypted', isChecked)}
                        />
                        <ToggleSwitch
                            name="published"
                            label="Published"
                            checked={settings.published}
                            onChange={(isChecked) => handleSettingChange('published', isChecked)}
                            disabled={settings.encrypted} // disable if encrypted is on
                        />
                    </div>
                    <div className="buttons">
                        {settingsChanged && (
                            <button className="btn primary" onClick={handleSaveChanges}>Save Changes</button>
                        )}
                        <button className="btn cancel" onClick={handleCloseSettings}>Cancel</button>
                    </div>
                </Modal>
            )}
            <div className="entry-header">
                <div className="entry-navigation tool-bar">
                    <button className="back-button" onClick={handleBackToJournal}>
                        <Icon name="arrow_back" /> Back to {journal?.title || 'Journal'}
                    </button>

                    <div className="right-side entry-status-badge">
                        {saveStatus && (
                            <div className={`right-side save-status-message ${saveStatus === 'error' ? 'error' : 'success'}`}>
                                {getSaveStatusMessage()}
                            </div>
                        )}
                        <span className={`status-indicator ${getStatusClass(entry)}`}>
                            {getStatusText(entry)}
                        </span>
                        <ToggleSwitch
                            name="edit-entry"
                            checked={isEditing}
                            onChange={setIsEditing}
                            label="Edit"
                        />
                        <button className="icon" onClick={handleOpenSettings}>
                            <Icon name="settings" />
                        </button>
                    </div>
                </div>


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
                        <div className="entry-title-display tool-bar" onClick={handleTitleEdit}>
                            <h1>{entry.title || 'Untitled Entry'}</h1>
                            <button className="icon">
                                <Icon name="edit" />
                            </button>
                        </div>
                    )}
                </div>
                <div className="tool-bar">
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

                    {isEditing && (
                        <div className="tool-bar add-module-container">
                            <div className="right-side">
                                <button
                                    ref={dropdownButtonRef}
                                    onClick={() => {
                                        // Close bottom dropdown if it's open
                                        if (showBottomModuleDropdown) {
                                            setShowBottomModuleDropdown(false);
                                        }
                                        setShowTopModuleDropdown(!showTopModuleDropdown);
                                    }}
                                >
                                    <Icon name="add" /> Add Content
                                </button>

                                {showTopModuleDropdown && (
                                    <div
                                        className="module-dropdown"
                                        ref={topDropdownRef}
                                    >
                                        {modules.map(module => (
                                            <div
                                                key={module.id}
                                                className="module-option"
                                                onClick={() => addModule(module.type, 'top')}
                                            >
                                                <Icon name={module.icon} />
                                                <span>{module.name}</span>
                                            </div>
                                        ))}
                                    </div>
                                )}
                            </div>
                        </div>
                    )}
                </div>
            </div>

            <div className="entry-content">
                {entry.description && (
                    <div className="entry-description">
                        <p>{entry.description}</p>
                    </div>
                )}


                <div className="entry-modules">
                    {entryJson.modules.map((module) => {
                        if (!module.type) return;
                        const moduleType = modules.find(m => m.type === module.type);
                        const ModuleComponent = moduleType?.module;
                        return (
                            <div 
                                key={'module-' + module.id} 
                                className={
                                    `entry module-${module.type?.replace(' ', '-') ?? ''} ` +
                                    `module-id-${module.id} ${isEditing ? 'editable' : ''}` +
                                    (module.manuallyAdded ? ' manually-added' : '')
                                }
                            >
                                {isEditing && (
                                    <div className="module-tab-container">
                                        <div className="module-tab">
                                            <div className="module-type">{moduleType?.name}</div>
                                            <div className="box">
                                                <div className="tool-bar vertical">
                                                    <button
                                                        className="icon"
                                                        ref={module.id === currentModuleId ? moduleDropdownButtonRef : null}
                                                        onClick={() => {
                                                            setCurrentModuleId(module.id);
                                                            setShowModuleAboveDropdown(!showModuleAboveDropdown || currentModuleId !== module.id);
                                                        }}
                                                    >
                                                        <Icon name="add" />
                                                    </button>
                                                    {showModuleAboveDropdown && currentModuleId === module.id && (
                                                        <div
                                                            className="module-dropdown module-dropdown-left"
                                                            ref={moduleDropdownRef}
                                                        >
                                                            {modules.map(moduleOption => (
                                                                <div
                                                                    key={moduleOption.id}
                                                                    className="module-option"
                                                                    onClick={() => addModuleAbove(moduleOption.type)}
                                                                >
                                                                    <Icon name={moduleOption.icon} />
                                                                    <span>{moduleOption.name}</span>
                                                                </div>
                                                            ))}
                                                        </div>
                                                    )}
                                                    <button className="icon" onClick={() => removeModule(module.id)}>
                                                        <Icon name="delete" />
                                                    </button>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                )}
                                <ModuleComponent 
                                    module={module} 
                                    entryId={entryId} 
                                    onUpdate={handleUpdatedModule} 
                                    isEditable={isEditing} 
                                    manuallyAdded={module.manuallyAdded} 
                                />
                            </div>
                        )
                    })}
                </div>

                {/* Bottom Add Content button - only shows if there are modules and editing is enabled */}
                {entryJson.modules.length > 0 && isEditing && (
                    <div className="tool-bar add-module-container bottom-add-module">
                        <div className="right-side">
                            <button
                                ref={bottomDropdownButtonRef}
                                onClick={() => {
                                    // Close top dropdown if it's open
                                    if (showTopModuleDropdown) {
                                        setShowTopModuleDropdown(false);
                                    }
                                    setShowBottomModuleDropdown(!showBottomModuleDropdown);
                                }}
                            >
                                <Icon name="add" /> Add Content
                            </button>
                            {showBottomModuleDropdown && (
                                <div
                                    className="module-dropdown"
                                    ref={bottomDropdownRef}
                                >
                                    {modules.map(module => (
                                        <div
                                            key={module.id}
                                            className="module-option"
                                            onClick={() => addModule(module.type, 'bottom')}
                                        >
                                            <Icon name={module.icon} />
                                            <span>{module.name}</span>
                                        </div>
                                    ))}
                                </div>
                            )}
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}
