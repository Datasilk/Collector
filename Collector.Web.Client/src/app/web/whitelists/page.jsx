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
import { Whitelists } from '@/api/admin/whitelists';
//helpers
import { handleSort, getSortIcon } from '@/helpers/format';
import messages from '@/helpers/messages';

/**
 * <summary>Admin Whitelists Page</summary>
 * <description>Displays and manages the list of whitelists in the admin panel.</description>
 */
export default function AdminWhitelists() {
    const session = useSession();
    const { getWhitelists, createWhitelist, deleteWhitelist } = Whitelists(session);

    const [allWhitelists, setAllWhitelists] = useState([]);
    const [displayedWhitelists, setDisplayedWhitelists] = useState([]);
    const [showAdd, setShowAdd] = useState(false);
    const [newDomain, setNewDomain] = useState('');
    const [searchName, setSearchName] = useState('');
    const [sort, setSort] = useState('Name ASC');
    const [deleteModal, setDeleteModal] = useState(null);
    const [currentPage, setCurrentPage] = useState(1);
    const [totalCount, setTotalCount] = useState(0);
    const pageSize = 100;

    useEffect(() => {
        fetchWhitelists();
    }, [searchName, sort, currentPage]);

    const fetchWhitelists = () => {
        getWhitelists({
            search: searchName,
            sort: sort,
            start: (currentPage - 1) * pageSize + 1,
            length: pageSize
        }).then(response => {
            if (response.data.success) {
                setAllWhitelists(response.data.data.items || []);
                setTotalCount(response.data.data.totalCount || 0);
                setDisplayedWhitelists(response.data.data.items || []);
            } else {
                messages.error(response.data.message || 'Failed to fetch whitelists');
                console.error('Failed to fetch whitelists:', response.data.message);
            }
        }).catch(error => {
            messages.error('Failed to fetch whitelists');
            console.error('Error fetching whitelists:', error);
        });
    };

    const handlePageChange = (page) => {
        setCurrentPage(page);
    };

    const totalPages = Math.ceil(totalCount / pageSize) || 1;

    const handleDelete = (whitelist) => {
        setDeleteModal(whitelist);
    };

    const handleDeleteClose = () => {
        setDeleteModal(null);
    };

    const handleDeleteConfirmed = (domain) => {
        deleteWhitelist(domain)
            .then(response => {
                if (response.data.success) {
                    messages.success('Whitelist domain removed successfully');
                    fetchWhitelists();
                } else {
                    messages.error(response.data.message || 'Failed to remove whitelist domain');
                    console.error('Failed to remove whitelist domain:', response.data.message);
                }
            })
            .catch(error => {
                messages.error('Failed to remove whitelist domain');
                console.error('Error removing whitelist domain:', error);
            })
            .finally(() => {
                handleDeleteClose();
            });
    };

    const handleAddDomain = () => {
        if (!newDomain || newDomain.trim() === '') {
            messages.error('Domain is required');
            return;
        }
        createWhitelist(newDomain.trim())
            .then(response => {
                if (response.data.success) {
                    messages.success('Domain added to whitelist');
                    setNewDomain('');
                    setShowAdd(false);
                    fetchWhitelists();
                } else {
                    messages.error(response.data.message || 'Failed to add domain');
                }
            })
            .catch(error => {
                console.error('Error adding whitelist domain:', error);
                messages.error('Failed to add domain to whitelist');
            });
    };

    const DeleteModal = () => {
        return (<>
            <Modal
                title="Remove Whitelisted Domain"
                onClose={handleDeleteClose}
            >
                <p>
                    Do you really want to remove the domain "{deleteModal.name}" from the whitelist?
                </p>
                <div className="buttons">
                    <button className="submit" onClick={() => { handleDeleteConfirmed(deleteModal.name) }}>Yes</button>
                    <button className="cancel" onClick={handleDeleteClose}>Cancel</button>
                </div>
            </Modal>
        </>);
    };

    const AddModal = () => {
        return (<>
            <Modal
                title="Add Domain to Whitelist"
                onClose={() => setShowAdd(false)}
            >
                <div className="form-sized">
                    <div className="form-row">
                        <Input
                            label="Domain"
                            name="domain"
                            type="text"
                            value={newDomain}
                            onInput={(e) => setNewDomain(e.target.value)}
                            placeholder="Enter domain (e.g. example.com)"
                            required={true}
                        />
                    </div>
                    <div className="buttons">
                        <button className="submit" onClick={handleAddDomain}><Icon name="add"></Icon>Add Domain</button>
                        <button className="cancel" onClick={() => setShowAdd(false)}>Cancel</button>
                    </div>
                </div>
            </Modal>
        </>);
    };

    const tools = (<>
        <button onClick={() => setShowAdd(true)}><Icon name="add"></Icon>New Domain</button>
    </>);

    return (
        <div className="admin-whitelists">
            {showAdd && <AddModal></AddModal>}
            {deleteModal != null && <DeleteModal></DeleteModal>}
            <Container
                title="Whitelist Management"
                tools={tools}
            >
                <div className="filters">
                    <Input
                        name="whitelistsearch"
                        type="text"
                        placeholder="Search by Domain"
                        value={searchName}
                        onInput={(e) => setSearchName(e.target.value)}
                        className="nameInput"
                    />
                </div>
                <table className="spreadsheet">
                    <thead>
                        <tr>
                            <th onClick={() => setSort(handleSort('Name', sort))}>
                                Domain {getSortIcon('Name', sort) && <span className="material-symbols-rounded">{getSortIcon('Name', sort)}</span>}
                            </th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                        {displayedWhitelists.map(whitelist =>
                            <tr
                                key={whitelist.name}
                            >
                                <td>{whitelist.name}</td>
                                <td className="buttons">
                                    <button className="icon" onClick={() => handleDelete(whitelist)} title="remove domain">
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
