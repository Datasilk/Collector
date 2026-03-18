import React, { useState, useEffect, useRef } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import './navigation.css';
//components
import Icon from '@/components/ui/icon';
import ToggleSwitch from '@/components/ui/toggle-switch';
import workers from './workers';
//context
import { useSession } from '@/context/session';
import { useWorkerHub } from '@/context/workerhub';
import { useTheme } from '@/context/theme';

const Navigation = () => {
  //context
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout } = useSession();
  const { getWorkers, progressAll, subscribe } = useWorkerHub();
  const { theme, toggleTheme } = useTheme();

  //state
  const [section, setSection] = useState('');
  const [showUserMenu, setShowUserMenu] = useState(false);
  const [showNotifications, setShowNotifications] = useState(false);
  const [activeWorkers, setActiveWorkers] = useState([]);
  const [workerProgress, setWorkerProgress] = useState({});

  //effect
  useEffect(() => {
    setSection(location.pathname.split('/')[1]);
  }, [location]);

  // Fetch active workers and subscribe to progress updates
  useEffect(() => {
    if (!user?.token) return;

    const fetchWorkers = async () => {
      try {
        const workerList = await getWorkers();
        setActiveWorkers(workerList || []);

        // Subscribe to each worker for progress updates
        for (const worker of workerList || []) {
          subscribe(worker.workerId, ({ eventName, payload }) => {
              setWorkerProgress(prev => ({
                ...prev,
                [worker.workerId]: {...prev[worker.workerId], ...payload}
              }));
          });
        }

        // Request progress for all workers
        if (workerList?.length > 0) {
          await progressAll();
        }
      } catch (err) {
        console.error('Error fetching workers:', err);
      }
    };

    fetchWorkers();

    // Poll for new workers every 10 seconds
    const interval = setInterval(fetchWorkers, 10000);
    return () => clearInterval(interval);
  }, [user?.token]);

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

  // Notifications dropdown logic
  const handleMouseDownNotifications = (e) => {
    let target = e.target;
    while (target) {
      if (target.classList?.contains('notifications-popup') || target.classList?.contains('notifications-icon')) return;
      target = target.parentNode;
    }
    setShowNotifications(false);
    document.removeEventListener('mousedown', handleMouseDownNotifications);
  };

  const handleNotificationsClick = (e) => {
    e.stopPropagation();
    e.preventDefault();
    setShowNotifications(!showNotifications);
    if (!showNotifications) {
      document.addEventListener('mousedown', handleMouseDownNotifications);
    }
  };

  const renderWorkerComponent = (worker) => {
    const workerConfig = workers[worker.route];
    if (!workerConfig) return null;

    const WorkerComponent = workerConfig.component;
    const progress = workerProgress[worker.workerId];

    return (
      <WorkerComponent 
        key={worker.workerId} 
        worker={worker} 
        progress={progress} 
      />
    );
  };

  return (
    <nav className="nav-container">
      <div className="nav-left">
        <svg className="logo" viewBox="0 0 550 130">
          <use href="#logo" />
        </svg>
      </div>
      <div className="nav-right">
        {!user?.token ? <>
          <Link to="/signup" className={'nav-link' + (section == 'signup' ? ' selected' : '')}>Sign Up</Link>
          <Link to="/login" className={'nav-link' + (section == 'login' ? ' selected' : '')}>Login</Link>
        </>
          :
          <>
            {(user.isAdmin) && <>
              <Link to="/admin" className={'nav-link' + (section == 'admin' ? ' selected' : '')}>Administration</Link>
            </>}
            <Link to="/journal" className={'nav-link' + (section == 'journal' ? ' selected' : '')}>Journal</Link>
            <Link to="/feeds" className={'nav-link' + (section == 'feeds' ? ' selected' : '')}>Feeds</Link>
            <a href="#" title="Notifications" className="notifications-icon" onClick={handleNotificationsClick}>
              <Icon name="notifications" />
              {activeWorkers.length > 0 && <span className="notification-dot" />}
            </a>
            <div className="notifications-popup" style={{ display: showNotifications ? 'block' : 'none' }}>
              {activeWorkers.length === 0 ? (
                <div className="no-notifications">No active tasks</div>
              ) : (
                <div className="notifications-list">
                  {activeWorkers.map(worker => renderWorkerComponent(worker))}
                </div>
              )}
            </div>
            <Link to="/account" title="My Account" className={'account-icon' + (section == 'account' ? ' selected' : '')} onClick={handleUserMenuClick}>
              <Icon name="person" />
            </Link>
            <ul className="user-menu-popup" style={{ display: showUserMenu ? 'block' : 'none' }}>
              {(user.isAdmin) && <>
                <li className="user-menu-item">
                  <Link to="/admin"><Icon name="admin_panel_settings" /> Administration</Link>
                </li>
              </>}
              <li className="user-menu-item">
                <Link to="/account"><Icon name="person" /> My Account</Link>
              </li>
              <li className="user-menu-item theme-toggle">
                <Icon name="dark_mode" />
                <span>Dark Mode</span>
                <ToggleSwitch 
                  checked={theme === 'dark'} 
                  onChange={toggleTheme}
                  name="theme-toggle"
                />
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
