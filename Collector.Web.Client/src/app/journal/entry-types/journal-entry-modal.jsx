import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import Modal from '@/components/ui/modal';
import SelectJournal from '@/components/forms/select-journal';

/**
 * <summary>Journal Entry Modal</summary>
 * <description>Modal for creating a new journal entry by selecting category and journal.</description>
 */
export default function JournalEntryModal({ onClose, journalId = null, defaultTagIds = null }) {
    const navigate = useNavigate();

    const [selectedJournalId, setSelectedJournalId] = useState(journalId);

    useEffect(() => {
        if (journalId) handleCreateEntry(journalId);
    }, [journalId]);

    const handleCreateEntry = (id) => {
        const journalIdToUse = id ?? selectedJournalId;
        if (journalIdToUse) {
            const navOptions = {};
            if (Array.isArray(defaultTagIds) && defaultTagIds.length > 0) {
                navOptions.state = { defaultTagIds };
            }
            navigate(`/journal/${journalIdToUse}/entry/new`, navOptions);
            onClose();
        }
    };
    if (journalId) return <></>;
    return (
        <Modal title="Create Journal Entry" onClose={onClose}>
            <div className="modal-form">
                <SelectJournal
                    journalId={journalId}
                    onChange={({ categoryId, journalId: selectedId }) => {
                        setSelectedJournalId(selectedId || '');
                    }}
                />
                <div className="buttons">
                    <button className="cancel" onClick={onClose}>Cancel</button>
                    <button
                        onClick={() => handleCreateEntry()}
                        disabled={!selectedJournalId}
                    >
                        Create Entry
                    </button>
                </div>
            </div>
        </Modal>
    );
}
