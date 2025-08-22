import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import './tree-view.css';

export default function TreeView({ label, children, defaultOpen = false, className = '', onClick, url, menu }) {
    const [isOpen, setIsOpen] = useState(defaultOpen);
    const hasChildren = React.Children.count(children) > 0;

    const handleToggle = (e) => {
        if (hasChildren) {
            setIsOpen(!isOpen);
            e.stopPropagation();
        }
    };

    const handleClick = (e) => {
        if (onClick) {
            onClick(e);
        }
    };

    return (
        <div className={`tree-view ${className}`}>
            <div
                className={`tree-view-item${isOpen ? ' open' : ''}`}
                onClick={!hasChildren && !url ? handleClick : undefined}
            >
                <div className="tree-view-header">
                    <div className="tree-view-toggle" onClick={handleToggle}>
                        {hasChildren && (
                            <span className="tree-view-icon">
                                {isOpen ? '−' : '+'}
                            </span>
                        )}
                        {!hasChildren && <span className="tree-view-spacer"></span>}
                    </div>

                    <div
                        className="tree-view-label"
                        onClick={(e) => {
                            if (hasChildren) handleToggle(e);
                            handleClick(e);
                        }}
                    >
                        {!hasChildren && url ?
                            <Link to={url} className="tree-view-link">{label}</Link>
                            :
                            <>{label}</>
                        }
                    </div>
                    <div className="tree-view-menu">{menu}</div>
                </div>
            </div>
            {hasChildren && (
                <div
                    className="tree-view-content"
                    style={{ display: isOpen ? 'block' : 'none' }}
                >
                    {children}
                </div>
            )}
        </div>
    );
}
