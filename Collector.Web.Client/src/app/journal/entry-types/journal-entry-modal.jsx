import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useSession } from '@/context/session';
import { Journals } from '@/api/user/journals';
import Modal from '@/components/ui/modal';
import Select from '@/components/forms/select';

/**
 * <summary>Journal Entry Modal</summary>
 * <description>Modal for creating a new journal entry by selecting category and journal.</description>
 */
export default function JournalEntryModal({ onClose }) {
    const session = useSession();
    const navigate = useNavigate();

    const [categories, setCategories] = useState([]);
    const [selectedCategoryId, setSelectedCategoryId] = useState('');
    const [selectedJournalId, setSelectedJournalId] = useState('');
    const [loading, setLoading] = useState(false);
    const [journals, setJournals] = useState([]);

    useEffect(() => {
        fetchCategories();
    }, []);

    useEffect(() => {
        if (selectedCategoryId) {
            const category = categories.find(c => c.id == selectedCategoryId);
            if (category && category.journals) {
                setJournals(category.journals);
                // Auto-select first journal if available
                if (category.journals.length > 0) {
                    setSelectedJournalId(category.journals[0].id);
                }
            } else {
                setJournals([]);
                setSelectedJournalId('');
            }
        } else {
            setJournals([]);
            setSelectedJournalId('');
        }
    }, [selectedCategoryId, categories]);

    const fetchCategories = () => {
        setLoading(true);
        Journals(session).getCategories().then(response => {
            if (response.data && response.data.success) {
                setCategories(response.data.data);
                // Auto-select first category if available
                if (response.data.data.length > 0) {
                    setSelectedCategoryId(response.data.data[0].id);
                }
            }
            setLoading(false);
        }).catch(err => {
            console.error('Error fetching categories:', err);
            setLoading(false);
        });
    };

    const handleCreateEntry = () => {
        if (selectedJournalId) {
            navigate(`/journal/${selectedJournalId}/entry/new`);
            onClose();
        }
    };

    const categoryOptions = categories.map(cat => ({
        label: cat.title,
        value: cat.id
    }));

    const journalOptions = journals.map(journal => ({
        label: journal.title,
        value: journal.id
    }));

    return (
        <Modal title="Create Journal Entry" onClose={onClose}>
            <div className="modal-form">
                <div className="col-2">
                    <Select
                        label="Category"
                        name="category"
                        options={categoryOptions}
                        value={selectedCategoryId}
                        onChange={(e) => setSelectedCategoryId(e.target.value)}
                    />
                </div>
                <div className="col-2">
                    {selectedCategoryId && (
                        <Select
                            label="Journal"
                            name="journal"
                            options={journalOptions}
                            value={selectedJournalId}
                            onChange={(e) => setSelectedJournalId(e.target.value)}
                        />
                    )}
                </div>
                <div className="buttons">
                    <button className="cancel" onClick={onClose}>Cancel</button>
                    <button
                        onClick={handleCreateEntry}
                        disabled={!selectedJournalId || loading}
                    >
                        Create Entry
                    </button>
                </div>
            </div>
        </Modal>
    );
}
