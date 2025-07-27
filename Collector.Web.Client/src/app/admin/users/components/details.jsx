import React, { useState } from 'react';
//components
import Input from '@/components/forms/input';
import Select from '@/components/forms/select';
import Checkbox from '@/components/forms/checkbox';
import Modal from '@/components/ui/modal';
import Icon from '@/components/ui/icon';
import messages from '@/helpers/messages';
import { useSession } from '@/context/session';
import { Users } from '@/api/admin/users';

/**
 * <summary>Admin User Details</summary>
 * <description>Form and logic for editing user details in the admin panel, including role assignment, email changes, and password resets.</description>
 */
export default function AdminUserDetails({ user, setUser, onError }) {
    const [errors, setErrors] = useState({});
    const [formState, setFormState] = useState('edit');
    const [saved, setSaved] = useState(false);
    const [showChangeEmail, setShowChangeEmail] = useState(false);
    const [showChangePassword, setShowChangePassword] = useState(false);
    const [newEmail, setNewEmail] = useState('');
    const session = useSession();
    const { edit, updateEmail, resetPassword } = Users(session);

    const handleChange = (e) => {
        const { name, value } = e.target;
        setUser({ ...user, [name]: value });
        if (errors[name]) setErrors({ ...errors, [name]: null });
    };

    const handleIsAdminChange = (e) => {
    setUser({ ...user, isAdmin: e.target.checked });
    if (errors.isAdmin) setErrors({ ...errors, isAdmin: null });
};

    const handleSubmit = async (e) => {
        e.preventDefault();
        const newErrors = {};
        if (!user.fullName || user.fullName.trim() === '') newErrors.fullName = 'required';
        // No role validation needed for isAdmin checkbox
        setErrors(newErrors);
        if (Object.keys(newErrors).length > 0) return;
        setFormState('submitting');
        try {
            const updatedUser = {
                ...user,
                fullName: user.fullName,
                isAdmin: user.isAdmin,
                status: user.status
            };
            const response = await edit(updatedUser);
            if (response.data && response.data.success) {
                setUser(response.data.data);
                setSaved(true);
                setFormState('success');
            } else {
                setErrors({ ...errors, form: response.data?.message || 'Failed to save changes' });
                setFormState('error');
                if (typeof onError === 'function') onError(response.data?.message || 'Failed to save changes');
            }
        } catch (err) {
            console.log(err);
            setErrors({ ...errors, form: messages.errors.generic });
            setFormState('error');
            if (typeof onError === 'function') onError(messages.errors.generic);
        }
    };

    const handleSaveEmail = async () => {
        if (!user) return;
        const originalEmail = user.email;
        setErrors({});
        user.email = newEmail;
        try {
            const response = await updateEmail(user);
            if (response.data && response.data.success !== true) {
                setErrors({ email: response.data.message });
            }
        } catch (err) {
            user.email = originalEmail;
            setErrors({ email: err.message });
        }
        setShowChangeEmail(false);
    };

    const handleSavePassword = async () => {
        if (!user) return;
        try {
            await resetPassword(user);
        } catch (err) {
            setErrors({ password: err.message });
        }
        setShowChangePassword(false);
    };

    const SavedModal = () => (
        <Modal title="User Saved" onClose={() => setSaved(false)}>
            <p>Changes to the user have been saved.</p>
            <div className="buttons">
                <button className="cancel" onClick={() => setSaved(false)}>Okay</button>
            </div>
        </Modal>
    );

    return (
        <>{saved && <SavedModal />}
            <div className="form-sized">
                <form className="admin-form" onSubmit={handleSubmit}>
                    <div className="form-row">
                        <Input
                            label={<>Email Address<a href="#" onClick={e => { e.preventDefault(); setShowChangeEmail(true); }}>Change Email Address</a></>}
                            name="email"
                            isLabel={true}
                            value={user?.email ?? ''}
                            disabled
                        />
                        <Input
                            label={<>Password<a href="#" onClick={e => { e.preventDefault(); setShowChangePassword(true); }}>Change Password</a></>}
                            name="password"
                            isLabel={true}
                            value={'***********'}
                            disabled
                        />
                    </div>
                    <div className="form-row">
                        <Input
                            label="Full Name"
                            name="fullName"
                            value={user?.fullName ?? ''}
                            onInput={handleChange}
                            required={true}
                            maxLength={64}
                            placeholder="Enter full name"
                            error={errors.fullName}
                        />
                        <Checkbox
                            label="Administrator Privileges"
                            name="isAdmin"
                            checked={user?.isAdmin ?? false}
                            onChange={handleIsAdminChange}
                            error={errors.isAdmin}
                        />
                        <Select
                            label="Status"
                            name="status"
                            value={user?.status ?? 0}
                            onChange={handleChange}
                            options={[
                                { value: 0, label: 'New' },
                                { value: 1, label: 'Active' },
                                { value: 2, label: 'Locked' },
                                { value: 3, label: 'Archived' }
                            ]}
                            error={errors.status}
                        />
                    </div>
                    {showChangeEmail && (
                        <Modal onClose={() => setShowChangeEmail(false)}>
                            <Input
                                type="email"
                                placeholder="New Email Address"
                                value={newEmail}
                                onChange={e => setNewEmail(e.target.value)}
                            />
                            <button onClick={handleSaveEmail}>Save Changes</button>
                        </Modal>
                    )}
                    {showChangePassword && (
                        <Modal onClose={() => setShowChangePassword(false)}>
                            <p>The user will be sent an email instructing them to change their password.</p>
                            <button onClick={handleSavePassword}>Okay</button>
                            <button className='cancel' onClick={() => setShowChangePassword(false)}>Cancel</button>
                        </Modal>
                    )}
                    {formState === 'submitting' ? (
                        <div className="submitting">Saving Changes...</div>
                    ) : (
                        <div className="buttons">
                            <button type="submit" className="action-button">
                                <Icon name="save" />Save Changes
                            </button>
                        </div>
                    )}
                    {errors.form && <div className="form-error">{errors.form}</div>}
                </form>
            </div>
        </>
    );
}
