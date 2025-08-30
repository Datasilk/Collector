import React from 'react';
import Icon from '@/components/ui/icon';
import './pagination.css';

/**
 * <summary>Pagination Component</summary>
 * <description>Reusable pagination component with page numbers</description>
 * @param {Object} props Component props
 * @param {number} props.currentPage Current active page (1-based)
 * @param {number} props.totalPages Total number of pages
 * @param {number} props.pageSize Number of items per page
 * @param {number} props.totalItems Total number of items
 * @param {Function} props.onPageChange Function to call when page changes
 * @param {number} props.maxPageNumbers Maximum number of page numbers to show (default: 5)
 */
export default function Pagination({ 
    currentPage, 
    totalPages, 
    pageSize, 
    totalItems, 
    onPageChange,
    maxPageNumbers = 5
}) {
    if (totalPages <= 1) return null;

    // Calculate the range of page numbers to display
    const getPageNumbers = () => {
        const pageNumbers = [];
        
        // Calculate the start and end page numbers
        let startPage = Math.max(1, currentPage - Math.floor(maxPageNumbers / 2));
        let endPage = startPage + maxPageNumbers - 1;
        
        // Adjust if endPage exceeds totalPages
        if (endPage > totalPages) {
            endPage = totalPages;
            startPage = Math.max(1, endPage - maxPageNumbers + 1);
        }
        
        // Generate the page numbers
        for (let i = startPage; i <= endPage; i++) {
            pageNumbers.push(i);
        }
        
        return pageNumbers;
    };

    const handlePageChange = (page) => {
        if (page !== currentPage && page >= 1 && page <= totalPages) {
            onPageChange(page);
        }
    };

    const pageNumbers = getPageNumbers();
    const startItem = (currentPage - 1) * pageSize + 1;
    const endItem = Math.min(currentPage * pageSize, totalItems);

    return (
        <div className="pagination">
            <div className="pagination-controls tool-bar">
                <button 
                    onClick={() => handlePageChange(1)} 
                    disabled={currentPage === 1}
                    title="First Page"
                    className="icon"
                >
                    <Icon name="first_page" />
                </button>
                <button 
                    onClick={() => handlePageChange(currentPage - 1)} 
                    disabled={currentPage === 1}
                    title="Previous Page"
                    className="icon"
                >
                    <Icon name="navigate_before" />
                </button>
                
                {pageNumbers.map(number => (
                    <button
                        key={number}
                        onClick={() => handlePageChange(number)}
                        className={currentPage === number ? 'active' : ''}
                    >
                        {number}
                    </button>
                ))}
                
                <button 
                    onClick={() => handlePageChange(currentPage + 1)} 
                    disabled={currentPage === totalPages}
                    title="Next Page"
                    className="icon"
                >
                    <Icon name="navigate_next" />
                </button>
                <button 
                    onClick={() => handlePageChange(totalPages)} 
                    disabled={currentPage === totalPages}
                    title="Last Page"
                    className="icon"
                >
                    <Icon name="last_page" />
                </button>
            </div>
            <div className="pagination-info">
                Showing {startItem}-{endItem} of {totalItems} items
            </div>
        </div>
    );
}
