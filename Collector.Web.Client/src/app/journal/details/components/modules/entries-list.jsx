import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import Icon from '@/components/ui/icon';
import Modal from '@/components/ui/modal';
import Select from '@/components/forms/select';
import Checkbox from '@/components/forms/checkbox';
import { useSession } from '@/context/session';
import { Journals } from '@/api/user/journals';

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
    const [viewType, setViewType] = useState('Details');
    const [columns, setColumns] = useState({
        created: true,
        modified: true,
        status: true,
        chapter: true
    });
    const [tempViewType, setTempViewType] = useState('Details');
    const [tempColumns, setTempColumns] = useState({
        created: true,
        modified: true,
        status: true,
        chapter: true
    });
    const [chapters, setChapters] = useState([]);

    // effect
    useEffect(() => {
        fetchEntries();
        fetchChapters();
        loadSettings();
        if(tabButtons) tabButtons([
            {
                icon: 'settings',
                title: 'Settings',
                callback: handleShowSettingsModal
            }
        ]);
    }, [journalId]);

    // actions
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
                console.log(response.data.data);
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

    // Settings
    const loadSettings = async () => {
        try {
            const api = Journals(session);
            const response = await api.getJournalSettings(journalId);
            if (response.data.success && response.data.data.entryList) {
                const settings = response.data.data.entryList;
                setViewType(settings.viewType || 'Details');
                setColumns(settings.columns || { 
                    created: true, 
                    modified: true, 
                    status: true, 
                    chapter: true 
                });
            }
        } catch (err) {
            console.error('Error loading entry list settings:', err);
        }
    };

    const handleShowSettingsModal = () => {
        setTempViewType(viewType);
        setTempColumns({ ...columns });
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
            setShowSettingsModal(false);
        } catch (err) {
            console.error('Error saving entry list settings:', err);
        }
    };

    const handleColumnToggle = (columnName, checked) => {
        setTempColumns({
            ...tempColumns,
            [columnName]: checked
        });
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

    // Get chapter display text
    const getChapterText = (entry) => {
        console.log(entry);
        if (!entry.chapterId) return '';
        const chapter = chapters.find(ch => ch.chapterId === entry.chapterId);
        if (!chapter) return '';
        return `${chapter.sort}: ${chapter.title}`;
    };

    // Handle sorting
    const handleSort = (field, currentSort) => {
        const [currentField, currentDirection] = currentSort.split('_');
        const direction = currentField === field && currentDirection === 'asc' ? 'desc' : 'asc';
        const newSort = `${field}_${direction}`;
        localStorage.setItem(`collector:journal:${journalId}:sort`, newSort);
        return newSort;
    };

    // Get sort icon
    const getSortIcon = (field, currentSort) => {
        const [currentField, currentDirection] = currentSort.split('_');
        if (currentField !== field) return null;
        return currentDirection === 'asc' ? 'arrow_upward' : 'arrow_downward';
    };

    // Get status text
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

    // Sort entries based on current sort setting
    const sortedEntries = [...entries].sort((a, b) => {
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

    if (loading) {
        return (
            <div className="entries-list-module loading">
                <Icon name="progress_activity" spin={true} />
                <span>Loading entries...</span>
            </div>
        );
    }

    return (
        <div className="entries-list-module">
            {entries.length === 0 ? (
                <div className="empty-state">
                    <p>No entries yet. Create your first entry to get started!</p>
                    <button onClick={handleNewEntry}>
                        <Icon name="add" /> New Entry
                    </button>
                </div>
            ) : (
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
                                    key={entry.id}
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
            )}
            {showSettingsModal && (
                <Modal title="Entry List Settings" onClose={handleCloseSettingsModal}>
                    <Select
                        label="View"
                        name="view-type"
                        value={tempViewType}
                        onChange={(e) => setTempViewType(e.target.value)}
                        options={[
                            { label: 'Details', value: 'Details' },
                            { label: 'Cards', value: 'Cards' }
                        ]}
                    />
                    {tempViewType === 'Details' && (
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
                </Modal>
            )}
        </div>
    );
}
