import React, { useState, useEffect } from 'react';
import { useSession } from '@/context/session';
import { Journals } from '@/api/user/journals';
import Select from '@/components/forms/select';

/**
 * <summary>Select Journal Component</summary>
 * <description>
 * Reusable category + journal dropdowns that remember the last selected
 * category and journal using localStorage and notify parent via onChange.
 * </description>
 */
export default function SelectJournal({ onChange, journalId = null }) {
    const session = useSession();

    const [categories, setCategories] = useState([]);
    const [selectedCategoryId, setSelectedCategoryId] = useState('');
    const [journals, setJournals] = useState([]);
    const [selectedJournalId, setSelectedJournalId] = useState('');
    const [loading, setLoading] = useState(false);

    const CATEGORY_STORAGE_KEY = 'selectedJournalCategoryId';
    const JOURNAL_STORAGE_KEY = 'selectedJournalId';

    useEffect(() => {
        fetchCategories();
    }, []);

    useEffect(() => {
        if (journalId) {
            return;
        }

        if (!selectedCategoryId || categories.length === 0) {
            setJournals([]);
            setSelectedJournalId('');
            notifyChange('', '', loading);
            return;
        }

        const category = categories.find(c => c.id == selectedCategoryId);
        if (category && category.journals) {
            setJournals(category.journals);

            // Attempt to restore last selected journal from localStorage
            let storedJournalId = null;
            try {
                storedJournalId = window.localStorage.getItem(JOURNAL_STORAGE_KEY);
            } catch {
                storedJournalId = null;
            }

            let initialJournalId = '';
            if (storedJournalId && category.journals.some(j => j.id == storedJournalId)) {
                initialJournalId = storedJournalId;
            } else if (category.journals.length > 0) {
                initialJournalId = category.journals[0].id;
            }

            setSelectedJournalId(initialJournalId);
            notifyChange(selectedCategoryId, initialJournalId, loading);

            if (initialJournalId) {
                try {
                    window.localStorage.setItem(JOURNAL_STORAGE_KEY, String(initialJournalId));
                } catch {
                    // ignore storage errors
                }
            }
        } else {
            setJournals([]);
            setSelectedJournalId('');
            notifyChange(selectedCategoryId, '', loading);
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [selectedCategoryId, categories]);

    const notifyChange = (categoryId, journalId, isLoading) => {
        if (typeof onChange === 'function') {
            onChange({ categoryId, journalId, loading: isLoading });
        }
    };

    const fetchCategories = () => {
        setLoading(true);
        notifyChange(selectedCategoryId, selectedJournalId, true);
        Journals(session).getCategories().then(response => {
            if (response.data && response.data.success) {
                const data = response.data.data || [];
                setCategories(data);

                if (journalId && data.length > 0) {
                    // When a specific journalId is provided, preselect its category and journal
                    Journals(session).getJournal(journalId).then(journalResponse => {
                        if (journalResponse.data && journalResponse.data.success) {
                            const journal = journalResponse.data.data;
                            const categoryId = journal.categoryId;
                            const category = data.find(c => c.id == categoryId);

                            if (category && category.journals) {
                                setSelectedCategoryId(String(categoryId));
                                setJournals(category.journals);
                                setSelectedJournalId(String(journalId));
                                notifyChange(String(categoryId), String(journalId), false);
                            } else {
                                setSelectedCategoryId('');
                                setJournals([]);
                                setSelectedJournalId('');
                                notifyChange('', '', false);
                            }
                        }
                        setLoading(false);
                    }).catch(err => {
                        console.error('Error fetching journal for preselection:', err);
                        setLoading(false);
                        notifyChange(selectedCategoryId, selectedJournalId, false);
                    });
                } else if (data.length > 0) {
                    // Try to restore last selected category
                    let storedCategoryId = null;
                    try {
                        storedCategoryId = window.localStorage.getItem(CATEGORY_STORAGE_KEY);
                    } catch {
                        storedCategoryId = null;
                    }

                    let initialCategoryId = '';
                    if (storedCategoryId && data.some(c => c.id == storedCategoryId)) {
                        initialCategoryId = storedCategoryId;
                    } else {
                        initialCategoryId = data[0].id;
                    }

                    setSelectedCategoryId(initialCategoryId);

                    try {
                        window.localStorage.setItem(CATEGORY_STORAGE_KEY, String(initialCategoryId));
                    } catch {
                        // ignore storage errors
                    }
                } else if (!journalId) {
                    setSelectedCategoryId('');
                    setJournals([]);
                    setSelectedJournalId('');
                    notifyChange('', '', false);
                }
            }

            if (!journalId) {
                setLoading(false);
                notifyChange(selectedCategoryId, selectedJournalId, false);
            }
        }).catch(err => {
            console.error('Error fetching categories:', err);
            setLoading(false);
            notifyChange(selectedCategoryId, selectedJournalId, false);
        });
    };

    const handleCategoryChange = (e) => {
        const newCategoryId = e.target.value;
        setSelectedCategoryId(newCategoryId);
        try {
            window.localStorage.setItem(CATEGORY_STORAGE_KEY, String(newCategoryId));
        } catch {
            // ignore storage errors
        }
    };

    const handleJournalChange = (e) => {
        const newJournalId = e.target.value;
        setSelectedJournalId(newJournalId);
        notifyChange(selectedCategoryId, newJournalId, loading);
        try {
            window.localStorage.setItem(JOURNAL_STORAGE_KEY, String(newJournalId));
        } catch {
            // ignore storage errors
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
        <>
            <div className="col-2">
                <Select
                    label="Category"
                    name="category"
                    options={categoryOptions}
                    value={selectedCategoryId}
                    onChange={handleCategoryChange}
                />
            </div>
            <div className="col-2">
                {selectedCategoryId && (
                    <Select
                        label="Journal"
                        name="journal"
                        options={journalOptions}
                        value={selectedJournalId}
                        onChange={handleJournalChange}
                    />
                )}
            </div>
        </>
    );
}
