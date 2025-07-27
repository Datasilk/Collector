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
    const [fullName, setFullName] = useState('');
    const [roleFilters, setRoleFilters] = useState(0);
    const [roleFiltersList, setRoleFiltersList] = useState([]);
    const [sort, setSort] = useState('Email ASC');

    //execute api
    useEffect(() => {
        if (users.length === 0) {
            filterUsers(); // Ensure this is only called once during initial load
        }
        fetchRoles();
    }, []);
        
    useEffect(() => {
        filterUsers();
    }, [fullName, roleFilters, sort]);

    const fetchRoles = () => {
        getRoles().then(response => {
            if (response.data.success) {
                setRoleFiltersList([{ id: 0, name: 'Any Role' }, ...response.data.data]); // Add 'Any' option
            }
        }).catch(() => { });
    };

    const filterUsers = () => {
        getUsersFiltered({
            fullName,
            role: roleFilters,
            sort
        }).then(response => {
            if (response.data.success) {
                setUsers(response.data.data);
            }
        }).catch(() => { });
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
                } else {
                    // Handle failure case
                    messages.errors.generic;
                    console.error('Failed to lock user:', response.data.message);
                }
            })
            .catch(error => {
                // Handle error case
                console.error('Error locking user:', error);
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
                        value={fullName}
                        onChange={(e) => setFullName(e.target.value)}
                        className="fullNameInput"
                    />
                    <Select
                        options={roleFiltersList.map(role => ({ value: role.id, label: role.name }))}
                        value={roleFilters}
                        onChange={(e) => setRoleFilters(e.target.value)}
                    />
                </div>
                <table className="spreadsheet">
                    <thead>
                        <tr>
                            <th onClick={() => setSort(handleSort('Email', sort))}>
                                Email {getSortIcon('Email', sort) && <span className="material-symbols-rounded">{getSortIcon('Email', sort)}</span>}
                            </th>
                            <th onClick={() => setSort(handleSort('FullName', sort))}>
                                Full Name {getSortIcon('FullName', sort) && <span className="material-symbols-rounded">{getSortIcon('FullName', sort)}</span>}
                            </th>
                            <th onClick={() => setSort(handleSort('Created', sort))}>
                                Created {getSortIcon('Created', sort) && <span className="material-symbols-rounded">{getSortIcon('Created', sort)}</span>}
                            </th>
                            <th onClick={() => setSort(handleSort('AR.Name', sort))}>
                                Role {getSortIcon('AR.Name', sort) && <span className="material-symbols-rounded">{getSortIcon('Role', sort)}</span>}
                            </th>
                            <th onClick={() => setSort(handleSort('LastLogin', sort))}>
                                Last Login {getSortIcon('LastLogin', sort) && <span className="material-symbols-rounded">{getSortIcon('LastLogin', sort)}</span>}
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
            </Container>
        </div>
    );
}