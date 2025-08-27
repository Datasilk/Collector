import React, { useState, useEffect, useRef } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import './navigation.css';
//components
import Icon from '@/components/ui/icon';
//context
import { useSession } from '@/context/session';

const Navigation = () => {
  //context
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout } = useSession();

  //state
  const [section, setSection] = useState('');
  const [showUserMenu, setShowUserMenu] = useState(false);

  //effect
  useEffect(() => {
    setSection(location.pathname.split('/')[1]);
  }, [location]);

  // Dropdown logic
  const handleMouseDownUserMenu = (e) => {
    let target = e.target;
    while (target) {
      if (target.classList?.contains('user-menu-popup') || target.classList?.contains('account-icon')) return;
      target = target.parentNode;
    }
    setShowUserMenu(false);
    document.removeEventListener('mousedown', handleMouseDownUserMenu);
  };
  const handleUserMenuClick = (e) => {
    //if (window.innerWidth < 1080) {
    e.stopPropagation();
    e.preventDefault();
    setShowUserMenu(!showUserMenu);
    if (!showUserMenu) {
      document.addEventListener('mousedown', handleMouseDownUserMenu);
    }
    //}
  };

  const handleLogOut = (e) => {
    e.preventDefault();
    e.stopPropagation();
    logout();
    navigate('/login');
  }

  return (
    <nav className="nav-container">
      <div className="nav-left">
        <img src="/logo.svg" alt="Collector Logo" className="logo" style={{ height: '2.2em' }} />
      </div>
      <div className="nav-right">
        {!user?.token ? <>
          <Link to="/signup" className={'nav-link' + (section == 'signup' ? ' selected' : '')}>Sign Up</Link>
          <Link to="/login" className={'nav-link' + (section == 'login' ? ' selected' : '')}>Login</Link>
        </>
          :
          <>
            {(user.isAdmin) && <Link to="/admin" className={'nav-link' + (section == 'admin' ? ' selected' : '')}>Administration</Link>}
            <Link to="/journal" className={'nav-link' + (section == 'journal' ? ' selected' : '')}>Journal</Link>
            <Link to="/account" title="My Account" className={'account-icon' + (section == 'account' ? ' selected' : '')} onClick={handleUserMenuClick}>
              <Icon name="person" />
            </Link>
            <ul className="user-menu-popup" style={{ display: showUserMenu ? 'block' : 'none' }}>
              {(user.isAdmin) && <li className="user-menu-item">
                <Link to="/admin"><Icon name="admin_panel_settings" /> Administration</Link>
              </li>}
              <li className="user-menu-item">
                <Link to="/account"><Icon name="person" /> My Account</Link>
              </li>
              <li className="user-menu-item">
                <Link to="/login" onClick={handleLogOut}>
                  <Icon name="logout" /> Log Out
                </Link>
              </li>
            </ul>
          </>
        }
      </div>
    </nav>
  );
};

export default Navigation;
