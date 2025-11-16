import React from 'react';
import Icon from '@/components/ui/icon';

export default function EntriesListPaging({ start, length, totalItems, onFilter }) {
    if (!length || length <= 0 || !totalItems || totalItems <= length) {
        return null;
    }

    const totalPages = Math.ceil(totalItems / length);
    const currentPage = Math.floor(start / length);

    const canPrev = currentPage > 0;
    const canNext = currentPage + 1 < totalPages;

    const handlePrevClick = () => {
        if (!canPrev) return;
        const newStart = (currentPage - 1) * length;
        if (typeof onFilter === 'function') {
            onFilter(newStart);
        }
    };

    const handleNextClick = () => {
        if (!canNext) return;
        const newStart = (currentPage + 1) * length;
        if (typeof onFilter === 'function') {
            onFilter(newStart);
        }
    };

    return (
        <div className="tool-bar center pagination">
            <button
                onClick={handlePrevClick}
                disabled={!canPrev}
                aria-label="Previous"
                className={!canPrev ? 'disabled' : ''}
            >
                <Icon name="chevron_left" />
            </button>
            <span className="page-info">{currentPage + 1} / {totalPages}</span>
            <button
                onClick={handleNextClick}
                disabled={!canNext}
                aria-label="Next"
                className={!canNext ? 'disabled' : ''}
            >
                <Icon name="chevron_right" />
            </button>
        </div>
    );
}
