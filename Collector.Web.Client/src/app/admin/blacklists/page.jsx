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
//context
import { useSession } from '@/context/session';
//api
import { Blacklists } from '@/api/user/blacklists';
//helpers
import { handleSort, getSortIcon } from '@/helpers/format';
import { localDateTime, printDate } from '@/helpers/datetime';
import messages from '@/helpers/messages';

/**
 * <summary>Admin Blacklists Page</summary>
 * <description>Displays and manages the list of blacklists in the admin panel.</description>
 */
export default function AdminBlacklists() {
    const navigate = useNavigate();
    const session = useSession();
    const { getDomainsList, getWildcardsList, removeDomain, removeWildcard } = Blacklists(session);
    
    const [blacklists, setBlacklists] = useState([]);
    const [wildcards, setWildcards] = useState([]);
    const [showAdd, setShowAdd] = useState(false);
    const [searchName, setSearchName] = useState('');
    const [statusFilters, setStatusFilters] = useState(0);
    const [statusFiltersList, setStatusFiltersList] = useState([
        { id: 0, name: 'All Statuses' },
        { id: 1, name: 'Active' },
        { id: 2, name: 'Inactive' }
    ]);
    const [sort, setSort] = useState('Name ASC');
    const [deleteModal, setDeleteModal] = useState(null);

    useEffect(() => {
        // Fetch blacklists from API when component mounts
        fetchBlacklists();
    }, []);
    
    const fetchBlacklists = () => {
        // Get domains list
        getDomainsList().then(response => {
            if (response.data.success) {
                const domains = response.data.data || [];
                // Transform domains into a format suitable for display
                const formattedDomains = domains.map((domain, index) => ({
                    id: index + 1,
                    name: domain,
                    domainCount: 1,
                    created: new Date().toISOString(), // API doesn't provide this info
                    lastUpdated: new Date().toISOString(), // API doesn't provide this info
                    status: 'Active',
                    type: 'domain'
                }));
                
                // Get wildcards list
                getWildcardsList().then(wildcardResponse => {
                    if (wildcardResponse.data.success) {
                        const wildcardDomains = wildcardResponse.data.data || [];
                        // Transform wildcards into a format suitable for display
                        const formattedWildcards = wildcardDomains.map((domain, index) => ({
                            id: domains.length + index + 1,
                            name: domain,
                            domainCount: 1, // Each wildcard counts as one entry
                            created: new Date().toISOString(),
                            lastUpdated: new Date().toISOString(),
                            status: 'Active',
                            type: 'wildcard'
                        }));
                        
                        // Combine domains and wildcards
                        setBlacklists([...formattedDomains, ...formattedWildcards]);
                    }
                }).catch(error => {
                    console.error('Error fetching wildcards:', error);
                });
            }
        }).catch(error => {
            console.error('Error fetching domains:', error);
        });
    };

    useEffect(() => {
        filterBlacklists();
    }, [searchName, statusFilters, sort]);

    const filterBlacklists = () => {
        // Since we're working with local data after fetching from API,
        // we'll filter the data client-side
        let filtered = [...blacklists];
        
        if (searchName) {
            filtered = filtered.filter(blacklist => 
                blacklist.name.toLowerCase().includes(searchName.toLowerCase())
            );
        }
        
        if (statusFilters !== 0) {
            const status = statusFiltersList.find(s => s.id === statusFilters)?.name;
            filtered = filtered.filter(blacklist => blacklist.status === status);
        }
        
        // Sort logic
        const [field, direction] = sort.split(' ');
        filtered.sort((a, b) => {
            let comparison = 0;
            if (a[field.toLowerCase()] < b[field.toLowerCase()]) comparison = -1;
            if (a[field.toLowerCase()] > b[field.toLowerCase()]) comparison = 1;
            return direction === 'ASC' ? comparison : -comparison;
        });
        
        setBlacklists(filtered);
    };

    const handleDelete = (blacklist) => {
        setDeleteModal(blacklist);
    };

    const handleDeleteClose = () => {
        setDeleteModal(null);
    };

    const handleDeleteConfirmed = (blacklist) => {
        // Call the appropriate API based on the blacklist type
        const deletePromise = blacklist.type === 'domain' ?
            removeDomain(blacklist.name) :
            removeWildcard(blacklist.name);
            
        deletePromise.then(response => {
            if (response.data.success) {
                setBlacklists(blacklists.filter(item => item.id !== blacklist.id));
                messages.success(`${blacklist.type === 'domain' ? 'Domain' : 'Wildcard'} removed successfully`);
            } else {
                messages.error(`Failed to remove ${blacklist.type}`);
            }
            handleDeleteClose();
        }).catch(error => {
            console.error(`Error removing ${blacklist.type}:`, error);
            messages.error(`An error occurred while removing the ${blacklist.type}`);
            handleDeleteClose();
        });
    };

    const DeleteModal = () => {
        return (<>
            <Modal
                title={`Delete ${deleteModal.type === 'domain' ? 'Domain' : 'Wildcard'}`}
                onClose={handleDeleteClose}
            >
                <p>
                    Do you really want to delete the {deleteModal.type} "{deleteModal.name}"?
                    <br />
                    This will remove it from the blacklist.
                </p>
                <div className="buttons">
                    <button className="submit" onClick={() => { handleDeleteConfirmed(deleteModal) }}>Yes</button>
                    <button className="cancel" onClick={handleDeleteClose}>Cancel</button>
                </div>
            </Modal>
        </>);
    };

    const handleClosedAddBlacklist = (blacklist) => {
        if(blacklist) {
            // Refresh the blacklist list to include the newly added domain/wildcard
            fetchBlacklists();
        }
        setShowAdd(false);
    };

    const tools = (<>
        <button onClick={() => setShowAdd(true)}><Icon name="add"></Icon>New Blacklist</button>
    </>);

    return (
        <div className="admin-blacklists">
            {showAdd && <div className="modal-placeholder">Add Blacklist Modal would appear here</div>}
            {deleteModal != null && <DeleteModal></DeleteModal>}
            <Container
                title="Blacklist Management"
                tools={tools}
            >
                <div className="filters">
                    <Input
                        name="blacklistsearch"
                        type="text"
                        placeholder="Search by Name"
                        value={searchName}
                        onInput={(e) => setSearchName(e.target.value)}
                        className="nameInput"
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
                            <th onClick={() => setSort(handleSort('Name', sort))}>
                                Name {getSortIcon('Name', sort) && <span className="material-symbols-rounded">{getSortIcon('Name', sort)}</span>}
                            </th>
                            <th onClick={() => setSort(handleSort('DomainCount', sort))}>
                                Domains {getSortIcon('DomainCount', sort) && <span className="material-symbols-rounded">{getSortIcon('DomainCount', sort)}</span>}
                            </th>
                            <th onClick={() => setSort(handleSort('Created', sort))}>
                                Created {getSortIcon('Created', sort) && <span className="material-symbols-rounded">{getSortIcon('Created', sort)}</span>}
                            </th>
                            <th onClick={() => setSort(handleSort('LastUpdated', sort))}>
                                Last Updated {getSortIcon('LastUpdated', sort) && <span className="material-symbols-rounded">{getSortIcon('LastUpdated', sort)}</span>}
                            </th>
                            <th onClick={() => setSort(handleSort('Status', sort))}>
                                Status {getSortIcon('Status', sort) && <span className="material-symbols-rounded">{getSortIcon('Status', sort)}</span>}
                            </th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                        {blacklists.map(blacklist =>
                            <tr 
                                key={blacklist.id} 
                                onClick={(e) => {
                                    // Prevent triggering if the event originated from action buttons
                                    if (e.target.closest('a')) {
                                        e.stopPropagation();
                                        return;
                                    }
                                    navigate('/admin/blacklists/edit/' + blacklist.id);
                                }}
                            >
                                <td>{blacklist.name}</td>
                                <td>{blacklist.domainCount}</td>
                                <td>{blacklist.created ? printDate(localDateTime(new Date(blacklist.created))) : 'N/A'}</td>
                                <td>{blacklist.lastUpdated ? printDate(localDateTime(new Date(blacklist.lastUpdated))) : 'N/A'}</td>
                                <td>{blacklist.status}</td>
                                <td className="buttons">
                                    <Link to={'/admin/blacklists/edit/' + blacklist.id} title="edit blacklist"><Icon name="edit_square"></Icon></Link>
                                    <Link 
                                        onClick={(e) => { 
                                            e.preventDefault(); 
                                            handleDelete(blacklist); 
                                        }} 
                                        title="delete blacklist"
                                    >
                                        <Icon name="delete"></Icon>
                                    </Link>
                                </td>
                            </tr>
                        )}
                    </tbody>
                </table>
            </Container>
        </div>
    );
}
