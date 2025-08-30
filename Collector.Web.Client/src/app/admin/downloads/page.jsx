import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
//styles
import '@/styles/admin/filter.css';
import './page.css';
//components
import Container from '@/components/admin/container';
import Modal from '@/components/ui/modal';
import Icon from '@/components/ui/icon';
import Select from '@/components/forms/select';
import Input from '@/components/forms/input';
//components
import AddDownload from './components/add';
//context
import { useSession } from '@/context/session';
//api
import { Downloads } from '@/api/user/downloads';
//helpers
import { handleSort, getSortIcon } from '@/helpers/format';
import { localDateTime, printDate } from '@/helpers/datetime';
import messages from '@/helpers/messages';

/**
 * <summary>Admin Downloads List Page</summary>
 * <description>Displays and manages the list of downloads in the admin panel.</description>
 */
export default function AdminDownloads() {
    const navigate = useNavigate();
    const session = useSession();
    const { getCount, checkQueue, updateQueueItemStatus } = Downloads(session);
    
    const [downloads, setDownloads] = useState([]);
    const [showAdd, setShowAdd] = useState(false);
    const [domainName, setDomainName] = useState('');
    const [statusFilters, setStatusFilters] = useState(0);
    const [statusFiltersList, setStatusFiltersList] = useState([
        { id: 0, name: 'All Statuses' },
        { id: 1, name: 'Completed' },
        { id: 2, name: 'In Progress' },
        { id: 3, name: 'Failed' }
    ]);
    const [sort, setSort] = useState('StartDate DESC');
    const [cancelModal, setCancelModal] = useState(null);

    useEffect(() => {
        // Fetch downloads from API
        fetchDownloads();
    }, []);
    
    const fetchDownloads = () => {
        // Using proper casing for C# models (CamelCase)
        checkQueue({
            FeedId: 0,
            Domain: domainName || '',
            DomainDelay: 60,
            Sort: sort
        }).then(response => {
            if (response.data.success) {
                // Transform the data to match the expected format
                const formattedDownloads = (response.data.data || []).map(download => ({
                    id: download.id,
                    domain: download.domain,
                    startDate: download.dateCreated,
                    endDate: download.dateCompleted,
                    status: getStatusText(download.status),
                    articlesCount: download.articlesCount || 0,
                    errors: download.errors || 0
                }));
                setDownloads(formattedDownloads);
            }
        }).catch(error => {
            console.error('Error fetching downloads:', error);
        });
    };
    
    // Helper function to convert status code to text
    const getStatusText = (statusCode) => {
        switch(statusCode) {
            case 0: return 'Pending';
            case 1: return 'In Progress';
            case 2: return 'Completed';
            case 3: return 'Failed';
            case 4: return 'Cancelled';
            default: return 'Unknown';
        }
    };

    useEffect(() => {
        filterDownloads();
    }, [domainName, statusFilters, sort]);

    const filterDownloads = () => {
        // Just call fetchDownloads since it already handles filtering with proper casing
        fetchDownloads();
    };

    const handleCancel = (download) => {
        setCancelModal(download);
    };

    const handleCancelClose = () => {
        setCancelModal(null);
    };

    const handleCancelConfirmed = (downloadId) => {
        // Call API to cancel the download - status 4 is 'Cancelled'
        // Using proper casing for C# models (CamelCase)
        updateQueueItemStatus(downloadId, 4).then(response => {
            if (response.data.success) {
                // Refresh the download list
                fetchDownloads();
            } else {
                console.error('Failed to cancel download:', response.data.message);
            }
        }).catch(error => {
            console.error('Error cancelling download:', error);
        }).finally(() => {
            handleCancelClose();
        });
    };

    const CancelModal = () => {
        return (<>
            <Modal
                title="Cancel Download"
                onClose={handleCancelClose}
            >
                <p>
                    Do you really want to cancel the download for {cancelModal.domain}?
                    <br />
                    This will stop the download process immediately.
                </p>
                <div className="buttons">
                    <button className="submit" onClick={() => { handleCancelConfirmed(cancelModal.id) }}>Yes</button>
                    <button className="cancel" onClick={handleCancelClose}>Cancel</button>
                </div>
            </Modal>
        </>);
    };

    const handleClosedAddDownload = (download) => {
        if(download) {
            // Refresh the download list to include the newly added download
            fetchDownloads();
        }
        setShowAdd(false);
    };

    const tools = (<>
        <button onClick={() => setShowAdd(true)}><Icon name="add"></Icon>New Download</button>
    </>);

    return (
        <div className="admin-downloads">
            {showAdd && <AddDownload onClose={handleClosedAddDownload}></AddDownload>}
            {cancelModal != null && <CancelModal></CancelModal>}
            <Container
                title="Download Management"
                tools={tools}
            >
                <div className="filters">
                    <Input
                        name="domainsearch"
                        type="text"
                        placeholder="Search by Domain Name"
                        value={domainName}
                        onInput={(e) => setDomainName(e.target.value)}
                        className="domainNameInput"
                    />
                    <Select
                        options={statusFiltersList.map(status => ({ value: status.id, label: status.name }))}
                        value={statusFilters}
                        onChange={(e) => setStatusFilters(e.target.value)}
                    />
                </div>
                <table className="spreadsheet">
                    <thead>
                        <tr>
                            <th onClick={() => setSort(handleSort('Domain', sort))}>
                                Domain {getSortIcon('Domain', sort) && <span className="material-symbols-rounded">{getSortIcon('Domain', sort)}</span>}
                            </th>
                            <th onClick={() => setSort(handleSort('StartDate', sort))}>
                                Start Date {getSortIcon('StartDate', sort) && <span className="material-symbols-rounded">{getSortIcon('StartDate', sort)}</span>}
                            </th>
                            <th onClick={() => setSort(handleSort('EndDate', sort))}>
                                End Date {getSortIcon('EndDate', sort) && <span className="material-symbols-rounded">{getSortIcon('EndDate', sort)}</span>}
                            </th>
                            <th onClick={() => setSort(handleSort('Status', sort))}>
                                Status {getSortIcon('Status', sort) && <span className="material-symbols-rounded">{getSortIcon('Status', sort)}</span>}
                            </th>
                            <th onClick={() => setSort(handleSort('ArticlesCount', sort))}>
                                Articles {getSortIcon('ArticlesCount', sort) && <span className="material-symbols-rounded">{getSortIcon('ArticlesCount', sort)}</span>}
                            </th>
                            <th onClick={() => setSort(handleSort('Errors', sort))}>
                                Errors {getSortIcon('Errors', sort) && <span className="material-symbols-rounded">{getSortIcon('Errors', sort)}</span>}
                            </th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                        {downloads.map(download =>
                            <tr 
                                key={download.id} 
                                onClick={(e) => {
                                    // Prevent triggering if the event originated from action buttons
                                    if (e.target.closest('a')) {
                                        e.stopPropagation();
                                        return;
                                    }
                                    navigate('/admin/downloads/details/' + download.id);
                                }}
                            >
                                <td>{download.domain}</td>
                                <td>{download.startDate ? printDate(localDateTime(new Date(download.startDate))) : 'N/A'}</td>
                                <td>{download.endDate ? printDate(localDateTime(new Date(download.endDate))) : 'In Progress'}</td>
                                <td>{download.status}</td>
                                <td>{download.articlesCount}</td>
                                <td>{download.errors}</td>
                                <td className="buttons">
                                    <Link to={'/admin/downloads/details/' + download.id} title="view download details"><Icon name="visibility"></Icon></Link>
                                    {download.status === 'In Progress' && (
                                        <Link 
                                            onClick={(e) => { 
                                                e.preventDefault(); 
                                                handleCancel(download); 
                                            }} 
                                            title="cancel download"
                                        >
                                            <Icon name="cancel"></Icon>
                                        </Link>
                                    )}
                                </td>
                            </tr>
                        )}
                    </tbody>
                </table>
            </Container>
        </div>
    );
}
