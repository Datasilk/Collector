/**
 * <summary>Account Management Page</summary>
 * <description>Allows users to view and update their account details, including name, email, and password.</description>
 */
import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
//context
import { useSession } from '@/context/session';
//api
import { Users } from '@/api/account/users';
//components
import Input from '@/components/forms/input';
import Modal from '@/components/ui/modal';

const Account = () => {
  //context
  const navigate = useNavigate();
  const session = useSession();
  const {logout} = session;
  //state
  const [user, setUser] = useState(null);
  const [error, setError] = useState(null);
  const [showChangeEmail, setShowChangeEmail] = useState(false);
  const [showChangePassword, setShowChangePassword] = useState(false);
  const [newEmail, setNewEmail] = useState('');
  const [fullName, setFullName] = useState('');
  const [saving, setSaving] = useState(false);

  //api
  const { getMyInfo, resetPassword, edit, updateEmail } = Users(useSession());

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
    setError(null);
    const originalEmail = user.email;
    user.email = newEmail;
    try {
      const response = await updateEmail(user);
      if (response.data && response.data.success !== true) {
        setError(response.data.message);
      }
      // Optionally show a success message here
    } catch (err) {
      user.email = originalEmail; // revert on error
      setError(err.message);
    }
    setShowChangeEmail(false);
  };

  const handleSavePassword = async () => {
    if (!user) return;
    try {
      await resetPassword(user);
      // Optionally show a success message here
    } catch (err) {
      setError(err.message);
    }
    setShowChangePassword(false);
  };

  // Save changes handler
  const handleSaveChanges = async () => {
    if (!user) return;
    setSaving(true);
    setError(null);
    try {
      const updatedUser = {
        ...user,
        fullName: fullName
      };
      const response = await edit(updatedUser);
      if (response.data && response.data.success) {
        setUser(response.data.data);
        // Optionally show a success message
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
  }

  return (
    <div className="hero-container">
      <h2>Account Management</h2>
      
      {showChangeEmail && (
        <Modal onClose={() => setShowChangeEmail(false)}>
          <p>Enter your new email address below.</p>
          <Input
            type="email"
            placeholder="New Email Address"
            value={newEmail}
            onInput={e => setNewEmail(e.target.value)}
          ></Input>
          <button onClick={handleSaveEmail}>Save Changes</button>
        </Modal>
      )}

      {showChangePassword && (
        <Modal onClose={() => setShowChangePassword(false)}>
          <p>You will be sent an email with instructions to change your password.</p>
          <button onClick={handleSavePassword}>Okay</button>
          <button className='cancel' onClick={() => setShowChangePassword(false)}>Cancel</button>
        </Modal>
      )}

      {error && <div className="error-message">{error}</div>}
      
      {user && (
        <div className="account-details">
          <div>
            <label>Email Address: </label>
            <span>{user.email}</span>
            <a
              href="#"
              className="action-link"
              onClick={e => { e.preventDefault(); setShowChangeEmail(true); }}
              style={{ marginLeft: 8 }}
            >
              Change Email Address
            </a>
            <br /><br />
          </div>

          <div>
            <label>Password: </label>
            <span>***********</span>
            <a
              href="#"
              className="action-link"
              onClick={e => { e.preventDefault(); setShowChangePassword(true); }}
              style={{ marginLeft: 8 }}
            >
              Change Password
            </a>
            <br /><br />
          </div>

          <div className='form-row'>
            <Input
              label="Full Name:"
              type="text"
              value={fullName}
              onInput={e => setFullName(e.target.value)}
            />
          </div>
          
          <div style={{ marginTop: 10 }}>
            <button
              onClick={handleSaveChanges}
              disabled={saving}
            >
              {saving ? 'Saving...' : 'Save Changes'}
            </button>
          </div>
        </div>
      )}

      <br />
      <Link to="/login" onClick={handleLogOut}>Log out</Link>
    </div>
  );
};

export default Account;
