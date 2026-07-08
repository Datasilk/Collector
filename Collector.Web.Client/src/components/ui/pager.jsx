import React from 'react';
import Icon from '@/components/ui/icon';
import './pager.css';

/**
 * <summary>Pager Component</summary>
 * <description>Simple pager with back/next and up to 5 numbered page buttons</description>
 * @param {number} props.currentPage Current active page (1-based)
 * @param {number} props.totalPages Total number of pages
 * @param {Function} props.onPageChange Called with the selected page number
 */
export default function Pager({ currentPage, totalPages, onPageChange }) {
    if (totalPages <= 1) return null;

    const handlePageChange = (page) => {
        if (page >= 1 && page <= totalPages && page !== currentPage) {
            onPageChange(page);
        }
    };

    const getPageNumbers = () => {
        const maxButtons = 5;
        let start = Math.max(1, currentPage - Math.floor(maxButtons / 2));
        let end = start + maxButtons - 1;

        if (end > totalPages) {
            end = totalPages;
            start = Math.max(1, end - maxButtons + 1);
        }

        const pages = [];
        for (let i = start; i <= end; i++) {
            pages.push(i);
        }
        return pages;
    };

    const pages = getPageNumbers();

    return (
        <div className="pager">
            <button
                className="icon"
                onClick={() => handlePageChange(currentPage - 1)}
                disabled={currentPage === 1}
                title="Back"
            >
                <Icon name="navigate_before" />
            </button>

            {pages.map(page => (
                <button
                    key={page}
                    className={currentPage === page ? 'active' : ''}
                    onClick={() => handlePageChange(page)}
                >
                    {page}
                </button>
            ))}

            <button
                className="icon"
                onClick={() => handlePageChange(currentPage + 1)}
                disabled={currentPage === totalPages}
                title="Next"
            >
                <Icon name="navigate_next" />
            </button>
        </div>
    );
}
