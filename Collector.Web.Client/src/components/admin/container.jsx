import React, { useState, useEffect, Children } from 'react';
import { Link } from 'react-router-dom';
import './container.css';
//components
import Icon from '@/components/ui/icon';

export default function AdminContainer({ children, title, className, tools, error, centered = false, tabs = null, selectedIndex, breadcrumbs = null, goback = null, onTab, isChild = false }) {
    const [showError, setShowError] = useState(error != null && error != '');
    const [tabIndex, setIndex] = useState(selectedIndex ?? 0);

    //effect
    useEffect(() => {
        setIndex(selectedIndex ?? 0);
    }, [selectedIndex]);

    const selectTab = (index) => {
        setIndex(index);
        typeof onTab == 'function' && onTab(index);
    };

    useEffect(() => {
        if (error != null && error != '') {
            setShowError(true);
        } else {
            setShowError(false);
        }
    }, [error]);

    const handleCloseError = () => {
        setShowError(false);
    }
    return (<div className={className || 'admin-section'}>
        <div className={"admin-header tool-bar" + (isChild ? ' is-child' : '')}>
            <div className={centered ? "container-centered" : ""}>
                <div>
                    <span className="admin-title">{title}</span>
                    {goback && <span className="goback"><Link to={goback}><Icon name="arrow_back"></Icon> Back</Link></span>}
                    {breadcrumbs && <span className="breadcrumbs">
                        {breadcrumbs.map((a, i) => (
                            <><Link to={a.url}>{a.label}</Link>{i < breadcrumbs.length - 1 && <>&nbsp;&gt;&nbsp;</>}</>
                        ))}</span>}
                    <div className="right-side">
                        {tools}
                    </div>
                </div>
                {tabs &&
                    <div className="tabs-row">
                        {tabs.map((tab, index) => (
                            <div key={title + 'tab_' + index} className={'tab-label' + (index == tabIndex ? ' selected' : '')}
                                onClick={() => { selectTab(index) }}
                            >
                                {tab}
                            </div>
                        ))}
                    </div>
                }
            </div>
        </div>
        <div className={"admin-body" + (centered ? "container-centered" : "")}>
            {showError && <Error message={error} onClose={handleCloseError}></Error>}
            {tabs ? [Children.toArray(children)[tabIndex]] : children}
        </div>
    </div>);
}