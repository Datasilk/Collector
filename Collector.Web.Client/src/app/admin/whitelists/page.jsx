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
import { Whitelists } from '@/api/admin/whitelists';
//helpers
import { handleSort, getSortIcon } from '@/helpers/format';
import { localDateTime, printDate } from '@/helpers/datetime';
import messages from '@/helpers/messages';

/**
 * <summary>Admin Whitelists Page</summary>
 * <description>Displays and manages the list of whitelists in the admin panel.</description>
 */
export default function AdminWhitelists() {
    const navigate = useNavigate();
    const session = useSession();
    const { getWhitelists, deleteWhitelist } = Whitelists(session);
    
    const [whitelists, setWhitelists] = useState([]);
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
        // Fetch whitelists from API on initial load
        fetchWhitelists();
    }, []);

    useEffect(() => {
        filterWhitelists();
    }, [searchName, statusFilters, sort]);

    const fetchWhitelists = () => {
        getWhitelists({
            search: searchName,
            status: statusFilters,
            sort: sort
        }).then(response => {
            if (response.data.success) {
                setWhitelists(response.data.data);
            } else {
                messages.error(response.data.message || 'Failed to fetch whitelists');
                console.error('Failed to fetch whitelists:', response.data.message);
            }
        }).catch(error => {
            messages.error('Failed to fetch whitelists');
            console.error('Error fetching whitelists:', error);
        });
    };
    
    const filterWhitelists = () => {
        // Call API with filters
        fetchWhitelists();
    };

    const handleDelete = (whitelist) => {
        setDeleteModal(whitelist);
    };

    const handleDeleteClose = () => {
        setDeleteModal(null);
    };

    const handleDeleteConfirmed = (whitelistId) => {
        deleteWhitelist(whitelistId)
            .then(response => {
                if (response.data.success) {
                    messages.success('Whitelist deleted successfully');
                    fetchWhitelists(); // Refresh the list
                } else {
                    messages.error(response.data.message || 'Failed to delete whitelist');
                    console.error('Failed to delete whitelist:', response.data.message);
                }
            })
            .catch(error => {
                messages.error('Failed to delete whitelist');
                console.error('Error deleting whitelist:', error);
            })
            .finally(() => {
                handleDeleteClose();
            });
    };

    const DeleteModal = () => {
        return (<>
            <Modal
                title="Delete Whitelist"
                onClose={handleDeleteClose}
            >
                <p>
                    Do you really want to delete the whitelist "{deleteModal.name}"?
                    <br />
                    This will remove {deleteModal.domainCount} domains from the whitelist.
                </p>
                <div className="buttons">
                    <button className="submit" onClick={() => { handleDeleteConfirmed(deleteModal.id) }}>Yes</button>
                    <button className="cancel" onClick={handleDeleteClose}>Cancel</button>
                </div>
            </Modal>
        </>);
    };

    const handleClosedAddWhitelist = (whitelist) => {
        if(whitelist) {
            // Refresh the whitelist list from API
            fetchWhitelists();
        }
        setShowAdd(false);
    };

    const tools = (<>
        <button onClick={() => setShowAdd(true)}><Icon name="add"></Icon>New Whitelist</button>
    </>);

    return (
        <div className="admin-whitelists">
            {showAdd && <div className="modal-placeholder">Add Whitelist Modal would appear here</div>}
            {deleteModal != null && <DeleteModal></DeleteModal>}
            <Container
                title="Whitelist Management"
                tools={tools}
            >
                <div className="filters">
                    <Input
                        name="whitelistsearch"
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
                        {whitelists.map(whitelist =>
                            <tr 
                                key={whitelist.id} 
                                onClick={(e) => {
                                    // Prevent triggering if the event originated from action buttons
                                    if (e.target.closest('a')) {
                                        e.stopPropagation();
                                        return;
                                    }
                                    navigate('/admin/whitelists/edit/' + whitelist.id);
                                }}
                            >
                                <td>{whitelist.name}</td>
                                <td>{whitelist.domainCount}</td>
                                <td>{whitelist.created ? printDate(localDateTime(new Date(whitelist.created))) : 'N/A'}</td>
                                <td>{whitelist.lastUpdated ? printDate(localDateTime(new Date(whitelist.lastUpdated))) : 'N/A'}</td>
                                <td>{whitelist.status}</td>
                                <td className="buttons">
                                    <Link to={'/admin/whitelists/edit/' + whitelist.id} title="edit whitelist"><Icon name="edit_square"></Icon></Link>
                                    <Link 
                                        onClick={(e) => { 
                                            e.preventDefault(); 
                                            handleDelete(whitelist); 
                                        }} 
                                        title="delete whitelist"
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
