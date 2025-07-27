import React, { useState } from 'react';
//styles
import '@/styles/forms.css';
//components
import Input from '@/components/forms/input';
import Select from '@/components/forms/select';
import Checkbox from '@/components/forms/checkbox';
import Modal from '@/components/ui/modal';
import Icon from '@/components/ui/icon';
import messages from '@/helpers/messages';
//api
import { useSession } from '@/context/session';
import { Users } from '@/api/admin/users';

/**
 * <summary>Add User Component</summary>
 * <description>Form and logic for adding a new user in the admin panel.</description>
 */
export default function AdminAddUser({ onClose, onError }) {
    const [errors, setErrors] = useState({});
    const [formState, setFormState] = useState('new');
    const [user, setUser] = useState({ role: 4, isAdmin: false });
    const emailRegex = /^[a-zA-Z0-9._+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,10}$/;

    //api
    const session = useSession();
    const { add } = Users(session);

    const handleChange = (e) => {
        const { name, value, type, checked } = e.target;
        const data = { ...user, [name]: type === 'checkbox' ? checked : value };

        const errs = errors;
        let changed = false;
        if (errs.fullName && data.fullName != '') {
            errs.fullName = null;
            changed = true;
        }
        if (errs.email && data.email != '') {
            errs.email = null;
            changed = true;
        }
        if (changed == true) {
            setErrors(errs);
        }
        setUser(data);
    };

    const handleSubmit = (e) => {
        e.preventDefault();
        const newErrors = { exists: false };
        //validate form
        if (!user.fullName || user.fullName == '') {
            newErrors.fullName = 'required';
            newErrors.exists = true;
        }
        if (!user.email || user.email == '') {
            newErrors.email = 'required';
            newErrors.exists = true;
        }
        if (!emailRegex.test(user.email)) {
            newErrors.email = 'invalid email';
            newErrors.exists = true;
        }
        setErrors(newErrors);

        if (!newErrors.exists) {
            //submit form
            setFormState('submitting');
            //add a new user
            add({
                FullName: user.fullName,
                Email: user.email,
                Role: user.role,
                IsAdmin: user.isAdmin
            }).then(response => {
                if (response.data.success) {
                    setFormState('success');
                    onClose(response.data.data);
                } else {
                    setErrors({ ...errors, form: response.data.message });
                    setFormState('error');
                    if (typeof onError == 'function') onError(response.data.message);
                }
            }).catch(() => {
                setErrors({ ...errors, form: messages.errors.generic });
                setFormState('error');
                if (typeof onError == 'function') onError(messages.errors.generic);
            });
        }
    };

    return (
        <Modal
            title="Add New User"
            onClose={onClose}
        >
            <div className="form-sized">
                <form className="admin-form" onSubmit={handleSubmit}>
                    <div className="form-row">
                        <Input
                            label="Full Name"
                            name="fullName"
                            value={user.fullName || ''}
                            onInput={handleChange}
                            required={true}
                            maxLength={64}
                            placeholder="Enter full name"
                            error={errors.fullName}
                        />
                        <Input
                            label="Email Address"
                            name="email"
                            value={user?.email ?? ''}
                            onInput={handleChange}
                            required={true}
                            maxLength={64}
                            placeholder="Enter user's email address"
                            error={errors.email}
                        ></Input>
                    </div>
                    <div className="form-row">
                        <Checkbox
                            label="Administrator"
                            name="isAdmin"
                            checked={!!user.isAdmin}
                            onChange={handleChange}
                        />
                    </div>
                    {formState == 'submitting' ?
                        <div className="submitting">Creating User Account...</div>
                        :
                        <div className="buttons">
                            <button type="submit" className="action-button">
                                <><Icon name="add_circle"></Icon>Create User Account</>
                            </button>
                            <button onClick={onClose} className="cancel"><Icon name="close"></Icon>Cancel</button>
                        </div>
                    }
                </form>
            </div>
        </Modal>);
}
