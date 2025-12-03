import React, { useState, useRef, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import Icon from '@/components/ui/icon';
import ToggleSwitch from '@/components/ui/toggle-switch';
import Modal from '@/components/ui/modal';
import Input from '@/components/forms/input';
import NewEntryTag from './new-entry-tag';
import TagsList from './tags-list';
import SettingsModal from './entry-settings-modal';
import modules from '../modules';
//context
import { useSession } from '@/context/session';
//api
import { JournalTags } from '@/api/user/journal-tags';
import { JournalSnapshots } from '@/api/user/journal-snapshots';

export default function EntryToolbar({
    entry,
    journal,
    chapters,
    isEditing,
    saveStatus,
    setEntry,
    setChapters,
    setIsEditing,
    fromSnapshotId,
    onUpdateTitle,
    onGetEntryId,
    onAddModule,
    title,
    setTitle,
    hasModules,
    children
}) {
    const session = useSession();
    const { journalId } = useParams();
    const navigate = useNavigate();
    const { addTagToEntry, removeTagFromEntry } = JournalTags(session);
    const { getSnapshotsByEntry, createSnapshot } = JournalSnapshots(session);
    const [showSettingsModal, setShowSettingsModal] = useState(false);
    const [showCreateSnapshotModal, setShowCreateSnapshotModal] = useState(false);
    const [showSnapshotCreatedModal, setShowSnapshotCreatedModal] = useState(false);
    const [showHistoryDropdown, setShowHistoryDropdown] = useState(false);
    const [localLoadingSnapshots, setLocalLoadingSnapshots] = useState(false);
    const [tags, setTags] = useState([]);
    const [snapshots, setSnapshots] = useState([]);

    const historyDropdownRef = useRef(null);
    const historyButtonRef = useRef(null);
    const titleInputRef = useRef(null);
    const topDropdownRef = useRef(null);
    const bottomDropdownRef = useRef(null);
    const dropdownButtonRef = useRef(null);
    const bottomDropdownButtonRef = useRef(null);

    const [showTopModuleDropdown, setShowTopModuleDropdown] = useState(false);
    const [showBottomModuleDropdown, setShowBottomModuleDropdown] = useState(false);
    const [isTitleEditing, setIsTitleEditing] = useState(false);

    const effectiveLoadingSnapshots = localLoadingSnapshots;

    useEffect(() => {
        setTags(entry?.tags || []);
    }, [entry?.id]);

    useEffect(() => {
        const handleClickOutside = (event) => {
            if (showTopModuleDropdown && topDropdownRef.current && !topDropdownRef.current.contains(event.target)) {
                const isOutsideTopButton = !dropdownButtonRef.current || !dropdownButtonRef.current.contains(event.target);
                if (isOutsideTopButton) {
                    setShowTopModuleDropdown(false);
                }
            }

            if (showBottomModuleDropdown && bottomDropdownRef.current && !bottomDropdownRef.current.contains(event.target)) {
                const isOutsideBottomButton = !bottomDropdownButtonRef.current || !bottomDropdownButtonRef.current.contains(event.target);
                if (isOutsideBottomButton) {
                    setShowBottomModuleDropdown(false);
                }
            }

            if (showHistoryDropdown && historyDropdownRef.current && !historyDropdownRef.current.contains(event.target)) {
                const isOutsideHistoryButton = !historyButtonRef.current || !historyButtonRef.current.contains(event.target);
                if (isOutsideHistoryButton) {
                    setShowHistoryDropdown(false);
                }
            }
        };

        document.addEventListener('mousedown', handleClickOutside);
        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
        };
    }, [showTopModuleDropdown, showBottomModuleDropdown, showHistoryDropdown]);

    useEffect(() => {
        if (isTitleEditing && titleInputRef.current) {
            titleInputRef.current.focus();
        }
    }, [isTitleEditing]);

    const handleOpenSettings = () => {
        setShowSettingsModal(true);
    };

    const handleCloseSettings = () => {
        setShowSettingsModal(false);
    };

    const handleSettingsSavedInternal = (updatedEntry) => {
        setEntry(updatedEntry);
    };

    const handleChaptersChangedInternal = (updatedChapters) => {
        setChapters(updatedChapters);
    };

    const handleSettingsEntryUpdateInternal = async (newEntryJson) => {
        // SettingsModal will call back with updated entryJson; parent is responsible
        // for persisting via saveEntryContent through a higher-level mechanism.
        // For now, just forward via setEntryJson if needed externally.
    };

    const handleToggleHistoryDropdown = async () => {
        const currentEntryId = onGetEntryId();
        if (!showHistoryDropdown) {
            setLocalLoadingSnapshots(true);
            try {
                const response = await getSnapshotsByEntry(currentEntryId);
                if (response.data?.success && response.data.data) {
                    setSnapshots(response.data.data);
                }
            } finally {
                setLocalLoadingSnapshots(false);
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
            const currentEntryId = onGetEntryId();
            const response = await createSnapshot(currentEntryId);
            if (response.data?.success) {
                setShowSnapshotCreatedModal(true);
                const snapshotsResponse = await getSnapshotsByEntry(currentEntryId);
                if (snapshotsResponse.data?.success && snapshotsResponse.data.data) {
                    setSnapshots(snapshotsResponse.data.data);
                }
            }
        } catch (err) {
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
        const currentEntryId = onGetEntryId();
        navigate(`/journal/${journalId}/entry/${currentEntryId}/snapshot/${snapshot.id}`);
    };

    const handleViewLatestVersion = () => {
        setShowHistoryDropdown(false);
        const currentEntryId = onGetEntryId();
        navigate(`/journal/${journalId}/entry/${currentEntryId}`);
    };

    const handleTitleEdit = () => {
        setIsTitleEditing(true);
    };

    const handleTitleKeyDown = (e) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            onUpdateTitle();
            setIsTitleEditing(false);
            setIsEditing(false);
        } else if (e.key === 'Escape') {
            e.preventDefault();
            setIsTitleEditing(false);
            setTitle(entry.title);
        }
    };

    const handleTitleBlur = () => {
        onUpdateTitle();
    };

    const handleTitleInput = (e) => {
        setTitle(e.target.value);
    };

    const handleTopAddContentClick = () => {
        if (showBottomModuleDropdown) {
            setShowBottomModuleDropdown(false);
        }
        setShowTopModuleDropdown(!showTopModuleDropdown);
    };

    const handleBottomAddContentClick = () => {
        if (showTopModuleDropdown) {
            setShowTopModuleDropdown(false);
        }
        setShowBottomModuleDropdown(!showBottomModuleDropdown);
    };

    const handleAddModuleTop = (type) => {
        onAddModule(type, 'top');
    };

    const handleAddModuleBottom = (type) => {
        onAddModule(type, 'bottom');
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

    const getStatusText = (entryItem) => {
        if (entryItem.encrypted && entryItem.status > 0) {
            return 'Private';
        }
        switch (entryItem.status) {
            case 0: return 'Deleted';
            case 1: return 'Active';
            case 2: return 'Published';
            default: return 'Unknown';
        }
    };

    const getStatusClass = (entryItem) => {
        if (entryItem.encrypted && entryItem.status > 0) {
            return 'status-private';
        }
        switch (entryItem.status) {
            case 0: return 'status-deleted';
            case 1: return 'status-active';
            case 2: return 'status-published';
            default: return '';
        }
    };

    const getStatusTitle = (entryItem) => {
        if (!entryItem) return '';
        if (entryItem.encrypted && entryItem.status > 0) {
            return 'An encrypted and protected journal entry';
        }
        switch (entryItem.status) {
            case 0: return 'This entry has been archived';
            case 1: return 'This entry is active';
            case 2: return 'This entry has been published for public viewing';
            default: return '';
        }
    };

    const getChapter = () => {
        if (!entry || !entry.chapterId || chapters.length === 0) return null;
        const chapter = chapters.find(ch => ch.chapterId === entry.chapterId);
        if (!chapter) return null;
        return {
            ...chapter,
            name: `${chapter.sort}: ${chapter.title}`
        };
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

    return (
        <>
            {showSettingsModal && (
                <SettingsModal
                    entry={entry}
                    entryJson={null}
                    journalId={journalId}
                    chapters={chapters}
                    onClose={handleCloseSettings}
                    onSaved={handleSettingsSavedInternal}
                    onUpdate={handleSettingsEntryUpdateInternal}
                    onChaptersChanged={handleChaptersChangedInternal}
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
                    {entry && entry.parentEntryId ? (
                        <button
                            className="back-button"
                            onClick={() => navigate(`/journal/${journalId}/entry/${entry.parentEntryId}`)}
                        >
                            <Icon name="arrow_back" /> Back to {entry.parentEntryName || 'Parent Entry'}
                        </button>
                    ) : (
                        journal.entryId != onGetEntryId() ? (
                            <button
                                className="back-button"
                                onClick={() => navigate(`/journal/${journalId}`)}
                            >
                                <Icon name="arrow_back" /> Back to {journal?.title || 'Journal'}
                            </button>
                        ) : <></>
                    )}

                    <div className="right-side entry-status-badge">
                        {saveStatus && (
                            <div className={`save-status-message ${saveStatus === 'error' ? 'error' : 'success'}`}>
                                {getSaveStatusMessage()}
                            </div>
                        )}
                        <TagsList
                            tags={tags}
                            onRemoveTag={async (tag) => {
                                if (!entry || !entry.id || !tag || tag.tagId == null) return;
                                try {
                                    await removeTagFromEntry(entry.id, tag.tagId);
                                    setTags(prev => prev.filter(t => t.tagId !== tag.tagId));
                                    setEntry(prev => prev ? { ...prev, tags: prev.tags?.filter(t => t.tagId !== tag.tagId) } : prev);
                                } catch (err) {
                                    console.error('Error removing tag from entry:', err);
                                }
                            }}
                        />
                        {tags.length > 0 && (
                            <div className="tag-pointer">
                                <Icon name="chevron_left" />
                            </div>
                        )}
                        <NewEntryTag
                            entry={entry}
                            journalId={journalId}
                            onAddTag={async (tag) => {
                                if (!entry || !entry.id || entry.id === 0 || !tag || tag.id == null) return;
                                try {
                                    await addTagToEntry(entry.id, tag.id);
                                    setTags(prev => {
                                        const exists = prev.some(t => t.tagId === tag.id);
                                        if (exists) return prev;
                                        return [...prev, { tagId: tag.id, name: tag.tag }];
                                    });
                                    setEntry(prev => prev ? {
                                        ...prev,
                                        tags: (prev.tags || []).some(t => t.tagId === tag.id)
                                            ? prev.tags
                                            : [...(prev.tags || []), { tagId: tag.id, name: tag.tag }]
                                    } : prev);
                                } catch (err) {
                                    console.error('Error attaching tag to entry:', err);
                                }
                            }}
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
                                    {effectiveLoadingSnapshots ? (
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
                                                        {formatDate(snapshot.createdSnapshot)}
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
                            value={title}
                            onInput={handleTitleInput}
                            onKeyDown={handleTitleKeyDown}
                            onBlur={handleTitleBlur}
                            placeholder="Enter entry title"
                            ref={titleInputRef}
                            buttons={
                                <span className="tool-bar">
                                    <button className="icon" onClick={() => onUpdateTitle()}>
                                        <Icon name="check" />
                                    </button>
                                    <button className="icon icon-close" onClick={() => {
                                        setIsTitleEditing(false);
                                        setTitle(entry.title);
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
                                    onClick={handleTopAddContentClick}
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
                                                onClick={() => handleAddModuleTop(module.type)}
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
            {children}

            {hasModules && isEditing && (
                <div className="tool-bar add-module-container bottom-add-module">
                    <div className="right-side">
                        <button
                            ref={bottomDropdownButtonRef}
                            onClick={handleBottomAddContentClick}
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
                                        onClick={() => handleAddModuleBottom(module.type)}
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
        </>
    );
}
