import React from 'react';
import Icon from '@/components/ui/icon';
import './message.css';

export default function Message({ children, onClose, type = 'info' }) {
    const handleClose = () => {
        if (onClose) onClose();
    };

    return (
        <div className={`message ${type}`}>
            <div className="message-content">{children}</div>
            {onClose && (
                <div className="message-close" onClick={handleClose}>
                    <Icon name="close" />
                </div>
            )}
        </div>
    );
}
