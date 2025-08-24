import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import Icon from '@/components/ui/icon';
import { useSession } from '@/context/session';
import { Journals } from '@/api/user/journals';
import './page.css';
import '@/styles/forms.css';

/**
 * <summary>Journal Details Page</summary>
 * <description>Displays detailed information about a specific journal and its entries</description>
 */
export default function JournalDetailsPage() {
    //context
    const { journalId } = useParams();
    const navigate = useNavigate();
    const session = useSession();

    //state
    const [journal, setJournal] = useState(null);
    const [entries, setEntries] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [sort, setSort] = useState('Title_asc');

    //effect
    useEffect(() => {
        fetchJournalDetails();
    }, [journalId, navigate, session]);

    //actions
    const fetchJournalDetails = () => {
        setLoading(true);
        
        // Use the journals API to fetch journal details
        const api = Journals(session);
        
        // Get journal details
        api.getJournal(journalId)
            .then(response => {
                if (response.data.success) {
                    setJournal(response.data.data);
                }
                
                // After getting journal, get entries
                return api.getEntries(journalId);
            })
            .then(response => {
                if (response.data.success) {
                    setEntries(response.data.data);
                }
                setLoading(false);
            })
            .catch(err => {
                console.error('Error fetching journal details:', err);
                setError('Failed to load journal details. Please try again later.');
                setLoading(false);
            });
    };

    const handleNewEntry = () => {
        navigate(`/journal/${journalId}/entry/new`);
    };

    const handleEditJournal = () => {
        navigate(`/journal/${journalId}/edit`);
    };

    const handleViewEntry = (entryId) => {
        navigate(`/journal/${journalId}/entry/${entryId}`);
    };

    // Render loading state
    if (loading) {
        return (
            <div className="journal-details-page loading">
                <div className="loading-spinner">
                    <Icon name="loading" />
                    <p>Loading journal details...</p>
                </div>
            </div>
        );
    }

    // Render error state
    if (error) {
        return (
            <div className="journal-details-page error">
                <div className="error-message">
                    <Icon name="error" />
                    <p>{error}</p>
                    <button onClick={() => window.location.reload()}>Retry</button>
                </div>
            </div>
        );
    }

    // Render select journal message or journal not found
    if (!journal) {
        if (!journalId) {
            return (
                <div className="journal-details-page select-journal">
                    <div className="select-journal-message">
                        <Icon name="menu_book" />
                        <h2>Select a Journal</h2>
                        <p>Please select a journal from the sidebar to view its contents.</p>
                        <p className="hint">You can create a new journal by clicking the + button in the sidebar.</p>
                    </div>
                </div>
            );
        } else {
            return (
                <div className="journal-details-page not-found">
                    <div className="not-found-message">
                        <Icon name="warning" />
                        <h2>Journal Not Found</h2>
                        <p>The journal you're looking for doesn't exist or you don't have permission to view it.</p>
                        <button onClick={() => navigate('/journal')}>Back to Journals</button>
                    </div>
                </div>
            );
        }
    }

    // Format date for display
    const formatDate = (dateString) => {
        if (!dateString) return 'N/A';
        const date = new Date(dateString);
        return date.toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'long',
            day: 'numeric'
        });
    };
    
    // Handle sorting
    const handleSort = (field, currentSort) => {
        const [currentField, currentDirection] = currentSort.split('_');
        const direction = currentField === field && currentDirection === 'asc' ? 'desc' : 'asc';
        return `${field}_${direction}`;
    };
    
    // Get sort icon
    const getSortIcon = (field, currentSort) => {
        const [currentField, currentDirection] = currentSort.split('_');
        if (currentField !== field) return null;
        return currentDirection === 'asc' ? 'arrow_upward' : 'arrow_downward';
    };
    
    // Get status text
    const getStatusText = (status) => {
        switch(status) {
            case 0: return 'Draft';
            case 1: return 'Published';
            case 2: return 'Archived';
            default: return 'Unknown';
        }
    };

    // Sort entries based on current sort setting
    const sortedEntries = [...entries].sort((a, b) => {
        const [field, direction] = sort.split('_');
        const multiplier = direction === 'asc' ? 1 : -1;
        
        switch(field) {
            case 'Title':
                return multiplier * a.title.localeCompare(b.title);
            case 'Created':
                return multiplier * (new Date(a.created) - new Date(b.created));
            case 'Modified':
                return multiplier * (new Date(a.modified) - new Date(b.modified));
            case 'Status':
                return multiplier * (a.status - b.status);
            default:
                return 0;
        }
    });
    
    // Main render
    return (
        <div className="journal-details-page">
            <div className="tool-bar">
                <div className="title">{journal.title}</div>
                <div className="right-side">
                    <button onClick={handleNewEntry}>
                        <Icon name="add" /> New Entry
                    </button>
                    <button onClick={handleEditJournal}>
                        <Icon name="edit" /> Edit Journal
                    </button>
                </div>
            </div>

            <div className="journal-metadata">
                <div className="created-date">
                    <Icon name="calendar_today" />
                    <span>Created: {formatDate(journal.created)}</span>
                </div>
                <div className="status">
                    <Icon name="info" />
                    <span>Status: {journal.status == 1 ? 'Active' : 'Archived'}</span>
                </div>
            </div>
            
            <div className="journal-entries">
                {entries.length === 0 ? (
                    <div className="empty-state">
                        <p>No entries yet. Create your first entry to get started!</p>
                        <button onClick={handleNewEntry}>
                            <Icon name="add" /> New Entry
                        </button>
                    </div>
                ) : (
                    <div className="entries-table">
                        <table className="spreadsheet">
                            <thead>
                                <tr>
                                    <th onClick={() => setSort(handleSort('Title', sort))}>
                                        Title {getSortIcon('Title', sort) && <Icon name={getSortIcon('Title', sort)} />}
                                    </th>
                                    <th onClick={() => setSort(handleSort('Created', sort))}>
                                        Created {getSortIcon('Created', sort) && <Icon name={getSortIcon('Created', sort)} />}
                                    </th>
                                    <th onClick={() => setSort(handleSort('Modified', sort))}>
                                        Modified {getSortIcon('Modified', sort) && <Icon name={getSortIcon('Modified', sort)} />}
                                    </th>
                                    <th onClick={() => setSort(handleSort('Status', sort))}>
                                        Status {getSortIcon('Status', sort) && <Icon name={getSortIcon('Status', sort)} />}
                                    </th>
                                    <th></th>
                                </tr>
                            </thead>
                            <tbody>
                                {sortedEntries.map(entry => (
                                    <tr 
                                        key={entry.id}
                                        onClick={() => handleViewEntry(entry.id)}
                                    >
                                        <td>{entry.title}</td>
                                        <td>{formatDate(entry.created)}</td>
                                        <td>{formatDate(entry.modified)}</td>
                                        <td>
                                            <span className={`entry-status status-${entry.status}`}>
                                                {getStatusText(entry.status)}
                                            </span>
                                        </td>
                                        <td className="tool-bar align-right">
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
                )}
            </div>
        </div>
    );
}
