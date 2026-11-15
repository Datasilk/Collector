import React, { useState, useEffect, useRef } from 'react';
import Icon from '@/components/ui/icon';
import Input from '@/components/forms/input';
import { useSession } from '@/context/session';
import { showMenu, hideMenu } from '@/context/modal';
import { JournalTags } from '@/api/user/journal-tags';

/**
 * <summary>Journal Entry Tag Dropdown Menu</summary>
 * <description>
 * Self-contained tag search/selection menu rendered via the modal provider's
 * showMenu (outside modal bounds). Owns its search state so it re-renders
 * independently while anchored to the + Tag button.
 * </description>
 */
function TagDropdownMenu({ journalId, onAddTag }) {
    const session = useSession();
    const { searchTags, createOrGetTag, getTags } = JournalTags(session);

    const [tagSearch, setTagSearch] = useState('');
    const [tagResults, setTagResults] = useState([]);
    const [tagSearchTimer, setTagSearchTimer] = useState(null);

    useEffect(() => {
        loadAllTags();
    }, [journalId]);

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
    };

    const handleAddTagSuggestionClick = () => {
        handleAddTagFromInput();
    };

    return (
        <div className="entry-type-dropdown">
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
        </div>
    );
}

/**
 * <summary>Journal Entry Tag Button & Dropdown</summary>
 * <description>
 * Renders the "+ Tag" button which opens the tag dropdown menu through the
 * modal provider's showMenu so it is not clipped by modal overflow. The parent
 * is notified when a tag is selected via onAddTag.
 * </description>
 */
export default function NewEntryTag({
    entry,
    journalId,
    onAddTag
}) {
    const [showTagDropdown, setShowTagDropdown] = useState(false);
    const tagButtonRef = useRef(null);

    useEffect(() => {
        if (!showTagDropdown) {
            hideMenu();
            return;
        }

        const handleMenuClose = () => setShowTagDropdown(false);
        const handleAddTag = (tag) => {
            onAddTag?.(tag);
            setShowTagDropdown(false);
        };

        showMenu(
            <TagDropdownMenu journalId={journalId} onAddTag={handleAddTag} />,
            tagButtonRef,
            { align: 'bottom', onClose: handleMenuClose }
        );

        return () => hideMenu();
    }, [showTagDropdown, journalId]);

    const handleToggleClick = () => {
        setShowTagDropdown(!showTagDropdown);
    };

    // When entry is omitted entirely, the tag picker works in "selection only"
    // mode (e.g. collecting tags before an entry exists). A null entry or an
    // entry without an id still hides the button as before.
    if (entry === null || (entry && (!entry.id || entry.id === 0))) return null;

    return (
        <div className="entry-tags-region">
            <div
                className="entry-tags-container entry-type-dropdown"
            >
                <button
                    ref={tagButtonRef}
                    onClick={handleToggleClick}
                    title="Add a new tag to your journal entry"
                >
                    <Icon name="add" /> Tag
                </button>
            </div>
        </div>
    );
}
