export const formatCurrency = (value) => {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value);
};

export const formatNumber = (value) => {
    return new Intl.NumberFormat('en-US').format(value);
};

export const formatDateTime = (value) => {
    return new Intl.DateTimeFormat('en-US', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
    }).format(new Date(value));
};

export const formatDate = (value) => {
    return new Intl.DateTimeFormat('en-US', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
    }).format(new Date(value));
};

/**
 * Formats a number as a percentage string with one decimal place.
 * Example: 0.1234 => "12.3%"
 * @param {number} value
 * @returns {string}
 */
export function formatPercent(value) {
    if (typeof value !== 'number' || isNaN(value)) return '';
    return `${(value * 100).toFixed(1)}%`;
}

export const handleSort = (column, currentSort) => {
    const [prevColumn, prevDirection] = currentSort.split(' ');
    const newDirection = prevColumn === column && prevDirection === 'ASC' ? 'DESC' : 'ASC';
    return `${column} ${newDirection}`;
};

export const getSortIcon = (column, currentSort) => {
    const [currentColumn, currentDirection] = currentSort.split(' ');
    if (currentColumn === column) {
        return currentDirection === 'ASC' ? 'arrow_drop_up' : 'arrow_drop_down';
    }
    return null;
};
