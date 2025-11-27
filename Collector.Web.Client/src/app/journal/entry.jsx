import React, { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate, useLocation } from 'react-router-dom';
//css
import './entry.css';
//components
import Icon from '@/components/ui/icon';
import Input from '@/components/forms/input';
import ToggleSwitch from '@/components/ui/toggle-switch';
import ModuleList from './module-list';
import SettingsModal from './components/entry-settings-modal';
import NewEntryTag from './components/new-entry-tag';
import TagsList from './components/tags-list';
import Modal from '@/components/ui/modal';
//context
import { useSession } from '@/context/session';
//api
import { Journals } from '@/api/user/journals';
import { JournalSnapshots } from '@/api/user/journal-snapshots';
import { JournalTags } from '@/api/user/journal-tags';
//modules
import modules from './modules';

/**
 * <summary>Journal Entry Page</summary>
 * <description>Displays and allows editing of a specific journal entry</description>
 */
export default function JournalEntryPage() {
    //context
    const { journalId, entryId, snapshotId } = useParams();
    const navigate = useNavigate();
    const location = useLocation();
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
    const [entryTags, setEntryTags] = useState([]);
    const [showTopModuleDropdown, setShowTopModuleDropdown] = useState(false);
    const [showBottomModuleDropdown, setShowBottomModuleDropdown] = useState(false);
    const [isEditing, setIsEditing] = useState(false);
    const [showSettingsModal, setShowSettingsModal] = useState(false);
    const [chapters, setChapters] = useState([]);
    const [showHistoryDropdown, setShowHistoryDropdown] = useState(false);
    const [snapshots, setSnapshots] = useState([]);
    const [showCreateSnapshotModal, setShowCreateSnapshotModal] = useState(false);
    const [showSnapshotCreatedModal, setShowSnapshotCreatedModal] = useState(false);
    const [loadingSnapshots, setLoadingSnapshots] = useState(false);
    const [fromSnapshotId, setFromSnapshotId] = useState(null);

    // refs
    const entryRef = useRef(null);
    const entryJsonRef = useRef(null);
    const topDropdownRef = useRef(null);
    const bottomDropdownRef = useRef(null);
    const dropdownButtonRef = useRef(null);
    const bottomDropdownButtonRef = useRef(null);
    const titleInputRef = useRef(null);
    const secretsTimerRef = useRef(null);
    const currentChapterName = useRef(null);
    const currentChapter = useRef(null);
    const historyDropdownRef = useRef(null);
    const historyButtonRef = useRef(null);
    const lastEntryRef = useRef(null);

    //apis
    const { addEntry, renameEntry, getChapters, updateJournalEntryId } = Journals(session);
    const { getSnapshotsByEntry, getSnapshot, createSnapshot } = JournalSnapshots(session);
    const { addTagToEntry, removeTagFromEntry } = JournalTags(session);

    //effect
    useEffect(() => {
        //reset state
        setupEntry(null, null, [], false);
        setLoading(true);

        //reset refs
        secretsTimerRef.current = null;
        currentChapterName.current = null;
        currentChapter.current = null;

        //load entry details
        fetchEntryDetails();
    }, [journalId, entryId, snapshotId]);

    useEffect(() => {
        if (!journal || journal.id != journalId) {
            loadChapters();
        }
    }, [journalId]);

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

            // Check if click is outside the history dropdown and its button
            if (showHistoryDropdown && historyDropdownRef.current && !historyDropdownRef.current.contains(event.target)) {
                const isOutsideHistoryButton = !historyButtonRef.current || !historyButtonRef.current.contains(event.target);
                if (isOutsideHistoryButton) {
                    setShowHistoryDropdown(false);
                }
            }

            // Tag dropdown click handling is managed inside NewEntryTag component
        };

        document.addEventListener('mousedown', handleClickOutside);

        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
        };
    }, [showTopModuleDropdown, showBottomModuleDropdown, showHistoryDropdown]);

    //actions
    const getEntryId = (journalData) => {
        return entryId != null ? entryId : (journalData ? journalData.entryId : journal.entryId);
    };

    const updateEntryHistory = (currentEntry) => {
        if (!currentEntry || !currentEntry.id || currentEntry.id === 0) return;
        if (typeof window === 'undefined') return;
        if (!Array.isArray(window.entryHistory)) {
            window.entryHistory = [];
        }
        const history = window.entryHistory;
        const last = history.length > 0 ? history[history.length - 1] : null;
        if (!last || last.id !== currentEntry.id) {
            history.push({
                id: currentEntry.id,
                journalId: currentEntry.journalId,
                title: currentEntry.title
            });
        }
        if (history.length > 1) {
            lastEntryRef.current = history[history.length - 2];
        } else {
            lastEntryRef.current = null;
        }
    };

    //#region Entry
    const fetchEntryDetails = async () => {
        try {
            setLoading(true);
            const api = Journals(session);
            const numericJournalId = parseInt(journalId);
            let journalData = journal;

            if (!journalData || journalData.id !== numericJournalId) {
                const journalResponse = await api.getJournal(journalId);

                if (!journalResponse.data.success) {
                    return showError(journalResponse.data.message || 'Failed to load journal details');
                }

                journalData = journalResponse.data.data;
                setJournal(journalData);
            }
            const newEntryId = getEntryId(journalData);
            const isNewEntry = newEntryId === 'new' || newEntryId == null;

            //navigation tracking
            if (typeof window !== 'undefined' && !Array.isArray(window.entryHistory)) {
                window.entryHistory = [];
            }

            if (!newEntryId && !snapshotId) {
                //create new entry for journal
                return loadNewJournal(journalData);
            }

            if (isNewEntry) {
                // Create a new entry template
                const newEntry = {
                    id: 0,
                    journalId: parseInt(journalId),
                    title: '',
                    description: '',
                    encrypted: false,
                    created: new Date().toISOString(),
                    status: 1
                };
                const newEntryJson = {
                    modules: [
                        {
                            id: generateRandomId(),
                            type: 'text-editor',
                            manuallyAdded: false,
                            html: '<p>Type or paste your content here!</p>'
                        }
                    ]
                };

                setupEntry(newEntry, newEntryJson, [], true);

            } else if (snapshotId) {
                // Load snapshot data
                const snapshotResponse = await getSnapshot(snapshotId);
                if (!snapshotResponse.data.success) {
                    return showError(snapshotResponse.data.message || 'Failed to load snapshot');
                }

                const snapshotData = snapshotResponse.data.data;
                snapshotData.id = snapshotData.entryId;
                setFromSnapshotId(parseInt(snapshotId));
                updateEntryHistory(snapshotData);

                // Parse and set snapshot content
                try {
                    const contentJson = JSON.parse(snapshotData.content || '{modules:[]}');
                    contentJson.modules?.forEach(module => {
                        delete module.manuallyAdded;
                        if (module.id == null) module.id = generateRandomId();
                    });
                    setupEntry(snapshotData, contentJson, [], false);
                } catch (parseErr) {
                    console.error('Error parsing snapshot content JSON:', parseErr);
                }

                // Disable editing for snapshots
                setIsEditing(false);
                window.scrollTo(0, 0);
            } else {
                // Get existing entry data
                const entryResponse = await api.getEntry(newEntryId);
                if (!entryResponse.data.success) {
                    return showError(entryResponse.data.message || 'Failed to load entry details');
                }

                const entryData = entryResponse.data.data.entry || entryResponse.data.data;
                const tagsData = entryResponse.data.data.tags || entryData.tags || [];

                // Fetch entry content (JSON data)
                try {
                    const contentResponse = await api.getEntryContent(newEntryId);
                    if (contentResponse.data.success && contentResponse.data.data) {
                        try {
                            const contentJson = JSON.parse(contentResponse.data?.data || '{modules:[]}');
                            contentJson.modules?.forEach(module => {
                                //remove unneccessary properties
                                delete module.manuallyAdded;
                                if (module.id == null) module.id = generateRandomId();
                            });
                            setupEntry(entryData, contentJson, tagsData, false);
                        } catch (parseErr) {
                            console.error('Error parsing entry content JSON:', parseErr);
                        }
                    }
                } catch (contentErr) {
                    console.error('Error fetching entry content:', contentErr);
                }
                window.scrollTo(0, 0);
            }
            if (location.search == '?edit') {
                setIsEditing(true);
            }
            setLoading(false);
        } catch (err) {
            console.error('Error fetching entry details:', err);
            showError('Failed to load entry details. Please try again later.');
        }
    };

    const setupEntry = (newEntry, newEntryJson, newTags, editing) => {
        entryRef.current = newEntry;
        entryJsonRef.current = newEntryJson;
        setEntry(newEntry);
        setEntryTags(newTags);
        setEditedTitle(newEntry?.title ?? '');
        setEditedDescription(newEntry?.description ?? '');
        setIsTitleEditing(editing);
        setIsEditing(editing);
        setEntryJson(newEntryJson);
        applyEntryCss(newEntryJson?.css);
        setSaveStatus(null);
        setError(null);
        setShowSettingsModal(false);
        setShowCreateSnapshotModal(false);
        setShowSnapshotCreatedModal(false);
        window.scrollTo(0, 0);
    }

    const loadNewJournal = async (journalData) => {
        const defaultJournalEntryJson = {
            modules: [
                {
                    id: generateRandomId(),
                    type: 'entries-list',
                    manuallyAdded: false
                }
            ]
        };

        const newEntryForJournal = {
            journalId: parseInt(journalId),
            title: journalData.title,
            description: journalData.description ?? '',
            status: 1
        };

        const createdResponse = await addEntry(newEntryForJournal);

        if (!createdResponse.data?.success || !createdResponse.data.data) {
            return showError(createdResponse.data?.message || 'Failed to create default journal entry');
        }

        const createdEntry = createdResponse.data.data;

        try {
            const contentString = JSON.stringify(defaultJournalEntryJson);
            await api.updateEntryContent(createdEntry.id, contentString);
        } catch (contentErr) {
            console.error('Error saving default journal entry content:', contentErr);
        }

        try {
            await updateJournalEntryId(parseInt(journalId), createdEntry.id);
            setJournal({ ...journalData, entryId: createdEntry.id });
        } catch (updateErr) {
            console.error('Error updating journal EntryId:', updateErr);
        }

        setupEntry(createdEntry, defaultJournalEntryJson, [], true);
        updateEntryHistory(createdEntry);
        setLoading(false);
    }

    const showError = (message) => {
        setError(message);
        setLoading(false);
    };

    const loadChapters = () => {
        getChapters(journalId).then(response => {
            if (response.data?.success && response.data.data) {
                setChapters(response.data.data);
            }
        });
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

    const applyEntryCss = (css) => {
        if (!css) return;
        let styleEl = document.getElementById('entry_css');
        if (!styleEl) {
            styleEl = document.createElement('style');
            styleEl.id = 'entry_css';
            document.head.appendChild(styleEl);
        }

        styleEl.textContent = css || '';
    };

    const saveEntryContent = async (json) => {
        setEntryJson(json);
        if (JSON.stringify(json) == JSON.stringify(entryJsonRef.current)) return;
        entryJsonRef.current = json;
        if (!entry || !entry.id || entry.id === 0 || json.modules == null || json.modules.length === 0) return;

        setSaveStatus('saving');
        try {
            const api = Journals(session);
            const contentString = JSON.stringify(json);

            const response = await api.updateEntryContent(entry.id, contentString);

            if (!response.data.success) {
                return showError(response.data.message || 'Failed to save entry content');
            }

            applyEntryCss(json.css);

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
    //#endregion

    //#region Entry Title
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
        const newEntryId = getEntryId();

        if (newEntryId !== 'new' && entry.title === editedTitle.trim()) {
            //title is the same, do nothing
            setIsTitleEditing(false);
            return;
        }
        if (saveStatus == 'saving') return;

        setSaveStatus('saving');

        try {
            if (newEntryId === 'new') {
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
                const defaultTagIds = location?.state?.defaultTagIds;
                if (Array.isArray(defaultTagIds) && defaultTagIds.length > 0) {
                    try {
                        await Promise.all(
                            defaultTagIds.map(tagId => addTagToEntry(createdEntry.id, tagId))
                        );
                    } catch (tagErr) {
                        console.error('Error applying default tags to new entry:', tagErr);
                    }
                }

                navigate(`/journal/${journalId}/entry/${createdEntry.id}?edit`, { replace: true });
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
        if (typeof window !== 'undefined' && Array.isArray(window.entryHistory) && window.entryHistory.length > 0) {
            window.entryHistory.pop();
        }
        navigate(`/journal/${journalId}`);
    };

    const handleBackToLastEntry = () => {
        if (typeof window !== 'undefined' && Array.isArray(window.entryHistory) && window.entryHistory.length > 0) {
            window.entryHistory.pop();
            const history = window.entryHistory;
            const previousEntry = history.length > 0 ? history[history.length - 1] : null;

            if (previousEntry) {
                navigate(`/journal/${journalId}/entry/${previousEntry.id}`);
                return;
            }
        }

        navigate(`/journal/${journalId}`);
    };

    const handleOnAddTag = async (tag) => {
        if (!entry || !entry.id || entry.id === 0 || !tag || tag.id == null) return;

        try {
            await addTagToEntry(entry.id, tag.id);
            setEntryTags(prev => {
                const exists = prev.some(t => t.tagId === tag.id);
                if (exists) return prev;
                return [...prev, { tagId: tag.id, name: tag.tag }];
            });
        } catch (err) {
            console.error('Error attaching tag to entry:', err);
        }
    }

    //#endregion

    //#region Status
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

    const getStatusTitle = (entry) => {
        if (!entry) return '';
        if (entry.encrypted && entry.status > 0) {
            return 'An encrypted and protected journal entry';
        }
        switch (entry.status) {
            case 0: return 'This entry has been archived';
            case 1: return 'This entry is active';
            case 2: return 'This entry has been published for public viewing';
            default: return '';
        }
    };

    const getChapter = () => {
        if (currentChapter.current) return currentChapter.current;
        if (!entry || !entry.chapterId || chapters.length === 0) return null;
        const chapter = chapters.find(ch => ch.chapterId === entry.chapterId);
        chapter.name = chapter ? `${chapter.sort}: ${chapter.title}` : null;
        currentChapter.current = chapter;
        return chapter;
    };

    const getChapterName = () => {
        const chapter = getChapter();
        return chapter?.name ?? '';
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
    //#endregion

    //#region Modules
    const generateRandomId = () => {
        return String(Math.floor(Math.random() * 1000000));
    };

    const addModule = (type, position = 'bottom') => {
        const newModule = {
            id: generateRandomId(),
            type: type,
            manuallyAdded: true
        };

        const newEntryJson = { ...entryJsonRef.current, 
            modules: [...(entryJsonRef.current?.modules ?? []), newModule] 
        };
        saveEntryContent(newEntryJson);

        // Close the appropriate dropdown based on which one was used
        if (position === 'top') {
            setShowTopModuleDropdown(false);
        } else {
            setShowBottomModuleDropdown(false);
        }
    };

    const handleUpdatedModule = (updatedModule) => {
        const modules = entryJsonRef.current.modules;
        const index = modules.findIndex(a => a.id == updatedModule.id);
        if (index > -1) {
            modules[index] = updatedModule;
            saveEntryContent({ ...entryJsonRef.current, modules });
        }
    };

    const handleDroppedModule = (json) => {
        saveEntryContent({ ...entryJsonRef.current, ...json });
    };

    const handleAddedModule = (newModule, targetModuleId) => {
        const moduleIndex = entryJsonRef.current.modules.findIndex(module => module.id === targetModuleId);
        if (moduleIndex === -1) return;

        const updatedModules = [...entryJsonRef.current.modules];
        updatedModules.splice(moduleIndex, 0, newModule);
        const updatedEntryJson = {
            ...entryJsonRef.current,
            modules: updatedModules
        };

        saveEntryContent(updatedEntryJson);
    };

    const handleRemovedModule = (moduleId, updatedModules) => {
        saveEntryContent({ ...entryJsonRef.current, modules: updatedModules });
    };
    //#endregion

    //#region Settings
    const handleOpenSettings = () => {
        setShowSettingsModal(true);
    };

    const handleCloseSettings = () => {
        setShowSettingsModal(false);
    };

    const handleSettingsSaved = (updatedEntry) => {
        setEntry(updatedEntry);
        entryRef.current = updatedEntry;
        currentChapterName.current = null; // Reset cached chapter name
        setSaveStatus('saved');
        setTimeout(() => setSaveStatus(null), 3000);
    };

    const handleChaptersChanged = (updatedChapters) => {
        setChapters(updatedChapters);
    };

    const handleSettingsEntryUpdate = async (newEntryJson) => {
        if (!newEntryJson || !newEntryJson.modules || newEntryJson.modules.length === 0) return;

        saveEntryContent(newEntryJson);
    };
    //#endregion

    //#region Snapshots
    const handleToggleHistoryDropdown = async () => {
        const newEntryId = getEntryId();
        if (!showHistoryDropdown) {
            // Load snapshots when opening dropdown
            setLoadingSnapshots(true);
            try {
                const response = await getSnapshotsByEntry(newEntryId);
                if (response.data?.success && response.data.data) {
                    setSnapshots(response.data.data);
                }
            } catch (err) {
                console.error('Error loading snapshots:', err);
            } finally {
                setLoadingSnapshots(false);
            }
        }
        setShowHistoryDropdown(!showHistoryDropdown);
    };

    const handleCreateSnapshotClick = () => {
        setShowHistoryDropdown(false);
        setShowCreateSnapshotModal(true);
    };

    const handleConfirmCreateSnapshot = async () => {
        setShowCreateSnapshotModal(false);
        try {
            const newEntryId = getEntryId();
            const response = await createSnapshot(newEntryId);
            if (response.data?.success) {
                setShowSnapshotCreatedModal(true);
                // Refresh snapshots list
                const snapshotsResponse = await getSnapshotsByEntry(newEntryId);
                if (snapshotsResponse.data?.success && snapshotsResponse.data.data) {
                    setSnapshots(snapshotsResponse.data.data);
                }
            }
        } catch (err) {
            console.error('Error creating snapshot:', err);
        }
    };

    const handleCancelCreateSnapshot = () => {
        setShowCreateSnapshotModal(false);
    };

    const handleCloseSnapshotCreated = () => {
        setShowSnapshotCreatedModal(false);
    };

    const handleSnapshotClick = (snapshot) => {
        setShowHistoryDropdown(false);
        // Navigate to snapshot route
        const newEntryId = getEntryId();
        navigate(`/journal/${journalId}/entry/${newEntryId}/snapshot/${snapshot.id}`);
    };

    const handleViewLatestVersion = () => {
        setShowHistoryDropdown(false);
        // Navigate to entry without snapshot
        const newEntryId = getEntryId();
        navigate(`/journal/${journalId}/entry/${newEntryId}`);
    };

    const formatSnapshotDate = (dateString) => {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-US', {
            month: 'short',
            day: 'numeric',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    };
    //#endregion

    //#region Secrets

    const showSecret = (e) => {
        e.preventDefault();
        e.target.classList.toggle('show-secret');
    };

    //add event listeners to secret content on component render
    if (secretsTimerRef.current) clearTimeout(secretsTimerRef.current);
    secretsTimerRef.current = setTimeout(() => {
        const els = document.querySelectorAll('.module .secret-content');
        els.forEach(el => {
            el.onclick = showSecret;
        });
    }, 500);

    const handleRemoveEntryTag = async (tag) => {
        if (!entry || !entry.id || !tag || tag.tagId == null) return;

        try {
            await removeTagFromEntry(entry.id, tag.tagId);
            setEntryTags(prev => prev.filter(t => t.tagId !== tag.tagId));
        } catch (err) {
            console.error('Error removing tag from entry:', err);
        }
    };

    //#endregion

    //#region Rendering

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

    // Render entry UI
    return (
        <div className={"journal-entry-page " + (isEditing ? " editing" : "preview")}>
            {showSettingsModal && (
                <SettingsModal
                    entry={entry}
                    entryJson={entryJson}
                    journalId={journalId}
                    chapters={chapters}
                    onClose={handleCloseSettings}
                    onSaved={handleSettingsSaved}
                    onUpdate={handleSettingsEntryUpdate}
                    onChaptersChanged={handleChaptersChanged}
                />
            )}
            {showCreateSnapshotModal && (
                <Modal title="Create Snapshot" onClose={handleCancelCreateSnapshot}>
                    <p className="snapshot-modal-text">Do you want to create a snapshot of the current state of this journal entry? It will create a copy of all current data & metadata related to this entry.</p>
                    <div className="buttons">
                        <button className="cancel" onClick={handleCancelCreateSnapshot}>Cancel</button>
                        <button onClick={handleConfirmCreateSnapshot}>Create Snapshot</button>
                    </div>
                </Modal>
            )}
            {showSnapshotCreatedModal && (
                <Modal title="Snapshot Created" onClose={handleCloseSnapshotCreated}>
                    <p>Your journal entry has been copied to a snapshot. You can view it in the history dropdown and revert to it in the future if needed.</p>
                    <div className="buttons">
                        <button onClick={handleCloseSnapshotCreated}>Okay</button>
                    </div>
                </Modal>
            )}
            <div className="entry-header">
                <div className="entry-navigation tool-bar">
                    {lastEntryRef.current ?
                        <button className="back-button" onClick={handleBackToLastEntry}>
                            <Icon name="arrow_back" /> Back to {lastEntryRef.current.title}
                        </button>

                        : journal.entryId != getEntryId() ? (
                            <button className="back-button" onClick={handleBackToJournal}>
                                <Icon name="arrow_back" /> Back to {journal?.title || 'Journal'}
                            </button>
                        ) : <></>}

                    <div className="right-side entry-status-badge">
                        {saveStatus && (
                            <div className={`save-status-message ${saveStatus === 'error' ? 'error' : 'success'}`}>
                                {getSaveStatusMessage()}
                            </div>
                        )}
                        <TagsList
                            tags={entryTags}
                            onRemoveTag={handleRemoveEntryTag}
                        />
                        {entryTags.length > 0 && (
                            <div className="tag-pointer">
                                <Icon name="chevron_left" />
                            </div>
                        )}
                        <NewEntryTag
                            entry={entry}
                            journalId={journalId}
                            onAddTag={handleOnAddTag}
                        />
                        {chapters.length > 0 && getChapterName() != '' && (
                            <span className="chapter-label" title={'Chapter #' + getChapter().sort}>
                                <Icon name="book" /> {getChapterName()}
                            </span>
                        )}
                        <span className={`status-indicator ${getStatusClass(entry)}`} title={getStatusTitle(entry)}>
                            {getStatusText(entry)}
                        </span>
                        <ToggleSwitch
                            name="edit-entry"
                            checked={isEditing}
                            onChange={setIsEditing}
                            label="Edit"
                        />
                        <div className="right-side history-dropdown-container">
                            <button
                                className="icon"
                                onClick={handleToggleHistoryDropdown}
                                ref={historyButtonRef}
                                title="View snapshot history"
                            >
                                <Icon name="history" />
                            </button>
                            {showHistoryDropdown && (
                                <div className="dropdown-menu" ref={historyDropdownRef}>
                                    <div className="tool-bar pad-sm">
                                        <button
                                            onClick={handleCreateSnapshotClick}
                                            title="Record the current state of this journal entry for historical records"
                                        >
                                            <Icon name="add" /> Create Snapshot
                                        </button>
                                    </div>
                                    {loadingSnapshots ? (
                                        <div className="loading-snapshots">
                                            <Icon name="progress_activity" spin={true} />
                                        </div>
                                    ) : snapshots.length === 0 ? (
                                        <div className="no-snapshots">
                                            No snapshots yet
                                        </div>
                                    ) : (
                                        <>
                                            {fromSnapshotId && (
                                                <div
                                                    className="dropdown-item"
                                                    onClick={handleViewLatestVersion}
                                                    title="View the current version of this entry"
                                                >
                                                    <div className="snapshot-date">
                                                        View Latest Version
                                                        <Icon name="today" />
                                                    </div>
                                                </div>
                                            )}
                                            {snapshots.map(snapshot => (
                                                <div
                                                    key={snapshot.id}
                                                    className={`dropdown-item ${fromSnapshotId === snapshot.id ? 'active' : ''}`}
                                                    onClick={() => handleSnapshotClick(snapshot)}
                                                >
                                                    <div className="snapshot-date">
                                                        {formatSnapshotDate(snapshot.createdSnapshot)}
                                                        <Icon name="history" />
                                                    </div>
                                                </div>
                                            ))}
                                        </>
                                    )}
                                </div>
                            )}
                        </div>
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

                {!loading && (
                    <ModuleList
                        entryJson={entryJson}
                        entryId={getEntryId()}
                        journalId={journalId}
                        journal={journal}
                        chapters={chapters}
                        isEditing={isEditing}
                        canDragDrop={isEditing}
                        updatedModule={handleUpdatedModule}
                        addedModule={handleAddedModule}
                        removedModule={handleRemovedModule}
                        droppedModule={handleDroppedModule}
                        fromSnapshotId={fromSnapshotId}
                    />
                )}

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

    //#endregion
}
