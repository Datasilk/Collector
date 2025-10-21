import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import Icon from '@/components/ui/icon';
import ToggleSwitch from '@/components/ui/toggle-switch';
import { useSession } from '@/context/session';
import { Journals } from '@/api/user/journals';
import ModuleList from '@/app/journal/entry/module-list';
import detailsModules from './components/modules';
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

    //effect
    useEffect(() => {
        fetchJournalDetails();
    }, [journalId, navigate, session]);

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
                    setJournal(journalData);

                    // Parse modules from journal
                    const parsedModules = [];

                    if (journalData.modules && journalData.modules.length > 0) {
                        journalData.modules.forEach(module => {
                            try {
                                const json = JSON.parse(module.json);
                                parsedModules.push({
                                    ...module,
                                    ...json,
                                    entryId: module.journalEntryId,
                                    id: module.moduleId,
                                    pinned: true
                                });
                            } catch (err) {
                                console.error('Error parsing module JSON:', err);
                            }
                        });
                    }

                    // Insert or update entries-list module at the specified index
                    const entriesListIndex = journalData.entriesListIndex || 999;
                    const entriesListModule = {
                        id: 'entries-list',
                        type: 'entries-list',
                        showTab: false,
                        pinned: true,
                        sort: 999
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

                    setModules(parsedModules);
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

    const handleEditJournal = () => {
        navigate(`/journal/${journalId}/edit`);
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

    const handleUpdatedModule = (updatedEntryJson) => {
        // Update the modules list with the new order
        if (updatedEntryJson && updatedEntryJson.modules) {
            setModules(updatedEntryJson.modules);
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
                    <button onClick={handleEditJournal}>
                        <Icon name="edit" /> Edit Journal
                    </button>
                </div>
            </div>

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
