import React, { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
//components
import Container from '@/components/admin/container';
import Icon from '@/components/ui/icon';
import AddUser from '../components/add';
import AdminUserDetails from '../components/details';
import { LoadingPage } from '@/components/ui/loading';
//api
import { useSession } from '@/context/session';
import { Users } from '@/api/admin/users';

/**
 * <summary>Edit User Page</summary>
 * <description>Allows admins to edit an existing user's details in the admin panel.</description>
 */
export default function AdminUserEdit() {
    const { id } = useParams();
    const [user, setUser] = useState(null);
    const [showAdd, setShowAdd] = useState(false);
    const [error, setError] = useState(null);
    const { getById } = Users(useSession());

    useEffect(() => {
        getById(id)
            .then(response => {
                if (response.data.success) {
                    setUser(response.data.data);
                }
            })
            .catch(err => {
                setError(err.message);
            });
    }, [id]);

    const tools = (<>
        <button onClick={() => setShowAdd(true)}><Icon name="add"></Icon>New User</button>
    </>);

    return (<>
        {showAdd && <AddUser onClose={() => setShowAdd(false)}></AddUser>}
        <Container
            title={user ? user.fullName : 'Loading...'}
            error={error}
            tools={tools}
            tabs={['User Account']}
        >
            {user &&
                <div className="tab-1">
                    <AdminUserDetails user={user} setUser={setUser} onError={setError} />
                </div>
            }
            {!user && <LoadingPage></LoadingPage>}
        </Container>
    </>);
}