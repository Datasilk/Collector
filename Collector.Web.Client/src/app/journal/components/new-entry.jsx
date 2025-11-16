import React, { useState, useEffect, useRef } from 'react';
import Icon from '@/components/ui/icon';
import { useSession } from '@/context/session';
import entryTypes from '../entry-types';

export default function NewEntry({ journalId = null }) {
    const session = useSession();

    const [showEntryTypeDropdown, setShowEntryTypeDropdown] = useState(false);
    const entryTypeDropdownRef = useRef(null);

    const handleToggleEntryTypeDropdown = () => {
        setShowEntryTypeDropdown(!showEntryTypeDropdown);
    };

    const handleSelectEntryType = (entryType) => {
        setShowEntryTypeDropdown(false);

        session.showModal(() => (
            <entryType.modal
                onClose={() => session.hideModal()}
                journalId={journalId}
            />
        ));
    };

    useEffect(() => {
        const handleClickOutside = (event) => {
            if (entryTypeDropdownRef.current && !entryTypeDropdownRef.current.contains(event.target)) {
                setShowEntryTypeDropdown(false);
            }
        };

        if (showEntryTypeDropdown) {
            document.addEventListener('mousedown', handleClickOutside);
        }

        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
        };
    }, [showEntryTypeDropdown]);

    return (
        <div className="tool-bar entry-type-dropdown" ref={entryTypeDropdownRef}>
            <button onClick={handleToggleEntryTypeDropdown}>
                <Icon name="add" />New Entry
            </button>
            {showEntryTypeDropdown && (
                <div className="dropdown-menu">
                    {entryTypes.map(entryType => (
                        <div
                            key={entryType.id}
                            className="dropdown-item"
                            onClick={() => handleSelectEntryType(entryType)}
                        >
                            <Icon name={entryType.icon} />
                            <span>{entryType.name}</span>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}
