import React, { useState, useEffect } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
//styles
import './layout.css';
import '@/styles/admin/filter.css';
import '@/styles/layout.css';
//components
import Navigation from '@/components/navigation/navigation';
import Icon from '@/components/ui/icon';
//api
import { useSession } from '@/context/session';

/**
 * <summary>Dashboard Layout</summary>
 * <description>Provides the layout wrapper for all dashboard pages, including navigation and consistent structure.</description>
 */
export default function DashboardLayout({ children }) {
    //context
    const session = useSession();
    const navigate = useNavigate();
    const location = useLocation();

    //state
    const [isAuth, setAuth] = useState(null);
    const [mounted, setMounted] = useState(false);
    const [section, setSection] = useState('');
    const [conversations, setConversations] = useState('');
    const [pathClassName, setPathClassName] = useState('');
    const [showNewMenu, setShowNewMenu] = useState(false);

    //api
    const { logout, hasRole } = session;

    //effects
    useEffect(() => {
        if (!mounted) {
            setMounted(true);
            //check if user is allowed to view the dashboard
            if (!hasRole('user') && !hasRole('admin')) {
                navigate('/login');
            } else {
                setAuth(true);
            }
            window.removeEventListener('resize', handleResize);
            window.addEventListener('resize', handleResize);
        } else {
            handleResize();
        }
    }, [mounted]);

    useEffect(() => {
        setPathClassName(location.pathname.split('/').filter(a => a != '').slice(0, 2).join('-'));
        setSection(location.pathname.split('/')[1]);
    }, [location]);

    const handleResize = () => {
        const windowHeight = window.innerHeight;
        const sidebar = document.querySelector('.dashboard > .sidebar');
        const sidebarBottom = document.querySelector('.dashboard .sidebar-bottom');
        const rect = sidebar.getBoundingClientRect();
        const bottomRect = sidebarBottom.getBoundingClientRect();
        document.querySelector('.dashboard > .sidebar > .sidebar-top').style.height = (windowHeight - rect.y - bottomRect.height - 10) + 'px';
    }

    //actions
    const handleLogOut = (e) => {
        e.preventDefault();
        e.stopPropagation();
        logout();
        navigate('/login');
    }

    const handleToggleDashboardMenu = () => {
        const target = document.querySelector('.dashboard > .sidebar');
        const showing = target.style.display == 'block';
        target.style.display = showing ? '' : 'block';
        if (!showing) {
            document.addEventListener('mousedown', handleMouseDownDashboardMenu);
        } else {
            document.removeEventListener('mousedown', handleMouseDownDashboardMenu);
        }
    }

    const handleMouseDownDashboardMenu = (e) => {
        let target = e.target;
        while (target) {
            const classes = target.classList;
            if (classes?.contains('dashboard')) {
                document.removeEventListener('mousedown', handleMouseDownDashboardMenu);
                const target = document.querySelector('.dashboard > .sidebar');
                target.style.display = '';
                return;
            };
            if (classes?.contains('sidebar-mobile-toggle') || classes?.contains('sidebar')) return;
            target = target.parentNode;
        }
    }

    const handleNewPrompt = () => {
        navigate('/dashboard/conversation/new');
    }


    const MenuItem = ({ label, path, url, icon, onClick }) => {
        return <li className={section == path ? 'selected' : ''}><Link to={url} onClick={onClick}><Icon name={icon}></Icon><span className="label">{label}</span></Link></li>
    }

    return (
        <div className={"app-container " + pathClassName}>
            <header className="main-header">
                <Navigation />
            </header>
            {isAuth ?
                <main className={"dashboard has-sidebar" + (session.hasRole('admin') ? ' is-admin' : ' is-user')}>
                    <div className="sidebar-mobile-toggle" onClick={handleToggleDashboardMenu}>
                        <Icon name="menu"></Icon>
                    </div>
                    <div className="sidebar">
                        <div className="sidebar-top">
                            <div className="tool-bar">
                                <Link className="button" onClick={handleNewPrompt}><Icon name="add"></Icon>New Conversation</Link>
                            </div>
                            <ul className="user-conversations">
                                {conversations.map(a => <MenuItem key={'menuitem_' + a.id} label={a.name} path={'conversation/' + a.id} url={'/dashboard/conversation/' + a.id} icon="jamboard_kiosk" />)}
                            </ul>
                            {conversations.length > 0 && (
                                <div className="tool-bar">
                                    <div className="button" onClick={() => { navigate('/dashboard/conversations'); }}>
                                        <Icon name="folder_open"></Icon>View More
                                    </div>
                                </div>
                            )}
                        </div>

                        <div>
                            <ul className="sidebar-bottom">
                                <MenuItem label="Logout" path="login" url="/login" onClick={handleLogOut} icon="logout"></MenuItem>
                            </ul>
                        </div>
                    </div>
                    <div className="body">
                        {children}
                    </div>
                </main>
                :
                <main className="main-content"></main>
            }
        </div>
    );
}