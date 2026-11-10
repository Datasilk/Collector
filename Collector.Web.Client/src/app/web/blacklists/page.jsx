import React, { useState, useEffect } from 'react';
//styles
import '@/styles/admin/filter.css';
import './page.css';
//components
import Container from '@/components/admin/container';
import Modal from '@/components/ui/modal';
import Icon from '@/components/ui/icon';
import Input from '@/components/forms/input';
import Pager from '@/components/ui/pager';
//context
import { useSession } from '@/context/session';
//api
import { Blacklists } from '@/api/user/blacklists';
//helpers
import { handleSort, getSortIcon } from '@/helpers/format';
import messages from '@/helpers/messages';

/**
 * <summary>Admin Blacklists Page</summary>
 * <description>Displays and manages the list of blacklists in the admin panel.</description>
 */
export default function AdminBlacklists() {
    const session = useSession();
    const { getDomainsList, getWildcardsList, removeDomain, removeWildcard } = Blacklists(session);

    const [allBlacklists, setAllBlacklists] = useState([]);
    const [filteredBlacklists, setFilteredBlacklists] = useState([]);
    const [displayedBlacklists, setDisplayedBlacklists] = useState([]);
    const [showAdd, setShowAdd] = useState(false);
    const [searchName, setSearchName] = useState('');
    const [sort, setSort] = useState('Name ASC');
    const [deleteModal, setDeleteModal] = useState(null);
    const [currentPage, setCurrentPage] = useState(1);
    const pageSize = 100;

    useEffect(() => {
        // Fetch blacklists from API when component mounts
        fetchBlacklists();
    }, []);
    
    const fetchBlacklists = () => {
        // Get domains list
        getDomainsList().then(response => {
            if (response.data.success) {
                const domains = response.data.data || [];
                const formattedDomains = domains.map((domain, index) => ({
                    id: 'domain-' + index,
                    name: domain,
                    type: 'domain'
                }));

                // Get wildcards list
                getWildcardsList().then(wildcardResponse => {
                    if (wildcardResponse.data.success) {
                        const wildcardDomains = wildcardResponse.data.data || [];
                        const formattedWildcards = wildcardDomains.map((domain, index) => ({
                            id: 'wildcard-' + index,
                            name: domain,
                            type: 'wildcard'
                        }));

                        // Combine domains and wildcards
                        setAllBlacklists([...formattedDomains, ...formattedWildcards]);
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
    }, [searchName, sort, allBlacklists]);

    useEffect(() => {
        paginateBlacklists();
    }, [filteredBlacklists, currentPage]);

    const filterBlacklists = () => {
        let filtered = [...allBlacklists];

        if (searchName) {
            filtered = filtered.filter(blacklist =>
                blacklist.name.toLowerCase().includes(searchName.toLowerCase())
            );
        }

        filtered.sort((a, b) => {
            const direction = sort.toLowerCase().endsWith(' desc') ? -1 : 1;
            return a.name.localeCompare(b.name) * direction;
        });

        setFilteredBlacklists(filtered);
        setCurrentPage(1);
    };

    const paginateBlacklists = () => {
        const start = (currentPage - 1) * pageSize;
        const end = start + pageSize;
        setDisplayedBlacklists(filteredBlacklists.slice(start, end));
    };

    const handlePageChange = (page) => {
        setCurrentPage(page);
    };

    const totalPages = Math.ceil(filteredBlacklists.length / pageSize) || 1;

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
                setAllBlacklists(allBlacklists.filter(item => item.id !== blacklist.id));
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
                </div>
                <table className="spreadsheet">
                    <thead>
                        <tr>
                            <th onClick={() => setSort(handleSort('Name', sort))}>
                                Name {getSortIcon('Name', sort) && <span className="material-symbols-rounded">{getSortIcon('Name', sort)}</span>}
                            </th>
                            <th onClick={() => setSort(handleSort('Type', sort))}>
                                Type {getSortIcon('Type', sort) && <span className="material-symbols-rounded">{getSortIcon('Type', sort)}</span>}
                            </th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                        {displayedBlacklists.map(blacklist =>
                            <tr
                                key={blacklist.id}
                            >
                                <td>{blacklist.name}</td>
                                <td>{blacklist.type === 'domain' ? 'Domain' : 'Wildcard'}</td>
                                <td className="buttons">
                                    <button className="icon" onClick={() => handleDelete(blacklist)} title="delete blacklist">
                                        <Icon name="delete"></Icon>
                                    </button>
                                </td>
                            </tr>
                        )}
                    </tbody>
                </table>
                <Pager
                    currentPage={currentPage}
                    totalPages={totalPages}
                    onPageChange={handlePageChange}
                />
            </Container>
        </div>
    );
}
