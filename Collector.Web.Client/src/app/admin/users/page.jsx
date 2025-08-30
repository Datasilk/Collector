import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
//styles
import '@/styles/admin/filter.css';
import './page.css';
//components
import Container from '@/components/admin/container';
import Modal from '@/components/ui/modal';
import Icon from '@/components/ui/icon';
import AddUser from './components/add';
import Select from '@/components/forms/select';
import Input from '@/components/forms/input';
import Pagination from '@/components/ui/pagination';
//api
import { useSession } from '@/context/session';
import { Users } from '@/api/admin/users';
//helpers
import { localDateTime, printDate } from '@/helpers/datetime';
import { handleSort, getSortIcon } from '@/helpers/format';
import messages from '@/helpers/messages';

/**
 * <summary>Admin Users List Page</summary>
 * <description>Displays and manages the list of users in the admin panel.</description>
 */
export default function AdminUsers() {
    const navigate = useNavigate();
    const session = useSession();
    const { getUsersFiltered, getRoles, lockUser } = Users(session);

    const [users, setUsers] = useState([]);
    const [lock, setLock] = useState(null);
    const [showAdd, setShowAdd] = useState(false);
    const [filter, setFilter] = useState({
        fullName: '',
        role: 0,
        sort: 'Email ASC',
        start: 0,
        length: 10
    });
    const [roleFiltersList, setRoleFiltersList] = useState([]);
    const [totalItems, setTotalItems] = useState(0);
    const [totalPages, setTotalPages] = useState(1);

    //execute api
    useEffect(() => {
        if (users.length === 0) {
            filterUsers(); // Ensure this is only called once during initial load
        }
        fetchRoles();
    }, []);
        
    useEffect(() => {
        // Reset to first page when filter criteria change
        if (filter.fullName !== undefined || filter.role !== undefined || filter.sort !== undefined) {
            setFilter(prev => ({ ...prev, start: 0 }));
            filterUsers();
        }
    }, [filter.fullName, filter.role, filter.sort]);
    
    useEffect(() => {
        if (filter.start !== undefined || filter.length !== undefined) {
            filterUsers();
        }
    }, [filter.start, filter.length]);

    const fetchRoles = () => {
        getRoles().then(response => {
            if (response.data.success) {
                setRoleFiltersList([{ id: 0, name: 'Any Role' }, ...response.data.data]); // Add 'Any' option
            } else {
                console.error('Failed to fetch roles:', response.data.message);
                messages.error(response.data.message || 'Failed to fetch roles');
            }
        }).catch(error => {
            console.error('Error fetching roles:', error);
            messages.error('Failed to fetch roles');
        });
    };

    const filterUsers = () => {
        getUsersFiltered(filter).then(response => {
            if (response.data.success) {
                setUsers(response.data.data.items || []);
                setTotalItems(response.data.data.totalCount || 0);
                setTotalPages(Math.ceil((response.data.data.totalCount || 0) / filter.length));
            } else {
                messages.error(response.data.message || 'Failed to fetch users');
            }
        }).catch(error => {
            console.error('Error fetching users:', error);
            messages.error('Failed to fetch users');
        });
    };
    
    const handlePageChange = (page) => {
        const start = (page - 1) * filter.length;
        setFilter(prev => ({ ...prev, start }));
    };

    const handleLock = (person) => {
        setLock(person);
    }

    const LockModal = () => {
        return (<>
            <Modal
                title="Disable User Account"
                onClose={handleLockClose}
            >
                <p>
                    Do you really want to lock this user account for {lock.fullName}?
                    <br />
                    They will no longer be able to log into their account.
                </p>
                <div className="buttons">
                    <button className="submit" onClick={() => { handleLockConfirmed(lock.id) }}>Yes</button>
                    <button className="cancel" onClick={handleLockClose}>Cancel</button>
                </div>
            </Modal>
        </>);
    }

    const handleLockConfirmed = (userId) => {
        lockUser(userId, true)
            .then(response => {
                if (response.data.success) {
                    // refresh the user list
                    filterUsers();
                    messages.success('User account locked successfully');
                } else {
                    // Handle failure case
                    messages.error(response.data.message || 'Failed to lock user');
                    console.error('Failed to lock user:', response.data.message);
                }
            })
            .catch(error => {
                // Handle error case
                console.error('Error locking user:', error);
                messages.error('Failed to lock user');
            })
            .finally(() => {
                handleLockClose();
            });
    };

    const handleLockClose = () => {
        setLock(null);
    }

    const handleClosedAddUser = (user) => {
        if(user) {
            //new user created, refresh user list
            filterUsers();
        }
        setShowAdd(false);
    }

    const tools = (<>
        <button onClick={() => setShowAdd(true)}><Icon name="add"></Icon>New User</button>
    </>);

    return (
        <div className="admin-users">
            {showAdd && <AddUser onClose={handleClosedAddUser}></AddUser>}
            {lock != null && <LockModal></LockModal>}
            <Container
                title="User Accounts"
                tools={tools}
            >
                <div className="filters">
                    <Input
                        name="usersearch"
                        type="text"
                        placeholder="Search by Full Name"
                        value={filter.fullName}
                        onInput={(e) => setFilter(prev => ({ ...prev, fullName: e.target.value }))}
                        className="fullNameInput"
                    />
                    <Select
                        options={roleFiltersList.map(role => ({ value: role.id, label: role.name }))}
                        value={filter.role}
                        onChange={(e) => setFilter(prev => ({ ...prev, role: e.target.value }))}
                    />
                </div>
                <table className="spreadsheet">
                    <thead>
                        <tr>
                            <th onClick={() => setFilter(prev => ({ ...prev, sort: handleSort('Email', filter.sort) }))}>
                                Email {getSortIcon('Email', filter.sort) && <span className="material-symbols-rounded">{getSortIcon('Email', filter.sort)}</span>}
                            </th>
                            <th onClick={() => setFilter(prev => ({ ...prev, sort: handleSort('FullName', filter.sort) }))}>
                                Full Name {getSortIcon('FullName', filter.sort) && <span className="material-symbols-rounded">{getSortIcon('FullName', filter.sort)}</span>}
                            </th>
                            <th onClick={() => setFilter(prev => ({ ...prev, sort: handleSort('Created', filter.sort) }))}>
                                Created {getSortIcon('Created', filter.sort) && <span className="material-symbols-rounded">{getSortIcon('Created', filter.sort)}</span>}
                            </th>
                            <th onClick={() => setFilter(prev => ({ ...prev, sort: handleSort('AR.Name', filter.sort) }))}>
                                Role {getSortIcon('AR.Name', filter.sort) && <span className="material-symbols-rounded">{getSortIcon('AR.Name', filter.sort)}</span>}
                            </th>
                            <th onClick={() => setFilter(prev => ({ ...prev, sort: handleSort('LastLogin', filter.sort) }))}>
                                Last Login {getSortIcon('LastLogin', filter.sort) && <span className="material-symbols-rounded">{getSortIcon('LastLogin', filter.sort)}</span>}
                            </th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                        {users.map(person =>
                            <tr 
                                key={person.id} 
                                onClick={(e) => {
                                    // Prevent triggering if the event originated from the lock link
                                    if (e.target.closest('a[title="lock user account"]')) {
                                        e.stopPropagation();
                                        return;
                                    }
                                    navigate('/admin/users/edit/' + person.id);
                                }}
                            >
                                <td>{person.email}</td>
                                <td>{person.fullName}</td>
                                <td>{person.created ? printDate(localDateTime(new Date(person.created))) : 'N/A'}</td>
                                <td>{person.roleName}</td>
                                <td>{person.lastLogin}</td>
                                <td className="buttons">
                                    <Link to={'/admin/users/edit/' + person.id} title="edit user details"><Icon name="edit_square"></Icon></Link>
                                    <Link 
                                        onClick={(e) => { 
                                            e.preventDefault(); 
                                            handleLock(person); 
                                        }} 
                                        title="lock user account"
                                    >
                                        <Icon name="lock"></Icon>
                                    </Link>
                                </td>
                            </tr>
                        )}
                    </tbody>
                </table>
                
                <Pagination
                    currentPage={Math.floor(filter.start / filter.length) + 1}
                    totalPages={totalPages}
                    pageSize={filter.length}
                    totalItems={totalItems}
                    onPageChange={handlePageChange}
                    maxPageNumbers={5}
                />
            </Container>
        </div>
    );
}