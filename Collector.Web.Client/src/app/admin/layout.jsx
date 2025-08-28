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
        setSection(location.pathname.split('/')[1]);
    }, [location]);

    const handleLogOut = (e) => {
        e.preventDefault();
        e.stopPropagation();
        logout();
        navigate('/login');
    }

    const MenuItem = ({ label, path, url, icon, onClick }) => {
        return <li className={section == path ? 'selected' : ''}><Link to={url} onClick={onClick}><Icon name={icon}></Icon>{label}</Link></li>
    }

    return (
        <div className={"app-container " + pathClassName}>
            <header className="main-header">
                <Navigation />
            </header>
            {isAuth && user?.isAdmin ?
                <main className="admin has-sidebar">
                    <div className='sidebar'>
                        <ul>
                            <MenuItem label="Domains" path="domains" url="/admin/domains" icon="domain"></MenuItem>
                            <MenuItem label="Downloads" path="downloads" url="/admin/downloads" icon="download"></MenuItem>
                            <MenuItem label="Articles" path="articles" url="/admin/articles" icon="article"></MenuItem>
                            <MenuItem label="Subjects" path="subjects" url="/admin/subjects" icon="graph_2"></MenuItem>
                            <MenuItem label="Blacklists" path="blacklists" url="/admin/blacklists" icon="block"></MenuItem>
                            <MenuItem label="Whitelists" path="whitelists" url="/admin/whitelists" icon="beenhere"></MenuItem>
                            <MenuItem label="Users" path="users" url="/admin/users" icon="group"></MenuItem>
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

    );
}