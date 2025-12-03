import React, { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import Icon from '@/components/ui/icon';
import { useSession } from '@/context/session';
import { Journals } from '@/api/user/journals';
import { apiBasePath } from '@/helpers/endpoints.js';
import EntriesListSettingsModal from './entries-list/entries-list-settings-modal';
import EntriesListFilter from './entries-list/entries-list-filter';
import EntriesListPaging from './entries-list/entries-list-paging';
import NewEntry from '../components/new-entry';

export default function EntriesListModule({ module, journalId, entryId, entry, hasUpdated, chapters, isEditable = false, tabButtons, onUpdate }) {
    const navigate = useNavigate();
    const session = useSession();

    // state
    const [entries, setEntries] = useState([]);
    const [loading, setLoading] = useState(true);
    const [sort, setSort] = useState(() => {
        // Load sort from localStorage
        const savedSort = localStorage.getItem(`collector:journal:${journalId}:${entryId}:sort`);
        return savedSort || 'Title_asc';
    });
    const [viewType, setViewType] = useState(null);
    const [columns, setColumns] = useState({
        created: true,
        modified: true,
        status: true,
        chapter: true
    });
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

        // initialize view type, columns, and page size from module settings
        const moduleViewType = (module?.viewType || 'details').toLowerCase();
        const moduleColumns = module?.columns || {
            created: true,
            modified: true,
            status: true,
            chapter: true
        };
        const pageSize = module?.paging?.total || filterOptionsRef.current.length;

        viewTypeRef.current = moduleViewType;
        columnsRef.current = moduleColumns;
        setViewType(moduleViewType);
        setColumns(moduleColumns);

        const nextFilter = {
            ...filterOptionsRef.current,
            length: pageSize
        };

        setFilterOptions(nextFilter);
        filterOptionsRef.current = nextFilter;

        // After settings are applied, fetch entries using the resolved filter
        filterEntries(nextFilter, updateTabButtons);
    }, [journalId, module]);

    useEffect(() => {
        updateTabButtons();
    }, [hasUpdated]);

    // keep filterOptionsRef in sync with state (fallback for other updates)
    useEffect(() => {
        filterOptionsRef.current = filterOptions;
    }, [filterOptions]);

    // actions

    //#region "Entries"
    const filterEntries = async (customFilter, callback) => {
        setLoading(true);
        const api = Journals(session);
        const requestFilter = customFilter || {
            ...filterOptions,
            sort: filterOptions.sort || sort
        };

        try {
            const filterTagIds = (customFilter?.tags || module?.tags || []);

            const response = await api.filterEntries(journalId, {
                Search: requestFilter.search || '',
                Sort: requestFilter.sort || sort,
                Start: requestFilter.start,
                Length: requestFilter.length,
                Tags: filterTagIds
            });

            if (response.data && response.data.success) {
                const data = response.data.data || {};
                const resultEntries = (data.entries || []).filter(a => a.id != entryId) || [];
                const total = data.totalCount || 0;

                setEntries(resultEntries);
                setTotalItems(total);
                setTotalPages(total > 0 ? Math.ceil(total / requestFilter.length) : 1);
            }

            if (callback) callback();
        } catch (err) {
            console.error('Error fetching entries:', err);
        } finally {
            setLoading(false);
        }
    };

    const handleViewEntry = (newEntry) => {
        const navOptions = {};
        const state = {};

        if (entryId) {
            state.parentEntryId = entryId;
            state.parentEntryName = entry.title;
        }

        if (Object.keys(state).length > 0) {
            navOptions.state = state;
        }

        navigate(`/journal/${journalId}/entry/${newEntry.id}`, navOptions);
    };

    //#endregion

    //#region "Settings"
    const updateTabButtons = () => {
        if (tabButtons) tabButtons([
            {
                icon: 'settings',
                title: 'Entries List Settings',
                callback: handleShowSettingsModal
            }
        ]);
    }

    const handleShowSettingsModal = () => {
        session.showModal(() => (
            <EntriesListSettingsModal
                journalId={journalId}
                entryId={entryId}
                module={module}
                defaultViewType={viewTypeRef.current}
                defaultColumns={columnsRef.current}
                defaultTotal={filterOptionsRef.current.length}
                onSaved={handleOnSavedSettings}
            />
        ));
    };

    const handleOnSavedSettings = (newViewType, newColumns, entriesPerPage, tagIds) => {
        setViewType(newViewType);
        setColumns(newColumns);
        viewTypeRef.current = newViewType;
        columnsRef.current = newColumns;

        // Build updated filter, optionally applying a new page size
        const newFilter = {
            ...filterOptions,
            length: entriesPerPage && entriesPerPage > 0 ? entriesPerPage : filterOptions.length,
            ...(Array.isArray(tagIds) ? { tags: tagIds } : {})
        };

        setFilterOptions(newFilter);
        filterOptionsRef.current = newFilter;

        // Re-filter entries using the updated page size
        filterEntries(newFilter);

        // Persist tags and page size on the module
        if (typeof onUpdate === 'function') {
            const updatedModule = {
                ...module,
                viewType: newViewType,
                columns: newColumns,
                ...(Array.isArray(tagIds) ? { tags: tagIds } : {}),
                ...(entriesPerPage && entriesPerPage > 0
                    ? {
                        paging: {
                            ...(module?.paging || {}),
                            total: entriesPerPage
                        }
                    }
                    : {})
            };
            onUpdate(updatedModule);
        }
    }
    //#endregion

    //#region Details View
    const handleSort = (field, currentSort) => {
        const [currentField, currentDirection] = currentSort.split('_');
        const direction = currentField === field && currentDirection === 'asc' ? 'desc' : 'asc';
        const newSort = `${field}_${direction}`;
        localStorage.setItem(`collector:journal:${journalId}:${entryId}:sort`, newSort);
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

    const getChapterText = (newEntry) => {
        if (!newEntry.chapterId) return '';
        const chapter = chapters.find(ch => ch.chapterId === newEntry.chapterId);
        if (!chapter) return '';
        return `${chapter.sort}: ${chapter.title}`;
    };

    const getStatusText = (newEntry) => {
        if (newEntry.status > 0 && newEntry.encrypted) {
            return 'Private';
        }
        switch (newEntry.status) {
            case 0: return 'Deleted';
            case 1: return 'Active';
            case 2: return 'Published';
            default: return 'Unknown';
        }
    };

    const getStatusClass = (newEntry) => {
        if (newEntry.status > 0 && newEntry.encrypted) {
            return 'status-private';
        }
        switch (newEntry.status) {
            case 0: return 'status-deleted';
            case 1: return 'status-active';
            case 2: return 'status-published';
            default: return '';
        }
    };

    const getThumbnailPath = (newEntry) => {
        if (!newEntry.thumbnail) return '';

        // Get the file extension
        const lastDotIndex = newEntry.thumbnail.lastIndexOf('.');
        if (lastDotIndex === -1) return newEntry.thumbnail; // No extension found

        // Insert "_thumb" before the extension
        const filenameWithoutExt = newEntry.thumbnail.substring(0, lastDotIndex);
        const extension = newEntry.thumbnail.substring(lastDotIndex);
        const thumbnailFilename = `${filenameWithoutExt}_thumb${extension}`;

        return apiBasePath() + `/image/journal-entries/${newEntry.id}/${thumbnailFilename}`;
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

    //#region loading

    if (loading) {
        return (<>
            <div className="entries-list-module">
                <div className="entries-filter-bar tool-bar">
                    <EntriesListFilter
                        search={filterOptions.search}
                        sort={sort}
                        onFilter={handleFilterChange}
                    />
                    <div className="right-side">
                        <NewEntry journalId={journalId} entryId={entryId} defaultTagIds={module?.tags || []} />
                    </div>
                </div>
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

    //#region "Empty State"
    if (!loading && entries.length === 0) {
        const hasSearch = (filterOptions.search || '').trim().length > 0;

        return (
            <div className="entries-list-module">
                {hasSearch && (
                    <div className="entries-filter-bar tool-bar">
                        <EntriesListFilter
                            search={filterOptions.search}
                            sort={sort}
                            onFilter={handleFilterChange}
                        />
                        <div className="right-side">
                            <NewEntry journalId={journalId} entryId={entryId} defaultTagIds={module?.tags || []} />
                        </div>
                        <div className="right-side">
                            <EntriesListPaging
                                start={filterOptions.start}
                                length={filterOptions.length}
                                totalItems={totalItems}
                                onFilter={handlePagingFilter}
                            />
                        </div>
                    </div>
                )}
                <div className="empty-state">
                    <p>
                        {hasSearch
                            ? 'No results found in your search filter.'
                            : 'No entries yet. Create your first entry to get started!'}
                    </p>
                    {!hasSearch && (
                        <div className="centered">
                            <NewEntry journalId={journalId} entryId={entryId} defaultTagIds={module?.tags || []} />
                        </div>
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
                <div className="entries-filter-bar tool-bar">
                    <EntriesListFilter
                        search={filterOptions.search}
                        sort={sort}
                        onFilter={handleFilterChange}
                    />
                    <div className="right-side">
                        <NewEntry journalId={journalId} entryId={entryId} defaultTagIds={module?.tags || []} />
                    </div>
                    <div className="right-side">
                        <EntriesListPaging
                            start={filterOptions.start}
                            length={filterOptions.length}
                            totalItems={totalItems}
                            onFilter={handlePagingFilter}
                        />
                    </div>
                </div>

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
                            {entries.map(newEntry => (
                                <tr
                                    key={'tr_' + newEntry.id}
                                    onClick={() => handleViewEntry(newEntry)}
                                >
                                    <td className="entry-title">{newEntry.title}</td>
                                    {columns.chapter && <td className="entry-chapter">{getChapterText(newEntry)}</td>}
                                    {columns.created && <td className="entry-created">{formatDate(newEntry.created)}</td>}
                                    {columns.modified && <td className="entry-modified">{formatDate(newEntry.modified)}</td>}
                                    {columns.status && (
                                        <td className="entry-status-column">
                                            <span className={`entry-status status-indicator ${getStatusClass(newEntry)}`}>
                                                {getStatusText(newEntry)}
                                            </span>
                                        </td>
                                    )}
                                    <td className="entry-tool-bar tool-bar align-right">
                                        <button
                                            className="icon"
                                            onClick={(e) => {
                                                e.stopPropagation();
                                                handleViewEntry(newEntry);
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
                <div className="entries-filter-bar tool-bar">
                    <EntriesListFilter
                        search={filterOptions.search}
                        sort={sort}
                        onFilter={handleFilterChange}
                    />
                    <div className="right-side">
                        <NewEntry journalId={journalId} entryId={entryId} defaultTagIds={module?.tags || []} />
                    </div>
                    <div className="right-side">
                        <EntriesListPaging
                            start={filterOptions.start}
                            length={filterOptions.length}
                            totalItems={totalItems}
                            onFilter={handlePagingFilter}
                        />
                    </div>
                </div>
                <div className="entry-cards">
                    {entries.map(newEntry => (
                        <div
                            key={'entry-card_' + newEntry.id}
                            className={"entry-card" + (newEntry.thumbnail ? " has-thumbnail" : "")}
                            onClick={() => handleViewEntry(newEntry)}
                        >
                            <div className="entry-card-info">
                                <h3>{newEntry.title}</h3>
                                {getChapterText(newEntry) && (
                                    <span className="entry-chapter">
                                        <div className="chapter-label">
                                            <Icon name="book" />{getChapterText(newEntry)}
                                        </div>
                                    </span>)}
                                <span className="entry-created">{formatDate(newEntry.created)}</span>
                                <span className="entry-status">
                                    <span className={`entry-status ${getStatusClass(newEntry)}`}>
                                        {getStatusText(newEntry)}
                                    </span>
                                </span>
                            </div>
                            {newEntry.thumbnail && (
                                <div className="entry-card-thumbnail" style={{ backgroundImage: `url(${getThumbnailPath(newEntry)})` }}>
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
