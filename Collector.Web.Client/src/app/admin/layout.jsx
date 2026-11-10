/**
 * <summary>Admin Layout</summary>
 * <description>Provides the layout wrapper for all admin pages, including navigation and consistent structure.</description>
 */
import React, { useState, useEffect } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import './layout.css';
import '@/styles/layout.css';
import Navigation from '@/components/navigation/navigation';
import Icon from '@/components/ui/icon';
//context
import { useSession } from '@/context/session';
import { NavigationProvider } from '@/context/navigation';

export default function AdminLayout({ children }) {
    const navigate = useNavigate();
    const location = useLocation();
    const { user, logout, checkUser } = useSession();
    const [mounted, setMounted] = useState(false);
    const [section, setSection] = useState('');
    const [pathClassName, setPathClassName] = useState('');
    const [isAuth, setAuth] = useState(null);

    useEffect(() => {
        if (!mounted) {
            setMounted(true);
            //check if user is an Admin
            checkUser('admin').then(response => {
                const auth = response.data.success;
                setAuth(auth);
                if (!auth) {
                    navigate('/login');
                }
            }).catch(() => {
                navigate('/login');
            });
        }
    }, [mounted]);

    useEffect(() => {
        setPathClassName(location.pathname.split('/').filter(a => a != '').slice(0, 2).join('-'));
        setSection(location.pathname.split('/')[2] || '');
    }, [location]);

    const handleLogOut = (e) => {
        e.preventDefault();
        e.stopPropagation();
        logout();
        navigate('/login');
    }

    const handleToggleSidebar = () => {
        const target = document.querySelector('.admin > .sidebar');
        const showing = target.className.includes('show-mobile');
        if (!showing) {
            target.classList.add('show-mobile');
            document.addEventListener('mousedown', handleMouseDownSidebar);
        } else {
            target.classList.remove('show-mobile');
            document.removeEventListener('mousedown', handleMouseDownSidebar);
        }
    }

    const handleMouseDownSidebar = (e) => {
        let target = e.target;
        while (target) {
            const classes = target.classList;
            if (classes?.contains('admin')) {
                document.removeEventListener('mousedown', handleMouseDownSidebar);
                const sidebar = document.querySelector('.admin > .sidebar');
                sidebar.classList.remove('show-mobile');
                return;
            };
            if (classes?.contains('sidebar-mobile-toggle') || classes?.contains('sidebar')) return;
            target = target.parentNode;
        }
    }

    const MenuItem = ({ label, path, url, icon, onClick }) => {
        return <li className={section == path ? 'selected' : ''}><Link to={url} onClick={onClick}><Icon name={icon}></Icon>{label}</Link></li>
    }

    return (
        <NavigationProvider onToggleSidebar={handleToggleSidebar}>
            <div className={"app-container " + pathClassName}>
                <header className="main-header">
                    <Navigation />
                </header>
                {isAuth && user?.isAdmin ?
                    <main className="admin has-sidebar">
                        <div className='sidebar'>
                            <ul>
                                <MenuItem label="Users" path="users" url="/admin/users" icon="group"></MenuItem>
                                <MenuItem label="Open AI" path="openai" url="/admin/openai" icon="chat"></MenuItem>
                                <MenuItem label="Ollama" path="ollama" url="/admin/ollama" icon="smart_toy"></MenuItem>
                                <MenuItem label="Logout" path="logout" url="/login" onClick={handleLogOut} icon="logout"></MenuItem>
                            </ul>
                        </div>
                        <div className="body">
                            {children}
                        </div>
                    </main>
                    :
                    <main className="main-content">
                    </main>
                }
            </div>
        </NavigationProvider>

    );
}