import React, { useState, useEffect } from 'react';
import './dashboard.css';

/**
 * <summary>Dashboard Main Page</summary>
 * <description>Main user dashboard overview and navigation to dashboard features.</description>
 */
export default function Dashboard() {

    //state
    const [userId] = useState(session.user?.appUserId);
    const [hasAccess, setHasAccess] = useState(false);

    //effects
    useEffect(() => {
        setHasAccess(session.hasRole('admin'));
    }, []);


    return (
        <>
        </>
    );
}