import React from 'react';
import Icon from '@/components/ui/icon';

export default function TagsList({ tags, onRemoveTag }) {
    if (!tags || tags.length === 0) return null;

    const handleRemoveClick = (e) => {
        if (!onRemoveTag) return;
        const idValue = e.currentTarget.dataset.tagId;
        const tagId = idValue != null ? parseInt(idValue, 10) : null;
        if (!tagId) return;

        const tag = tags.find(t => t.tagId === tagId);
        if (tag) {
            onRemoveTag(tag);
        }
    };

    return (
        <div className="entry-tags-list">
            {tags.map(tag => (
                <span key={tag.tagId} className="entry-tag-pill">
                    {tag.name}
                    {onRemoveTag && (
                        <button
                            type="button"
                            className="icon"
                            data-tag-id={tag.tagId}
                            onClick={handleRemoveClick}
                            title="Remove tag"
                        >
                            <Icon name="close" />
                        </button>
                    )}
                </span>
            ))}
        </div>
    );
}
