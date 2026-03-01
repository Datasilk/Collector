import { useState, useEffect } from 'react';
import Modal from '@/components/ui/modal';
import Select from '@/components/forms/select';
import Icon from '@/components/ui/icon';
import { useSession } from '@/context/session';
import { Journals } from '@/api/user/journals';

export default function MoveEntryModal({ entry, currentJournalId, onClose, onMoved }) {
    const session = useSession();
    const { getCategories, getJournals, moveEntry } = Journals(session);

    const [categories, setCategories] = useState([]);
    const [journals, setJournals] = useState([]);
    const [selectedCategoryId, setSelectedCategoryId] = useState('');
    const [selectedJournalId, setSelectedJournalId] = useState('');
    const [loading, setLoading] = useState(true);
    const [moving, setMoving] = useState(false);
    const [filteredJournals, setFilteredJournals] = useState([]);

    useEffect(() => {
        loadData();
    }, []);

    useEffect(() => {
        if (selectedCategoryId) {
            const filtered = journals.filter(j => j.categoryId === parseInt(selectedCategoryId));
            setFilteredJournals(filtered);
            setSelectedJournalId('');
        } else {
            setFilteredJournals([]);
            setSelectedJournalId('');
        }
    }, [selectedCategoryId, journals]);

    const loadData = async () => {
        try {
            setLoading(true);
            const [categoriesResponse, journalsResponse] = await Promise.all([
                getCategories(),
                getJournals()
            ]);

            if (categoriesResponse.data.success) {
                setCategories(categoriesResponse.data.data || []);
            }

            if (journalsResponse.data.success) {
                const journalsList = journalsResponse.data.data || [];
                setJournals(journalsList);
                
                // Set default category and journal based on current journal
                if (currentJournalId) {
                    const currentJournal = journalsList.find(j => j.id === currentJournalId);
                    if (currentJournal) {
                        setSelectedCategoryId(currentJournal.categoryId.toString());
                        setSelectedJournalId(currentJournal.id.toString());
                    }
                }
            }
        } catch (err) {
            console.error('Error loading data:', err);
        } finally {
            setLoading(false);
        }
    };

    const handleMove = async () => {
        if (!selectedJournalId || moving) return;

        setMoving(true);
        try {
            const response = await moveEntry(entry.id, selectedJournalId);
            
            if (response.data.success) {
                const selectedJournal = journals.find(j => j.id === parseInt(selectedJournalId));
                if (onMoved) {
                    onMoved(selectedJournal);
                }
                onClose();
            } else {
                alert('Failed to move entry: ' + (response.data.message || 'Unknown error'));
            }
        } catch (err) {
            console.error('Error moving entry:', err);
            alert('Failed to move entry. Please try again.');
        } finally {
            setMoving(false);
        }
    };

    const categoryOptions = categories.map(cat => ({
        value: cat.id.toString(),
        label: cat.title
    }));

    const journalOptions = filteredJournals.map(journal => ({
        value: journal.id.toString(),
        label: journal.title
    }));

    return (
        <Modal title="Move Entry" onClose={onClose}>
            {loading ? (
                <div style={{ padding: '2em', textAlign: 'center' }}>
                    <Icon name="progress_activity" spin={true} />
                    <p>Loading...</p>
                </div>
            ) : (
                <>
                    <div style={{ display: 'flex', gap: '1em', marginBottom: '1em' }}>
                        <div style={{ flex: 1 }}>
                            <Select
                                label="Category"
                                name="category"
                                value={selectedCategoryId}
                                onChange={(e) => setSelectedCategoryId(e.target.value)}
                                options={[
                                    { value: '', label: 'Select a category...' },
                                    ...categoryOptions
                                ]}
                            />
                        </div>
                        {selectedCategoryId && (
                            <div style={{ flex: 1 }}>
                                <Select
                                    label="Journal"
                                    name="journal"
                                    value={selectedJournalId}
                                    onChange={(e) => setSelectedJournalId(e.target.value)}
                                    options={[
                                        { value: '', label: 'Select a journal...' },
                                        ...journalOptions
                                    ]}
                                />
                            </div>
                        )}
                    </div>

                    <div className="buttons">
                        {selectedJournalId && (
                            <button onClick={handleMove} disabled={moving}>
                                {moving ? (
                                    <>
                                        <Icon name="progress_activity" spin={true} />
                                        Moving...
                                    </>
                                ) : (
                                    'Move Entry'
                                )}
                            </button>
                        )}
                        <button className="cancel" onClick={onClose} disabled={moving}>
                            Cancel
                        </button>
                    </div>
                </>
            )}
        </Modal>
    );
}
