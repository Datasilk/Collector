import React, { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import Icon from '@/components/ui/icon';
import ToggleSwitch from '@/components/ui/toggle-switch';
import { useSession } from '@/context/session';
import { Journals } from '@/api/user/journals';
import ModuleList from '@/app/journal/entry/module-list';
import detailsModules from './components/modules';
import JournalSettingsModal from './components/settings-modal';
import '../entry/page.css';
import './page.css';
import '@/styles/forms.css';

/**
 * <summary>Journal Details Page</summary>
 * <description>Displays detailed information about a specific journal and its entries</description>
 */
export default function JournalDetailsPage() {
    //context
    const { journalId } = useParams();
    const navigate = useNavigate();
    const session = useSession();

    //state
    const [journal, setJournal] = useState(null);
    const [modules, setModules] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [canEditLayout, setCanEditLayout] = useState(false);
    const [showSettingsModal, setShowSettingsModal] = useState(false);

    //refs
    const strippedModulesRef = useRef(null);

    //effect
    useEffect(() => {
        fetchJournalDetails();
    }, [journalId, navigate, session]);

    useEffect(() => {
        // Inject CSS from journal settings
        if (journal?.settings?.css) {
            const styleId = `journal-${journalId}-styles`;
            let styleElement = document.getElementById(styleId);

            if (!styleElement) {
                styleElement = document.createElement('style');
                styleElement.id = styleId;
                document.head.appendChild(styleElement);
            }

            styleElement.textContent = journal.settings.css;
        }

        // Cleanup function to remove style when component unmounts or journal changes
        return () => {
            const styleId = `journal-${journalId}-styles`;
            const styleElement = document.getElementById(styleId);
            if (styleElement) {
                styleElement.remove();
            }
        };
    }, [journal?.settings?.css, journalId]);



    //actions
    const fetchJournalDetails = () => {
        setLoading(true);

        // Use the journals API to fetch journal details
        const api = Journals(session);

        // Get journal details
        api.getJournal(journalId)
            .then(response => {
                if (response.data.success) {
                    const journalData = response.data.data;
                    const modulesLayout = journalData.layout;

                    // Parse modules from journal
                    let parsedModules = [];
                    let foundEntriesList = false;

                    if (journalData.modules && journalData.modules.length > 0 && !modulesLayout) {
                        journalData.modules.forEach(module => {
                            try {
                                const json = module.json ? JSON.parse(module.json) : {};
                                const mod = {
                                    ...module,
                                    ...json,
                                    entryId: module.journalEntryId,
                                    id: module.moduleId,
                                    pinned: true
                                };
                                parsedModules.push(mod);
                            } catch (err) {
                                console.error('Error parsing module JSON:', err);
                            }
                        });
                    } else if (modulesLayout && modulesLayout.length > 0) {
                        //get custom layout for journal's modules list
                        const mergeModulesInHierarchy = (modulesList, parentEntryId) => {
                            const newModules = [];
                            modulesList.forEach(module => {
                                try {
                                    const moduleData = removeNullUndefined(journalData.modules.find(m => m.moduleId == module.id) ?? {});
                                    const json = removeNullUndefined(moduleData.json ? JSON.parse(moduleData.json) : {});
                                    var mod = {
                                        ...moduleData,
                                        ...json,
                                        ...module,
                                        entryId: moduleData.journalEntryId ?? module.entryId ?? parentEntryId,
                                        id: module.id ?? moduleData.id ?? json.id,
                                        pinned: true
                                    };
                                    mod.moduleId = mod.id;

                                    if (mod.id == 'entries-list') {
                                        // Insert or update entries-list module at the specified index
                                        const entriesListModule = {
                                            id: 'entries-list',
                                            type: 'entries-list',
                                            showTab: true,
                                            showPinned: false,
                                            pinned: true
                                        };
                                        foundEntriesList = true;
                                        // Update existing entries-list module
                                        mod = { ...mod, ...entriesListModule };
                                    }
                                    if (mod.modules && mod.modules.length > 0) {
                                        mod.modules = mergeModulesInHierarchy(mod.modules, mod.entryId);
                                    }
                                    newModules.push(mod);
                                } catch (err) {
                                    console.error('Error parsing module JSON:', err);
                                }
                            });
                            return newModules;
                        };
                        parsedModules = mergeModulesInHierarchy(modulesLayout);
                    }

                    if (!foundEntriesList) {
                        // Insert or update entries-list module at the specified index if it hasn't been added yet
                        const entriesListIndex = journalData.entriesListIndex || -1;
                        const entriesListModule = {
                            id: 'entries-list',
                            type: 'entries-list',
                            showTab: true,
                            showPinned: false,
                            pinned: true,
                            sort: -1
                        };

                        // Check if entries-list already exists
                        const existingIndex = parsedModules.findIndex(m => m.id == 'entries-list');
                        if (existingIndex > -1) {
                            // Update existing entries-list module
                            parsedModules[existingIndex] = { ...parsedModules[existingIndex], ...entriesListModule };
                        } else {
                            // Insert at the correct position
                            if (entriesListIndex >= parsedModules.length) {
                                parsedModules.push(entriesListModule);
                            } else {
                                parsedModules.splice(entriesListIndex, 0, entriesListModule);
                            }
                        }
                    }

                    setJournal(journalData);
                    setModules(parsedModules);

                    // Store the original stripped modules for comparison
                    const strippedModules = stripModulesData(parsedModules);
                    strippedModulesRef.current = JSON.stringify(strippedModules);

                    // Inject CSS if settings exist
                    if (journalData.settings?.css) {
                        const styleId = `journal-${journalId}-styles`;
                        let styleElement = document.getElementById(styleId);

                        if (!styleElement) {
                            styleElement = document.createElement('style');
                            styleElement.id = styleId;
                            document.head.appendChild(styleElement);
                        }

                        styleElement.textContent = journalData.settings.css;
                    }
                }

                setLoading(false);
            })
            .catch(err => {
                console.error('Error fetching journal details:', err);
                setError('Failed to load journal details. Please try again later.');
                setLoading(false);
            });
    };

    const handleNewEntry = () => {
        navigate(`/journal/${journalId}/entry/new`);
    };

    const handleOpenSettings = () => {
        setShowSettingsModal(true);
    };

    const handleCloseSettings = () => {
        setShowSettingsModal(false);
    };

    const handleSettingsSaved = (updatedJournal, css) => {
        // Update journal with new title
        setJournal(updatedJournal);

        // Inject updated CSS
        if (css !== undefined) {
            const styleId = `journal-${journalId}-styles`;
            let styleElement = document.getElementById(styleId);

            if (!styleElement) {
                styleElement = document.createElement('style');
                styleElement.id = styleId;
                document.head.appendChild(styleElement);
            }

            styleElement.textContent = css;
        }
    };

    // Format date for display
    const formatDate = (dateString) => {
        if (!dateString) return 'N/A';
        const date = new Date(dateString);
        return date.toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'long',
            day: 'numeric'
        });
    };

    const handleUnPinModule = async (moduleId) => {
        try {
            // Find the module to get its entryId
            const moduleToRemove = modules.find(m => m.id == moduleId);

            if (moduleToRemove && moduleToRemove.entryId) {
                // Call API to unpin the module from the journal
                const api = Journals(session);
                const response = await api.deleteModule(journalId, moduleToRemove.entryId, moduleId);

                if (!response.data.success) {
                    console.error('Failed to unpin module:', response.data.message);
                }
            }

            // Update local state to remove the module from the UI
            const updatedModules = modules.filter(module => module.id != moduleId);
            setModules(updatedModules);
        } catch (err) {
            console.error('Error removing module:', err);
        }
    };

    const handleEditLayout = (checked) => {
        setCanEditLayout(checked);
    };

    const removeNullUndefined = (obj) => {
        if (Array.isArray(obj)) {
            return obj.map(item => removeNullUndefined(item)).filter(item => item !== null && item !== undefined);
        } else if (obj !== null && typeof obj === 'object') {
            return Object.keys(obj).reduce((acc, key) => {
                const value = obj[key];
                if (value !== null && value !== undefined) {
                    acc[key] = removeNullUndefined(value);
                }
                return acc;
            }, {});
        }
        return obj;
    };

    const stripModulesData = (modulesList) => {
        return modulesList.map(module => {
            try {
                const mod = {
                    id: module.id,
                    type: module.type,
                    sort: module.sort,
                    width: module.width,
                    height: module.height
                };
                if (module.modules && module.modules.length > 0) {
                    mod.modules = stripModulesData(module.modules);
                }
                return mod;
            } catch (err) {
                console.error('Error parsing module JSON:', err);
            }
        });
    }

    const handleUpdatedModule = async (updatedModule) => {
        // Update the modules list with the new order
        if (updatedModule) {
            const index = modules.findIndex(a => a.id == updatedModule.id);
            const newModules = [...modules];
            if (index > -1) {
                newModules[index] = updatedModule;
                setModules(newModules);
            }

            // Compare stripped module data to see if layout changed
            const newStrippedModules = JSON.stringify(stripModulesData(newModules));
            if (strippedModulesRef.current !== newStrippedModules) {
                // Layout has changed, save it
                try {
                    const api = Journals(session);
                    await api.updateJournalLayout(journalId, newStrippedModules);

                    // Update the original to the new value
                    strippedModulesRef.current = newStrippedModules;
                } catch (err) {
                    console.error('Error saving journal layout:', err);
                }
            }
        }
    };

    // Render loading state
    if (loading) {
        return (
            <div className="journal-details-page loading">
                <div className="loading-spinner">
                    <Icon name="progress_activity" spin={true} />
                    <p>Loading journal details...</p>
                </div>
            </div>
        );
    }

    // Render error state
    if (error) {
        return (
            <div className="journal-details-page error">
                <div className="error-message">
                    <Icon name="error" />
                    <p>{error}</p>
                    <button onClick={() => window.location.reload()}>Retry</button>
                </div>
            </div>
        );
    }

    // Render select journal message or journal not found
    if (!journal) {
        if (!journalId) {
            return (
                <div className="journal-details-page select-journal">
                    <div className="select-journal-message">
                        <Icon name="menu_book" />
                        <h2>Select a Journal</h2>
                        <p>Please select a journal from the sidebar to view its contents.</p>
                        <p className="hint">You can create a new journal by clicking the + button in the sidebar.</p>
                    </div>
                </div>
            );
        } else {
            return (
                <div className="journal-details-page not-found">
                    <div className="not-found-message">
                        <Icon name="warning" />
                        <h2>Journal Not Found</h2>
                        <p>The journal you're looking for doesn't exist or you don't have permission to view it.</p>
                        <button onClick={() => navigate('/journal')}>Back to Journals</button>
                    </div>
                </div>
            );
        }
    }
    // Main render
    return (
        <div className="journal-details-page">
            <div className="tool-bar">
                <div className="title">{journal.title}</div>
                <div className="right-side">
                    <ToggleSwitch
                        name="edit-layout"
                        label="Edit"
                        checked={canEditLayout}
                        onChange={handleEditLayout}
                    />
                    <button onClick={handleNewEntry}>
                        <Icon name="add" /> New Entry
                    </button>
                    <button onClick={handleOpenSettings} className="icon">
                        <Icon name="settings" />
                    </button>
                </div>
            </div>

            {showSettingsModal && (
                <JournalSettingsModal
                    journal={journal}
                    onClose={handleCloseSettings}
                    onSaved={handleSettingsSaved}
                />
            )}

            <div className="journal-metadata">
                <div className="created-date">
                    <Icon name="calendar_today" />
                    <span>Created: {formatDate(journal.created)}</span>
                </div>
                <div className="status">
                    <Icon name="info" />
                    <span>Status: {journal.status == 1 ? 'Active' : 'Archived'}</span>
                </div>
            </div>
            <div className="modules-list">
                <ModuleList
                    entryJson={{ modules: modules }}
                    entryId={null}
                    journalId={journalId}
                    isEditing={true}
                    canAddAbove={false}
                    canDelete={false}
                    canPin={false}
                    showLabel={false}
                    showHoverTab={canEditLayout}
                    canResize={canEditLayout}
                    canDragDrop={canEditLayout}
                    updatedModule={handleUpdatedModule}
                    addedModule={() => { }}
                    showHoverOutline={false}
                    modulesRegistry={detailsModules}
                    onUnPinModule={handleUnPinModule}
                />
            </div>
        </div>
    );
}
