import React, { useState, useRef } from 'react';
import Input from '@/components/forms/input';
import Select from '@/components/forms/select';
import Icon from '@/components/ui/icon';
import NewEntry from '../../components/new-entry';

export default function EntriesListFilter({ search, sort, onFilter, journalId }) {
    const [localSearch, setLocalSearch] = useState(search || '');
    const [localSort, setLocalSort] = useState(sort || 'Title_asc');
    const debounceTimerRef = useRef(null);

    const executeFilter = (newSearch, newSort) => {
        if (typeof onFilter === 'function') {
            onFilter({ search: newSearch, sort: newSort });
        }
    };

    const handleSearchInput = (e) => {
        const value = e.target.value;
        setLocalSearch(value);

        if (debounceTimerRef.current) {
            clearTimeout(debounceTimerRef.current);
        }

        const timer = setTimeout(() => {
            executeFilter(value, localSort);
        }, 3000);

        debounceTimerRef.current = timer;
    };

    const handleSearchKeyDown = (e) => {
        if (e.key === 'Enter') {
            if (debounceTimerRef.current) {
                clearTimeout(debounceTimerRef.current);
            }
            executeFilter(localSearch, localSort);
        }
    };

    const handleSearchClick = () => {
        if (debounceTimerRef.current) {
            clearTimeout(debounceTimerRef.current);
        }
        executeFilter(localSearch, localSort);
    };

    const handleSortChange = (e) => {
        const newSort = e.target.value;
        setLocalSort(newSort);
        executeFilter(localSearch, newSort);
    };

    return (
        <div className="entries-filter-bar tool-bar">
            <div className="filters left-side">
                <Input
                    name="search"
                    type="text"
                    value={localSearch}
                    placeholder="Search entries..."
                    onInput={handleSearchInput}
                    onKeyDown={handleSearchKeyDown}
                    buttons={[
                        <button
                            key="search-btn"
                            className="btn-search icon"
                            onClick={handleSearchClick}
                        >
                            <Icon name="search" />
                        </button>
                    ]}
                />
                <Select
                    name="sort"
                    value={localSort}
                    onChange={handleSortChange}
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
            <div className="right-side">
                <NewEntry journalId={journalId} />
            </div>
        </div>
    );
}
