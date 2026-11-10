/**
 * <summary>Account Management Page</summary>
 * <description>Allows users to view and update their account details, including name, email, and password.</description>
 */
import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import './page.css';
//context
import { useSession } from '@/context/session';
//api
import { Users } from '@/api/account/users';
//components
import Input from '@/components/forms/input';
import Modal from '@/components/ui/modal';
import Icon from '@/components/ui/icon';

const Account = () => {
  //context
  const navigate = useNavigate();
  const session = useSession();
  const { logout } = session;
  //state
  const [user, setUser] = useState(null);
  const [error, setError] = useState(null);
  const [success, setSuccess] = useState(null);
  const [showChangeEmail, setShowChangeEmail] = useState(false);
  const [showChangePassword, setShowChangePassword] = useState(false);
  const [newEmail, setNewEmail] = useState('');
  const [fullName, setFullName] = useState('');
  const [saving, setSaving] = useState(false);

  //api
  const { getMyInfo, resetPassword, edit, updateEmail } = Users(useSession());

  const clearMessages = () => {
    setError(null);
    setSuccess(null);
  };

  const showSuccess = (message) => {
    setSuccess(message);
    setTimeout(() => setSuccess(null), 3000);
  };

  //effect
  useEffect(() => {
    if (session?.user?.appUserId) {
      getMyInfo().then(response => {
        if (response.data.success) {
          setUser(response.data.data);
          setFullName(response.data.data.fullName || '');
        }
      }).catch(err => {
        setError(err.message);
      });
    }
  }, [session?.user?.appUserId]);

  //actions
  const handleSaveEmail = async () => {
    if (!user) return;
    clearMessages();
    const originalEmail = user.email;
    user.email = newEmail;
    try {
      const response = await updateEmail(user);
      if (response.data && response.data.success !== true) {
        setError(response.data.message);
        user.email = originalEmail;
      } else {
        showSuccess('Email address updated successfully');
      }
    } catch (err) {
      user.email = originalEmail;
      setError(err.message);
    }
    setShowChangeEmail(false);
  };

  const handleSavePassword = async () => {
    if (!user) return;
    clearMessages();
    try {
      await resetPassword(user);
      showSuccess('Password reset email sent');
    } catch (err) {
      setError(err.message);
    }
    setShowChangePassword(false);
  };

  const handleSaveChanges = async () => {
    if (!user) return;
    setSaving(true);
    clearMessages();
    try {
      const updatedUser = {
        ...user,
        fullName: fullName
      };
      const response = await edit(updatedUser);
      if (response.data && response.data.success) {
        setUser(response.data.data);
        showSuccess('Profile saved successfully');
      } else {
        setError(response.data?.message || 'Failed to save changes');
      }
    } catch (err) {
      setError(err.message);
    }
    setSaving(false);
  };

  const handleLogOut = (e) => {
    e.preventDefault();
    e.stopPropagation();
    logout();
    navigate('/login');
  };

  return (
    <div className="account-container">
      <div className="account-card">
        <h2>Account Management</h2>

        {error && <div className="error-message">{error}</div>}
        {success && <div className="success-message">{success}</div>}

        {showChangeEmail && (
          <Modal title="Change Email Address" onClose={() => setShowChangeEmail(false)}>
            <p>Enter your new email address below.</p>
            <Input
              type="email"
              placeholder="New Email Address"
              value={newEmail}
              onInput={e => setNewEmail(e.target.value)}
              wide={true}
            />
            <div className="buttons">
              <button className="submit" onClick={handleSaveEmail}>Save</button>
              <button className="cancel" onClick={() => setShowChangeEmail(false)}>Cancel</button>
            </div>
          </Modal>
        )}

        {showChangePassword && (
          <Modal title="Change Password" onClose={() => setShowChangePassword(false)}>
            <p>You will be sent an email with instructions to change your password.</p>
            <div className="buttons">
              <button className="submit" onClick={handleSavePassword}>Send Email</button>
              <button className="cancel" onClick={() => setShowChangePassword(false)}>Cancel</button>
            </div>
          </Modal>
        )}

        {user && (
          <>
            <div className="account-section">
              <h3>Profile</h3>
              <Input
                label="Full Name"
                type="text"
                value={fullName}
                onInput={e => setFullName(e.target.value)}
                wide={true}
              />
              <div className="buttons">
                <button
                  className="submit"
                  onClick={handleSaveChanges}
                  disabled={saving}
                >
                  {saving ? 'Saving...' : 'Save Changes'}
                </button>
              </div>
            </div>

            <div className="account-section">
              <h3>Account Information</h3>
              <div className="account-row">
                <span className="label">Email</span>
                <span className="value">{user.email}</span>
                <button
                  className="icon"
                  onClick={() => setShowChangeEmail(true)}
                  title="Change email address"
                >
                  <Icon name="edit" />
                </button>
              </div>
              <div className="account-row">
                <span className="label">Password</span>
                <span className="value">***********</span>
                <button
                  className="icon"
                  onClick={() => setShowChangePassword(true)}
                  title="Change password"
                >
                  <Icon name="edit" />
                </button>
              </div>
            </div>
          </>
        )}

        <div className="account-logout">
          <Link to="/login" onClick={handleLogOut}>
            <Icon name="logout" /> Log out
          </Link>
        </div>
      </div>
    </div>
  );
};

export default Account;
