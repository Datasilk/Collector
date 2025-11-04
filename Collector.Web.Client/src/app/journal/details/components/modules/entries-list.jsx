import React, { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import Icon from '@/components/ui/icon';
import Modal from '@/components/ui/modal';
import Select from '@/components/forms/select';
import Checkbox from '@/components/forms/checkbox';
import Input from '@/components/forms/input';
import { useSession } from '@/context/session';
import { Journals } from '@/api/user/journals';
import { apiBasePath } from '@/helpers/endpoints.js';

export default function EntriesListModule({ module, journalId, isEditable = false, tabButtons }) {
    const navigate = useNavigate();
    const session = useSession();

    // state
    const [entries, setEntries] = useState([]);
    const [loading, setLoading] = useState(true);
    const [sort, setSort] = useState(() => {
        // Load sort from localStorage
        const savedSort = localStorage.getItem(`collector:journal:${journalId}:sort`);
        return savedSort || 'Title_asc';
    });
    const [showSettingsModal, setShowSettingsModal] = useState(false);
    const [viewType, setViewType] = useState(null);
    const [columns, setColumns] = useState({
        created: true,
        modified: true,
        status: true,
        chapter: true
    });
    const [tempViewType, setTempViewType] = useState(null);
    const [tempColumns, setTempColumns] = useState(null);
    const [chapters, setChapters] = useState([]);
    const [searchQuery, setSearchQuery] = useState('');

    //refs
    const viewTypeRef = useRef(null);
    const columnsRef = useRef(null);

    // effect
    useEffect(() => {
        fetchEntries();
        fetchChapters();
        loadSettings();
        if (tabButtons) tabButtons([
            {
                icon: 'settings',
                title: 'Settings',
                callback: handleShowSettingsModal
            }
        ]);
    }, [journalId]);

    // actions

    //#region "Entries"
    const fetchEntries = async () => {
        try {
            setLoading(true);
            const api = Journals(session);
            const response = await api.getEntries(journalId);
            if (response.data.success) {
                setEntries(response.data.data);
            }
            setLoading(false);
        } catch (err) {
            console.error('Error fetching entries:', err);
            setLoading(false);
        }
    };

    const fetchChapters = async () => {
        try {
            const api = Journals(session);
            const response = await api.getChapters(journalId);
            if (response.data?.success && response.data.data) {
                setChapters(response.data.data);
            }
        } catch (err) {
            console.error('Error fetching chapters:', err);
        }
    };

    const handleViewEntry = (entryId) => {
        navigate(`/journal/${journalId}/entry/${entryId}`);
    };

    const handleNewEntry = () => {
        navigate(`/journal/${journalId}/entry/new`);
    };

    const handleSortDropdown = (e) => {
        const newSort = e.target.value;
        setSort(newSort);
        localStorage.setItem(`collector:journal:${journalId}:sort`, newSort);
    };
    //#endregion

    //#region "Settings"
    const loadSettings = async () => {
        try {
            const api = Journals(session);
            const response = await api.getJournalSettings(journalId);
            if (response.data.success && response.data.data.entryList) {
                const settings = response.data.data.entryList;
                viewTypeRef.current = settings.viewType.toLowerCase();
                columnsRef.current = settings.columns || {
                    created: true,
                    modified: true,
                    status: true,
                    chapter: true
                };
                setViewType(viewTypeRef.current);
                setColumns(columnsRef.current);
            }
        } catch (err) {
            console.error('Error loading entry list settings:', err);
        }
    };

    const handleShowSettingsModal = () => {
        setTempViewType(viewTypeRef.current);
        setTempColumns({ ...columnsRef.current });
        setShowSettingsModal(true);
    };

    const handleCloseSettingsModal = () => {
        setShowSettingsModal(false);
    };

    const handleSaveSettings = async () => {
        try {
            const api = Journals(session);
            const entryListSettings = {
                viewType: tempViewType,
                columns: tempColumns
            };
            await api.updateEntryListSettings(journalId, entryListSettings);
            setViewType(tempViewType);
            setColumns(tempColumns);
            viewTypeRef.current = tempViewType;
            columnsRef.current = tempColumns;
            setShowSettingsModal(false);
        } catch (err) {
            console.error('Error saving entry list settings:', err);
        }
    };
    //#endregion

    //#region Details View
    const handleColumnToggle = (columnName, checked) => {
        setTempColumns({
            ...tempColumns,
            [columnName]: checked
        });
    };

    const handleSort = (field, currentSort) => {
        const [currentField, currentDirection] = currentSort.split('_');
        const direction = currentField === field && currentDirection === 'asc' ? 'desc' : 'asc';
        const newSort = `${field}_${direction}`;
        localStorage.setItem(`collector:journal:${journalId}:sort`, newSort);
        return newSort;
    };

    const getSortIcon = (field, currentSort) => {
        const [currentField, currentDirection] = currentSort.split('_');
        if (currentField !== field) return null;
        return currentDirection === 'asc' ? 'arrow_upward' : 'arrow_downward';
    };

    if (loading) {
        return (
            <div className="entries-list-module loading">
                <Icon name="progress_activity" spin={true} />
                <span>Loading entries...</span>
            </div>
        );
    }

    //#endregion

    //#region Helpers

    const formatDate = (dateString) => {
        if (!dateString) return 'N/A';
        const date = new Date(dateString);
        return date.toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'long',
            day: 'numeric'
        });
    };

    const getChapterText = (entry) => {
        if (!entry.chapterId) return '';
        const chapter = chapters.find(ch => ch.chapterId === entry.chapterId);
        if (!chapter) return '';
        return `${chapter.sort}: ${chapter.title}`;
    };

    const getStatusText = (entry) => {
        if (entry.status > 0 && entry.encrypted) {
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
        if (entry.status > 0 && entry.encrypted) {
            return 'status-private';
        }
        switch (entry.status) {
            case 0: return 'status-deleted';
            case 1: return 'status-active';
            case 2: return 'status-published';
            default: return '';
        }
    };

    const getThumbnailPath = (entry) => {
        if (!entry.thumbnail) return '';

        // Get the file extension
        const lastDotIndex = entry.thumbnail.lastIndexOf('.');
        if (lastDotIndex === -1) return entry.thumbnail; // No extension found

        // Insert "_thumb" before the extension
        const filenameWithoutExt = entry.thumbnail.substring(0, lastDotIndex);
        const extension = entry.thumbnail.substring(lastDotIndex);
        const thumbnailFilename = `${filenameWithoutExt}_thumb${extension}`;

        return apiBasePath() + `/image/journal-entries/${entry.id}/${thumbnailFilename}`;
    };

    // Filter entries based on search query
    const filteredEntries = entries.filter(entry => {
        if (!searchQuery) return true;
        const query = searchQuery.toLowerCase();
        return entry.title.toLowerCase().includes(query) ||
            (entry.description && entry.description.toLowerCase().includes(query));
    });

    // Sort entries based on current sort setting
    const sortedEntries = [...filteredEntries].sort((a, b) => {
        const [field, direction] = sort.split('_');
        const multiplier = direction === 'asc' ? 1 : -1;

        switch (field) {
            case 'Title':
                return multiplier * a.title.localeCompare(b.title);
            case 'Created':
                return multiplier * (new Date(a.created) - new Date(b.created));
            case 'Modified':
                return multiplier * (new Date(a.modified) - new Date(b.modified));
            case 'Status':
                return multiplier * (a.status - b.status);
            default:
                return 0;
        }
    });

    //#endregion


    //#region "Settings Modal"
    if (showSettingsModal && tempViewType !== null && tempColumns !== null) {
        return (
            <Modal title="Entry List Settings" onClose={handleCloseSettingsModal}>
                <Select
                    label="View"
                    name="view-type"
                    value={tempViewType}
                    onChange={(e) => setTempViewType(e.target.value)}
                    options={[
                        { label: 'Details', value: 'details' },
                        { label: 'Cards', value: 'cards' }
                    ]}
                />
                {tempViewType === 'details' && (
                    <div className="column-settings">
                        <h4>Visible Columns</h4>
                        <Checkbox
                            label="Date Created"
                            name="column-created"
                            checked={tempColumns.created}
                            onChange={(checked) => handleColumnToggle('created', checked)}
                        />
                        <Checkbox
                            label="Date Modified"
                            name="column-modified"
                            checked={tempColumns.modified}
                            onChange={(checked) => handleColumnToggle('modified', checked)}
                        />
                        <Checkbox
                            label="Status"
                            name="column-status"
                            checked={tempColumns.status}
                            onChange={(checked) => handleColumnToggle('status', checked)}
                        />
                        <Checkbox
                            label="Chapter"
                            name="column-chapter"
                            checked={tempColumns.chapter}
                            onChange={(checked) => handleColumnToggle('chapter', checked)}
                        />
                    </div>
                )}
                <div className="buttons">
                    <button onClick={handleSaveSettings}>Save</button>
                    <button className="cancel" onClick={handleCloseSettingsModal}>Cancel</button>
                </div>
            </Modal>);
    }
    //#endregion

    //#region "Empty State"
    if (entries.length === 0) {
        return (
            <div className="entries-list-module">
                <div className="empty-state">
                    <p>No entries yet. Create your first entry to get started!</p>
                    <button onClick={handleNewEntry}>
                        <Icon name="add" /> New Entry
                    </button>
                </div>
            </div>
        );
    }
    //#endregion

    //#region "Details View"
    if (!viewType || viewType == 'details') {
        return (
            <div className="entries-list-module">
                <div className="entries-table">
                    <table className="spreadsheet">
                        <thead>
                            <tr>
                                <th className="entry-title" onClick={() => setSort(handleSort('Title', sort))}>
                                    Title {getSortIcon('Title', sort) && <Icon name={getSortIcon('Title', sort)} />}
                                </th>
                                {columns.chapter && (
                                    <th className="entry-chapter">Chapter</th>
                                )}
                                {columns.created && (
                                    <th className="entry-created" onClick={() => setSort(handleSort('Created', sort))}>
                                        Created {getSortIcon('Created', sort) && <Icon name={getSortIcon('Created', sort)} />}
                                    </th>
                                )}
                                {columns.modified && (
                                    <th className="entry-modified" onClick={() => setSort(handleSort('Modified', sort))}>
                                        Modified {getSortIcon('Modified', sort) && <Icon name={getSortIcon('Modified', sort)} />}
                                    </th>
                                )}
                                {columns.status && (
                                    <th className="entry-status-column" onClick={() => setSort(handleSort('Status', sort))}>
                                        Status {getSortIcon('Status', sort) && <Icon name={getSortIcon('Status', sort)} />}
                                    </th>
                                )}
                                <th></th>
                            </tr>
                        </thead>
                        <tbody>
                            {sortedEntries.map(entry => (
                                <tr
                                    key={'tr_' + entry.id}
                                    onClick={() => handleViewEntry(entry.id)}
                                >
                                    <td className="entry-title">{entry.title}</td>
                                    {columns.chapter && <td className="entry-chapter">{getChapterText(entry)}</td>}
                                    {columns.created && <td className="entry-created">{formatDate(entry.created)}</td>}
                                    {columns.modified && <td className="entry-modified">{formatDate(entry.modified)}</td>}
                                    {columns.status && (
                                        <td className="entry-status-column">
                                            <span className={`entry-status ${getStatusClass(entry)}`}>
                                                {getStatusText(entry)}
                                            </span>
                                        </td>
                                    )}
                                    <td className="entry-tool-bar tool-bar align-right">
                                        <button
                                            className="icon"
                                            onClick={(e) => {
                                                e.stopPropagation();
                                                handleViewEntry(entry.id);
                                            }}
                                            title="View entry"
                                        >
                                            <Icon name="visibility" />
                                        </button>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            </div>
        );
    }
    //#endregion

    //#region "Cards View"
    if (viewType == 'cards') {
        return (
            <div className="entries-list-module">
                <div className="tool-bar cards-tool-bar">
                    <div className="left-side">
                        <Input
                            name="search"
                            type="text"
                            value={searchQuery}
                            placeholder="Search entries..."
                            onChange={(e) => setSearchQuery(e.target.value)}
                            buttons={[<button className="btn-search icon"><Icon name="search" /></button>]}
                        />
                    </div>
                    <div className="right-side">
                        <Select
                            name="sort"
                            value={sort}
                            onChange={handleSortDropdown}
                            options={[
                                { label: 'Title (A-Z)', value: 'Title_asc' },
                                { label: 'Title (Z-A)', value: 'Title_desc' },
                                { label: 'Created (Oldest)', value: 'Created_asc' },
                                { label: 'Created (Newest)', value: 'Created_desc' },
                                { label: 'Modified (Oldest)', value: 'Modified_asc' },
                                { label: 'Modified (Newest)', value: 'Modified_desc' },
                                { label: 'Status (Low-High)', value: 'Status_asc' },
                                { label: 'Status (High-Low)', value: 'Status_desc' }
                            ]}
                        />
                    </div>
                </div>
                <div className="entry-cards">
                    {sortedEntries.map(entry => (
                        <div
                            key={'entry-card_' + entry.id}
                            className={"entry-card" + (entry.thumbnail ? " has-thumbnail" : "")}
                            onClick={() => handleViewEntry(entry.id)}
                        >
                            <div className="entry-card-info">
                                <h3>{entry.title}</h3>
                                {getChapterText(entry) && (
                                    <span className="entry-chapter">
                                        <div className="chapter-label">
                                            <Icon name="book" />{getChapterText(entry)}
                                        </div>
                                    </span>)}
                                <span className="entry-created">{formatDate(entry.created)}</span>
                                <span className="entry-status">
                                    <span className={`entry-status ${getStatusClass(entry)}`}>
                                        {getStatusText(entry)}
                                    </span>
                                </span>
                            </div>
                            {entry.thumbnail && (
                                <div className="entry-card-thumbnail" style={{ backgroundImage: `url(${getThumbnailPath(entry)})` }}>
                                </div>
                            )}
                        </div>
                    ))}
                </div>
            </div>
        );
    }
    //#endregion
}
