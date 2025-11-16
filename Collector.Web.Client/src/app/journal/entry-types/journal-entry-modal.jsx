import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import Modal from '@/components/ui/modal';
import SelectJournal from '@/components/forms/select-journal';

/**
 * <summary>Journal Entry Modal</summary>
 * <description>Modal for creating a new journal entry by selecting category and journal.</description>
 */
export default function JournalEntryModal({ onClose }) {
    const navigate = useNavigate();

    const [selectedJournalId, setSelectedJournalId] = useState('');

    const handleCreateEntry = () => {
        if (selectedJournalId) {
            navigate(`/journal/${selectedJournalId}/entry/new`);
            onClose();
        }
    };

    return (
        <Modal title="Create Journal Entry" onClose={onClose}>
            <div className="modal-form">
                <SelectJournal
                    onChange={({ categoryId, journalId }) => {
                        setSelectedJournalId(journalId || '');
                    }}
                />
                <div className="buttons">
                    <button className="cancel" onClick={onClose}>Cancel</button>
                    <button
                        onClick={handleCreateEntry}
                        disabled={!selectedJournalId}
                    >
                        Create Entry
                    </button>
                </div>
            </div>
        </Modal>
    );
}
