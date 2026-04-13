import React, { useState, useEffect, useRef } from 'react';
import Icon from '@/components/ui/icon';
import Input from '@/components/forms/input';
import { useSession } from '@/context/session';
import { JournalTags } from '@/api/user/journal-tags';

/**
 * <summary>Journal Entry Tag Button & Dropdown</summary>
 * <description>
 * Renders the "+ Tag" button, tag search dropdown, and current tag pills
 * for a journal entry. All tag-related state, debounced search, click
 * behavior, and API calls are managed here. The parent is only notified
 * when the list of tags for the entry changes via onAddTag.
 * </description>
 */
export default function NewEntryTag({
    entry,
    journalId,
    onAddTag
}) {
    if (!entry || !entry.id || entry.id === 0) return null;

    const session = useSession();
    const { searchTags, createOrGetTag, getTags } = JournalTags(session);

    const [showTagDropdown, setShowTagDropdown] = useState(false);
    const [tagSearch, setTagSearch] = useState('');
    const [tagResults, setTagResults] = useState([]);
    const [tagSearchTimer, setTagSearchTimer] = useState(null);

    const tagDropdownRef = useRef(null);
    const tagButtonRef = useRef(null);

    useEffect(() => {
        loadAllTags();
    }, [journalId]);

    useEffect(() => {
        const handleClickOutside = (event) => {
            if (showTagDropdown && tagDropdownRef.current && !tagDropdownRef.current.contains(event.target)) {
                const isOutsideButton = !tagButtonRef.current || !tagButtonRef.current.contains(event.target);
                if (isOutsideButton) {
                    setShowTagDropdown(false);
                }
            }
        };

        document.addEventListener('mousedown', handleClickOutside);

        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
        };
    }, [showTagDropdown]);

    const loadAllTags = async () => {
        try {
            const response = await getTags(journalId);
            if (response.data?.success && Array.isArray(response.data.data)) {
                setTagResults(response.data.data.slice(0, 10));
            } else {
                setTagResults([]);
            }
        } catch (err) {
            console.error('Error loading tags:', err);
            setTagResults([]);
        }
    };

    // NewEntryTag no longer auto-loads entry tags; parent owns current tag list

    const handleToggleClick = () => {
        setShowTagDropdown(!showTagDropdown);
    };

    const handleTagSearchInput = (e) => {
        const value = e.target.value;
        setTagSearch(value);

        if (tagSearchTimer) {
            clearTimeout(tagSearchTimer);
        }

        const timer = setTimeout(() => {
            handleTagSearch(value);
        }, 300);

        setTagSearchTimer(timer);
    };

    const handleTagSearch = async (searchValue) => {
        const value = (searchValue ?? tagSearch).trim();
        if (!value) {
            await loadAllTags();
            return;
        }

        try {
            const response = await searchTags(journalId, value, 10);
            if (response.data?.success && response.data.data) {
                setTagResults(response.data.data);
            } else {
                setTagResults([]);
            }
        } catch (err) {
            console.error('Error searching tags:', err);
            setTagResults([]);
        }
    };

    const handleAddTagFromInput = async () => {
        const value = (tagSearch || '').trim();
        if (!value) return;

        try {
            const numericJournalId = parseInt(journalId);
            const createResponse = await createOrGetTag(numericJournalId, value);
            if (!createResponse.data?.success || !createResponse.data.data) return;

            const tag = createResponse.data.data;

            if (typeof onAddTag === 'function') {
                onAddTag(tag);
            }

            setTagSearch('');
            setTagResults([]);
        } catch (err) {
            console.error('Error adding tag to entry:', err);
        }
    };

    const handleTagInputKeyDown = (e) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            handleAddTagFromInput();
        }
    };

    const handleTagResultClick = (tag) => {
        if (typeof onAddTag === 'function') {
            onAddTag(tag);
        }

        setTagSearch('');
        setTagResults([]);
        setShowTagDropdown(false);
    };

    const handleAddTagSuggestionClick = () => {
        handleAddTagFromInput();
    };

    return (
        <div className="entry-tags-region">
            <div
                className="entry-tags-container entry-type-dropdown"
                ref={tagDropdownRef}
            >
                <button
                    ref={tagButtonRef}
                    onClick={handleToggleClick}
                    title="Add a new tag to your journal entry"
                >
                    <Icon name="add" /> Tag
                </button>
                {showTagDropdown && (
                    <div className="dropdown-menu entry-tags-dropdown">
                        <Input
                            name="entry-tag"
                            value={tagSearch}
                            onInput={handleTagSearchInput}
                            onKeyDown={handleTagInputKeyDown}
                            placeholder="enter a word or phrase"
                            formGroupClassName="entry-tag-input"
                            buttons={
                                <button
                                    type="button"
                                    className="icon"
                                    onClick={handleAddTagFromInput}
                                >
                                    <Icon name="add" />
                                </button>
                            }
                        />
                        <div className="tag-search-results">
                            {tagResults.map(tag => (
                                <div
                                    key={tag.id}
                                    className="dropdown-item tag-result-item"
                                    onClick={() => handleTagResultClick(tag)}
                                >
                                    <span>{tag.tag}</span>
                                </div>
                            ))}
                            {tagSearch && tagSearch.trim().length > 0 && tagResults.length === 0 && (
                                <div
                                    className="dropdown-item tag-result-item tag-result-add"
                                    onClick={handleAddTagSuggestionClick}
                                >
                                    Add tag "{tagSearch.trim()}" to the journal entry
                                </div>
                            )}
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}
