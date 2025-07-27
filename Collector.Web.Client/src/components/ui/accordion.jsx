import React, { useState } from 'react';
import './accordion.css';

export function Accordion({ title, children, defaultOpen = true, className = '', onOpen, onClose }) {
    const [open, setOpen] = useState(!!defaultOpen);

    const handleToggle = () => {
        setOpen(!open);
        if (open) {
            if (onClose) onClose();
        } else {
            if (onOpen) onOpen();
        }
    }

    return (
        <div className={`accordion ${className}`}>
            <div
                className={`accordion-item${open ? ' open' : ''}`}
            >
                <div
                    className="accordion-title"
                    onClick={handleToggle}
                    tabIndex={0}
                    role="button"
                    aria-expanded={open}
                    aria-controls={`accordion-content-0`}
                >
                    <span>{title}</span>
                    <span className="accordion-arrow">{open ? '\u25B2' : '\u25BC'}</span>
                </div>
                <div
                    className="accordion-content"
                    id={`accordion-content-0`}
                    style={{ display: open ? 'block' : 'none' }}
                >
                    {children}
                </div>
            </div>
        </div>
    );
}

