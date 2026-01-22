import React, { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate, useLocation } from 'react-router-dom';
//css
import './entry.css';
//components
import Icon from '@/components/ui/icon';
import ModuleList from './module-list';
import EntryToolbar from './components/entry-toolbar';
//context
import { useSession } from '@/context/session';
//api
import { Journals } from '@/api/user/journals';
import { JournalTags } from '@/api/user/journal-tags';
import { JournalSnapshots } from '@/api/user/journal-snapshots';

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
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [saveStatus, setSaveStatus] = useState(null);
    const [entryJson, setEntryJson] = useState({ modules: [] });
    const [isEditing, setIsEditing] = useState(false);
    const [chapters, setChapters] = useState([]);
    const [fromSnapshotId, setFromSnapshotId] = useState(null);

    // refs
    const entryRef = useRef(null);
    const entryJsonRef = useRef(null);
    const secretsTimerRef = useRef(null);
    const currentChapterName = useRef(null);
    const currentChapter = useRef(null);

    //apis
    const { addEntry, renameEntry, getChapters, updateJournalEntryId, 
        getJournal, getEntry, getEntryContent, setEntryParent, 
        updateEntryThumbnail, updateEntryContent } = Journals(session);
    const { getSnapshot } = JournalSnapshots(session);
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

    //actions
    const getEntryId = (journalData) => {
        if (journalData) {
            return entryId != null ? entryId : journalData.entryId;
        }
        if (entry && entry.id) return entry.id;
        return entryId != null ? entryId : journal?.entryId;
    };

    //#region Entry
    const fetchEntryDetails = async () => {
        try {
            setLoading(true);
            const numericJournalId = parseInt(journalId);
            let journalData = journal;

            if (!journalData || journalData.id !== numericJournalId) {
                const journalResponse = await getJournal(journalId);

                if (!journalResponse.data.success) {
                    return showError(journalResponse.data.message || 'Failed to load journal details');
                }

                journalData = journalResponse.data.data;
                setJournal(journalData);
            }
            const newEntryId = getEntryId(journalData);
            const isNewEntry = newEntryId === 'new' || newEntryId == null;

            if (!newEntryId && !snapshotId) {
                //create new entry for journal
                return loadNewJournal(journalData);
            }
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

            if (isNewEntry) {
                // Create a new entry template
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
                const entryResponse = await getEntry(newEntryId);
                if (!entryResponse.data.success) {
                    return showError(entryResponse.data.message || 'Failed to load entry details');
                }

                const entryData = entryResponse.data.data.entry || entryResponse.data.data;
                const tagsData = entryResponse.data.data.tags || entryData.tags || [];
                
                if (!entryData.parentEntryId && location?.state?.parentEntryId) {
                    try {
                        await setEntryParent(entryData.id, location.state.parentEntryId);
                        entryData.parentEntryId = location.state.parentEntryId;
                        if (location.state.parentEntryName) {
                            entryData.parentEntryName = location.state.parentEntryName;
                        }
                        setEntry(entryData);
                        entryRef.current = entryData;
                    } catch (parentErr) {
                        console.error('Error updating entry parent:', parentErr);
                    }
                }

                // Fetch entry content (JSON data)
                try {
                    const contentResponse = await getEntryContent(newEntryId);
                    if (contentResponse.data.success && contentResponse.data.data) {
                        try {
                            const contentJson = JSON.parse(contentResponse.data?.data || '{modules:[]}');
                            contentJson.modules?.forEach(module => {
                                //remove unneccessary properties
                                delete module.manuallyAdded;
                                if (module.id == null) module.id = generateRandomId();
                            });
                            setupEntry(entryData, contentJson, tagsData, false);
                            //check if we can get a thumbnail
                            checkEntryThumbnail(entryData, contentJson?.modules);
                        } catch (parseErr) {
                            console.error('Error parsing entry content JSON:', parseErr);
                        }
                    }else{
                        setupEntry(entryData, newEntryJson, [], true, false);
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

    const setupEntry = (newEntry, newEntryJson, newTags, editing, editingTitle = true) => {
        entryRef.current = newEntry;
        entryJsonRef.current = newEntryJson;
        setEntry(newEntry);
        setEditedTitle(newEntry?.title ?? '');
        setEditedDescription(newEntry?.description ?? '');
        setIsEditing(editing);
        setEntryJson(newEntryJson);
        applyEntryCss(newEntryJson?.css);
        setSaveStatus(null);
        setError(null); 
        window.scrollTo(0, 0);
        checkEntryThumbnail(newEntry, newEntryJson?.modules);
    }

    const checkEntryThumbnail = async (entryData, modules) => {
        if (!entryData || !modules) return;

        // If we have a thumbnailModuleId, check if that module's thumbnail has changed
        if (entryData.thumbnailModuleId) {
            const sourceModule = modules.find(m => m.id === entryData.thumbnailModuleId);
            if (sourceModule) {
                const moduleThumbnail = sourceModule.type === 'video-player' ? sourceModule.thumbnailPath : sourceModule.image;
                if (moduleThumbnail && moduleThumbnail !== entryData.thumbnail) {
                    // Thumbnail has changed, update it
                    await updateThumbnail(entryData, moduleThumbnail, entryData.thumbnailModuleId);
                }
            }
            return;
        }

        // No existing thumbnail or thumbnailModuleId - find first available
        if (entryData.thumbnail) return;

        // Find first video module with thumbnailPath
        const videoModule = modules.find(m => m.type === 'video-player' && m.thumbnailPath);
        if (videoModule) {
            await updateThumbnail(entryData, videoModule.thumbnailPath, videoModule.id);
            return;
        }

        // Find first image module with image
        const imageModule = modules.find(m => m.type === 'image' && m.image);
        if (imageModule) {
            await updateThumbnail(entryData, imageModule.image, imageModule.id);
        }
    };

    const updateThumbnail = async (entryData, thumbnail, moduleId = null) => {
        try {
            await updateEntryThumbnail(entryData.id, thumbnail, moduleId);
            setEntry({ ...entryData, thumbnail, thumbnailModuleId: moduleId });
            entryRef.current = { ...entryData, thumbnail, thumbnailModuleId: moduleId };
        } catch (err) {
            console.error('Error updating entry thumbnail:', err);
        }
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
            await updateEntryContent(createdEntry.id, contentString);
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
        if (JSON.stringify(json) == JSON.stringify(entryJsonRef.current)) {
            console.warn('No changes detected, skipping save', entryJsonRef.current);
            return;
        }
        entryJsonRef.current = json;
        
        // Skip save if entry is not fully loaded or has no modules
        if (!entry || !entry.id) {
            console.warn('saveEntryContent: entry not loaded yet', entry);
            return;
        }
        if (json.modules == null || json.modules.length === 0) {
            console.warn('saveEntryContent: no modules to save');
            return;
        }

        setSaveStatus('saving');
        try {
            const contentString = JSON.stringify(json);
            const response = await updateEntryContent(entry.id, contentString);

            if (!response.data.success) {
                console.error('saveEntryContent: API error', response.data);
                const errorMessage = response.data.message || '';
                const isFileInUseError = errorMessage.toLowerCase().includes('because it is being used by another process');
                if (isFileInUseError) {
                    setSaveStatus('save_failed_file_locked');
                    return;
                }
                return showError(errorMessage || 'Failed to save entry content');
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
    const updateEntryTitle = async () => {
        if (editedTitle.trim() === '') return;
        const newEntryId = getEntryId();

        if (newEntryId !== 'new' && entry.title === editedTitle.trim()) {
            //title is the same, do nothing
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
                    parentEntryId: location?.state?.parentEntryId ?? null,
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

                // Preserve noui and edit querystring parameters
                const searchParams = new URLSearchParams(window.location.search);
                const queryParams = [];
                if (searchParams.has('noui')) queryParams.push('noui');
                if (searchParams.has('edit')) queryParams.push('edit');
                const queryString = queryParams.length > 0 ? '?' + queryParams.join('&') : '?edit';

                navigate(`/journal/${journalId}/entry/${createdEntry.id}${queryString}`, { replace: true });
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
        }
    };

    const handleBackToJournal = () => {
        navigate(`/journal/${journalId}`);
    };

    //#endregion

    //#region Modules
    const generateRandomId = () => {
        return String(Math.floor(Math.random() * 1000000));
    };

    const addModule = (type) => {
        const newModule = {
            id: generateRandomId(),
            type: type,
            manuallyAdded: true
        };

        const newEntryJson = { ...entryJsonRef.current, 
            modules: [...(entryJsonRef.current?.modules ?? []), newModule] 
        };
        saveEntryContent(newEntryJson);
    };

    const handleUpdatedModule = (updatedModule) => {
        const updatedModules = [...entryJsonRef.current.modules];
        const index = updatedModules.findIndex(a => a.id == updatedModule.id);
        if (index > -1) {
            updatedModules[index] = updatedModule;
            saveEntryContent({ ...entryJsonRef.current, modules: updatedModules });
            checkEntryThumbnail(entryRef.current, updatedModules);
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
        checkEntryThumbnail(entryRef.current, updatedModules);
    };

    const handleRemovedModule = (moduleId, updatedModules) => {
        saveEntryContent({ ...entryJsonRef.current, modules: updatedModules });
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
            <EntryToolbar
                entry={entry}
                entryJson={entryJson}
                journal={journal}
                chapters={chapters}
                isEditing={isEditing}
                saveStatus={saveStatus}
                setEntry={setEntry}
                setChapters={setChapters}
                setIsEditing={setIsEditing}
                fromSnapshotId={fromSnapshotId}
                onUpdateTitle={updateEntryTitle}
                onGetEntryId={getEntryId}
                onAddModule={addModule}
                onUpdateEntryJson={saveEntryContent}
                title={editedTitle}
                setTitle={setEditedTitle}
                hasModules={entryJson.modules.length > 0}
            >
                <div className="entry-content">

                    {!loading && (
                        <ModuleList
                            entryJson={entryJson}
                            entryId={getEntryId()}
                            entry={entry}
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
                </div>
            </EntryToolbar>
        </div>
    );

    //#endregion
}
