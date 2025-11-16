import React, { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import Icon from '@/components/ui/icon';
import { useSession } from '@/context/session';
import { Journals } from '@/api/user/journals';
import { apiBasePath } from '@/helpers/endpoints.js';
import EntriesListSettingsModal from './entries-list/entries-list-settings-modal';
import EntriesListFilter from './entries-list/entries-list-filter';
import EntriesListPaging from './entries-list/entries-list-paging';

export default function EntriesListModule({ module, journalId, isEditable = false, tabButtons, onUpdate }) {
    const navigate = useNavigate();
    const session = useSession();

    // state
    const [entries, setEntries] = useState([]);
    const [loading, setLoading] = useState(true);
    const [filterWhileLoading, setFilterWhileLoading] = useState(false);
    const [sort, setSort] = useState(() => {
        // Load sort from localStorage
        const savedSort = localStorage.getItem(`collector:journal:${journalId}:sort`);
        return savedSort || 'Title_asc';
    });
    const [viewType, setViewType] = useState(null);
    const [columns, setColumns] = useState({
        created: true,
        modified: true,
        status: true,
        chapter: true
    });
    const [chapters, setChapters] = useState([]);
    const [filterOptions, setFilterOptions] = useState({
        search: '',
        sort: null,
        start: 0,
        length: module?.paging?.total || 20
    });
    const [totalItems, setTotalItems] = useState(0);
    const [totalPages, setTotalPages] = useState(1);

    //refs
    const viewTypeRef = useRef(null);
    const columnsRef = useRef(null);
    const filterOptionsRef = useRef(filterOptions);

    // effect
    useEffect(() => {
        // initialize filter sort from current sort state
        setFilterOptions(prev => {
            const updated = {
                ...prev,
                sort: sort,
                start: 0
            };
            filterOptionsRef.current = updated;
            return updated;
        });

        fetchChapters();
        loadSettings();
    }, [journalId]);

    // keep filterOptionsRef in sync with state (fallback for other updates)
    useEffect(() => {
        filterOptionsRef.current = filterOptions;
    }, [filterOptions]);

    // actions

    //#region "Entries"
    const filterEntries = (customFilter, callback) => {
        setLoading(true);
        const api = Journals(session);
        const requestFilter = customFilter || {
            ...filterOptions,
            sort: filterOptions.sort || sort
        };

        api.filterEntries(journalId, {
            Search: requestFilter.search || '',
            Sort: requestFilter.sort || sort,
            Start: requestFilter.start,
            Length: requestFilter.length
        })
            .then(response => {
                if (response.data && response.data.success) {
                    const data = response.data.data || {};
                    setEntries(data.entries || []);
                    const total = data.totalCount || 0;
                    setTotalItems(total);
                    setTotalPages(total > 0 ? Math.ceil(total / requestFilter.length) : 1);
                    if (total > 0 && filterWhileLoading == false) {
                        setFilterWhileLoading(true);
                    }
                }
                setLoading(false);
                if (callback) callback();
            })
            .catch(err => {
                console.error('Error fetching entries:', err);
                setLoading(false);
            });
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

    //#endregion

    //#region "Settings"
    const loadSettings = async () => {
        try {
            const api = Journals(session);
            const response = await api.getJournalSettings(journalId);
            let nextFilter = null;

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
                const totalEntries = settings.entriesPerPage;
                nextFilter = { ...filterOptionsRef.current, length: totalEntries };
                setFilterOptions(nextFilter);
                filterOptionsRef.current = nextFilter;
            } else {
                // No settings file yet, just use current filterOptions with start reset
                nextFilter = { ...filterOptionsRef.current, start: 0 };
                setFilterOptions(nextFilter);
                filterOptionsRef.current = nextFilter;
            }

            // After settings (if any) are applied, fetch entries using the resolved filter
            filterEntries(nextFilter || filterOptions, () => {
                if (tabButtons) tabButtons([
                    {
                        icon: 'settings',
                        title: 'Settings',
                        callback: handleShowSettingsModal
                    }
                ]);
            });
        } catch (err) {
            console.error('Error loading entry list settings:', err);
        }
    };

    const handleShowSettingsModal = () => {
        session.showModal(() => (
            <EntriesListSettingsModal
                journalId={journalId}
                defaultViewType={viewTypeRef.current}
                defaultColumns={columnsRef.current}
                defaultTotal={filterOptionsRef.current.length}
                onSaved={(newViewType, newColumns, entriesPerPage) => {
                    setViewType(newViewType);
                    setColumns(newColumns);
                    viewTypeRef.current = newViewType;
                    columnsRef.current = newColumns;

                    // Build updated filter, optionally applying a new page size
                    const newFilter = {
                        ...filterOptions,
                        length: entriesPerPage && entriesPerPage > 0 ? entriesPerPage : filterOptions.length
                    };

                    setFilterOptions(newFilter);
                    filterOptionsRef.current = newFilter;

                    // Re-filter entries using the updated page size
                    filterEntries(newFilter);

                    // Persist page size on the module when provided
                    if (entriesPerPage && entriesPerPage > 0 && typeof onUpdate === 'function') {
                        const updatedModule = {
                            ...module,
                            paging: {
                                ...(module?.paging || {}),
                                total: entriesPerPage
                            }
                        };
                        onUpdate(updatedModule);
                    }
                }}
            />
        ));
    };
    //#endregion

    //#region Details View
    const handleSort = (field, currentSort) => {
        const [currentField, currentDirection] = currentSort.split('_');
        const direction = currentField === field && currentDirection === 'asc' ? 'desc' : 'asc';
        const newSort = `${field}_${direction}`;
        localStorage.setItem(`collector:journal:${journalId}:sort`, newSort);
        setSort(newSort);

        const newFilter = {
            ...filterOptions,
            sort: newSort,
            start: 0
        };
        setFilterOptions(newFilter);
        filterOptionsRef.current = newFilter;
        filterEntries(newFilter);

        return newSort;
    };

    const getSortIcon = (field, currentSort) => {
        const [currentField, currentDirection] = currentSort.split('_');
        if (currentField !== field) return null;
        return currentDirection === 'asc' ? 'arrow_upward' : 'arrow_downward';
    };

    if (loading) {
        return (<>
            <div className="entries-list-module">
                <EntriesListFilter
                    search={filterOptions.search}
                    sort={sort}
                    onFilter={handleFilterChange}
                    journalId={journalId}
                />
            </div>
            <div className="entries-list-module loading">

                <div className="loading-state">
                    <Icon name="progress_activity" spin={true} />
                    <span>Loading entries...</span>
                </div>
            </div>
        </>);
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

    const handlePagingFilter = (newStart) => {
        const newFilter = {
            ...filterOptions,
            start: newStart
        };
        setFilterOptions(newFilter);
        filterOptionsRef.current = newFilter;
        filterEntries(newFilter);
    };

    //#endregion

    //#region "Empty State"
    if (!loading && entries.length === 0) {
        const hasSearch = (filterOptions.search || '').trim().length > 0;

        return (
            <div className="entries-list-module">
                <EntriesListFilter
                    search={filterOptions.search}
                    sort={sort}
                    onFilter={handleFilterChange}
                    journalId={journalId}
                />
                <div className="empty-state">
                    <p>
                        {hasSearch
                            ? 'No results found in your search filter.'
                            : 'No entries yet. Create your first entry to get started!'}
                    </p>
                    {!hasSearch && (
                        <button onClick={handleNewEntry}>
                            <Icon name="add" /> New Entry
                        </button>
                    )}
                </div>
            </div>
        );
    }
    //#endregion

    //#region "Details View"
    function handleFilterChange({ search, sort: newSort }) {
        const sortValue = newSort || sort;

        setSort(sortValue);
        localStorage.setItem(`collector:journal:${journalId}:sort`, sortValue);

        const newFilter = {
            ...filterOptions,
            search: search != null ? search : filterOptions.search,
            sort: sortValue,
            start: 0
        };

        setFilterOptions(newFilter);
        filterOptionsRef.current = newFilter;
        filterEntries(newFilter);
    }

    if (!viewType || viewType == 'details') {
        return (
            <div className="entries-list-module">
                <EntriesListFilter
                    search={filterOptions.search}
                    sort={sort}
                    onFilter={handleFilterChange}
                    journalId={journalId}
                />

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
                            {entries.map(entry => (
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
                <EntriesListPaging
                    start={filterOptions.start}
                    length={filterOptions.length}
                    totalItems={totalItems}
                    onFilter={handlePagingFilter}
                />
            </div>
        );
    }
    //#endregion

    //#region "Cards View"
    if (viewType == 'cards') {
        return (
            <div className="entries-list-module">
                <EntriesListFilter
                    search={filterOptions.search}
                    sort={sort}
                    onFilter={handleFilterChange}
                    journalId={journalId}
                />
                <div className="entry-cards">
                    {entries.map(entry => (
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
                <EntriesListPaging
                    start={filterOptions.start}
                    length={filterOptions.length}
                    totalItems={totalItems}
                    onFilter={handlePagingFilter}
                />
            </div>
        );
    }
    //#endregion
}
