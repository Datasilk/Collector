import React, { useState, useEffect, Children } from 'react';
import { Link } from 'react-router-dom';
import './container.css';
//components
import Icon from '@/components/ui/icon';

export default function DashboardContainer({ children, title, tools, error, centered = false, tabs = null, selectedIndex, breadcrumbs = null, goback = null, onTab }) {
    const [showError, setShowError] = useState(error != null && error != '');
    const [tabIndex, setIndex] = useState(selectedIndex ?? 0);

    //effect
    useEffect(() => {
        setIndex(selectedIndex ?? 0);
    },[selectedIndex]);

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
    return (<>
        <div className="dash-header tool-bar">
            <div className={centered ? "container-centered" : ""}>
                <div className="dash-header-row mobile-header">
                    <span className="dash-title">{title}</span>
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
        <div className="dash-body">
            <div className={centered ? "container-centered" : ""}>
                {showError &&
                    <div className="error-container">
                        <div className="error-msg">
                            {error}
                            <div className="error-close" onClick={handleCloseError}><Icon name="close"></Icon></div>
                        </div>
                    </div>}
                {tabs ? [Children.toArray(children)[tabIndex]] : children}
            </div>
        </div>
    </>);
}