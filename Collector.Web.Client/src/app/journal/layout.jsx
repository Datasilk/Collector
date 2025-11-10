import React, { useState, useEffect, useRef } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useSession } from '@/context/session';
import { Journals } from '@/api/user/journals';
import Navigation from '@/components/navigation/navigation';
import TreeView from '@/components/ui/tree-view';
import Modal from '@/components/ui/modal';
import Icon from '@/components/ui/icon';
import Input from '@/components/forms/input';
import entryTypes from './entry-types';
import './layout.css';

/**
 * <summary>Journal Layout</summary>
 * <description>Provides the layout wrapper for all journal pages, including navigation and sidebar with TreeView.</description>
 */
export default function JournalLayout({ children }) {
    // context
    const session = useSession();
    const navigate = useNavigate();
    const { journalId } = useParams();

    // state
    const [isAuth, setAuth] = useState(null);
    const [mounted, setMounted] = useState(false);
    const [categories, setCategories] = useState([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [editingCategoryId, setEditingCategoryId] = useState(null);
    const [editingCategoryTitle, setEditingCategoryTitle] = useState('');
    const [editingJournalId, setEditingJournalId] = useState(null);
    const [editingJournalTitle, setEditingJournalTitle] = useState('');
    const [categoryToggles, setCategoryToggles] = useState({});
    const storageKey = 'collector:journal:categories';
    const selectedJournalKey = 'collector:journal:selected';

    // refs for focusing inputs
    const categoryTitleInputRef = useRef(null);
    const journalTitleInputRef = useRef(null);

    // Category modal state
    const [showCategoryModal, setShowCategoryModal] = useState(false);
    const [newCategoryName, setNewCategoryName] = useState('');
    const [newCategoryColor, setNewCategoryColor] = useState('#3498db');

    // Journal modal state
    const [showJournalModal, setShowJournalModal] = useState(false);
    const [newJournalName, setNewJournalName] = useState('');
    const [newJournalColor, setNewJournalColor] = useState('#3498db');
    const [selectedCategoryId, setSelectedCategoryId] = useState(null);

    // Entry type dropdown state
    const [showEntryTypeDropdown, setShowEntryTypeDropdown] = useState(false);
    const [activeEntryTypeModal, setActiveEntryTypeModal] = useState(null);
    const entryTypeDropdownRef = useRef(null);

    // api
    const { logout, hasRole } = session;

    // effects
    useEffect(() => {
        if (!mounted) {
            setMounted(true);
            // check if user is allowed to view the journal section
            if (!hasRole('user') && !hasRole('admin')) {
                navigate('/login');
            } else {
                setAuth(true);
                fetchCategories();
                loadToggleStates();

                // If no journalId is provided in URL, check if we have a saved journal
                if (!journalId) {
                    const savedJournalId = localStorage.getItem(selectedJournalKey);
                    if (savedJournalId) {
                        navigate(`/journal/${savedJournalId}`);
                    }
                }
            }
        }
    }, [mounted, journalId]);

    // Load toggle states from local storage
    const loadToggleStates = () => {
        try {
            const savedToggles = JSON.parse(localStorage.getItem(storageKey)) || {};
            const catToggles = {};
            const jrnToggles = {};

            // Separate category and journal toggles
            Object.keys(savedToggles).forEach(key => {
                if (key.startsWith('category')) {
                    catToggles[key] = savedToggles[key];
                } else if (key.startsWith('journal')) {
                    jrnToggles[key] = savedToggles[key];
                }
            });

            setCategoryToggles(catToggles);
        } catch (error) {
            console.error('Error loading toggle states from localStorage:', error);
        }
    };

    // Save toggle state to local storage
    const saveToggleState = (id) => {
        try {
            const fullId = `category${id}`;
            const currentState = categoryToggles[fullId] || false;
            const savedToggles = JSON.parse(localStorage.getItem(storageKey)) || {};
            savedToggles[fullId] = !currentState;
            localStorage.setItem(storageKey, JSON.stringify(savedToggles));
            setCategoryToggles(prev => ({ ...prev, [fullId]: !currentState }));
        } catch (error) {
            console.error('Error saving toggle state to localStorage:', error);
        }
    };

    // Save selected journal to local storage
    const saveSelectedJournal = (id) => {
        try {
            localStorage.setItem(selectedJournalKey, id);
        } catch (error) {
            console.error('Error saving selected journal to localStorage:', error);
        }
    };

    // Focus category title input when edit mode is activated
    useEffect(() => {
        if (editingCategoryId !== null && categoryTitleInputRef.current) {
            categoryTitleInputRef.current.focus();
        }
    }, [editingCategoryId]);

    // Focus journal title input when edit mode is activated
    useEffect(() => {
        if (editingJournalId !== null && journalTitleInputRef.current) {
            journalTitleInputRef.current.focus();
        }
    }, [editingJournalId]);

    // Fetch categories with journals
    const fetchCategories = () => {
        setLoading(true);
        setError(null);
        Journals(session).getCategories().then(response => {
            if (response.data && response.data.success) {
                setCategories(response.data.data);
            } else {
                setError(response.data?.message || 'Failed to load categories');
            }
        }).catch(err => {
            setError('Error loading journal categories: ' + (err.message || 'Unknown error'));
            console.error('Error fetching categories:', err);
        }).finally(() => {
            setLoading(false);
        });
    };

    // actions
    const handleToggleSidebar = () => {
        const target = document.querySelector('.journal-layout > .sidebar');
        const showing = target.style.display == 'block';
        target.style.display = showing ? '' : 'block';
        if (!showing) {
            document.addEventListener('mousedown', handleMouseDownSidebar);
        } else {
            document.removeEventListener('mousedown', handleMouseDownSidebar);
        }
    }

    const handleMouseDownSidebar = (e) => {
        let target = e.target;
        while (target) {
            const classes = target.classList;
            if (classes?.contains('journal-layout')) {
                document.removeEventListener('mousedown', handleMouseDownSidebar);
                const target = document.querySelector('.journal-layout > .sidebar');
                target.style.display = '';
                return;
            };
            if (classes?.contains('sidebar-mobile-toggle') || classes?.contains('sidebar')) return;
            target = target.parentNode;
        }
    }

    const handleNewCategory = () => {
        setNewCategoryName('');
        setShowCategoryModal(true);
    }

    const handleCloseModal = () => {
        setShowCategoryModal(false);
    }

    const handleCreateCategory = () => {
        if (!newCategoryName.trim()) {
            return; // Don't create empty categories
        }

        setLoading(true);
        const category = {
            title: newCategoryName.trim(),
            color: newCategoryColor.replace('#', '') // Remove # if present
        };

        Journals(session).addCategory(category).then(response => {
            if (response.data && response.data.success) {
                // Close modal and refresh categories
                setShowCategoryModal(false);
                fetchCategories();
            } else {
                setError(response.data?.message || 'Failed to create category');
            }
            setLoading(false);
        });
    }

    const handleCategoryClick = (categoryId) => {
        // Find the category to edit
        const categoryToEdit = categories.find(cat => cat.id === categoryId);
        if (categoryToEdit) {
            setEditingCategoryId(categoryId);
            setEditingCategoryTitle(categoryToEdit.title);
        }
    }

    const handleCategoryTitleChange = (e) => {
        setEditingCategoryTitle(e.target.value);
    };

    const handleCategoryTitleKeyDown = (e, categoryId) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            updateCategoryTitle(categoryId);
        } else if (e.key === 'Escape') {
            e.preventDefault();
            setEditingCategoryId(null);
            setEditingCategoryTitle('');
        }
    };

    const handleCategoryTitleBlur = (categoryId) => {
        updateCategoryTitle(categoryId);
    };

    const updateCategoryTitle = (categoryId) => {
        if (editingCategoryTitle.trim() === '') return;

        // Find the current category to compare titles
        const currentCategory = categories.find(cat => cat.id === categoryId);
        if (!currentCategory || currentCategory.title === editingCategoryTitle.trim()) {
            // Title hasn't changed, just exit edit mode
            setEditingCategoryId(null);
            return;
        }

        setLoading(true);
        Journals(session).renameCategory(categoryId, editingCategoryTitle.trim())
            .then(response => {
                if (response.data && response.data.success) {
                    // Update the category in the state
                    const updatedCategories = categories.map(cat => {
                        if (cat.id === categoryId) {
                            return { ...cat, title: editingCategoryTitle.trim() };
                        }
                        return cat;
                    });
                    setCategories(updatedCategories);
                } else {
                    setError(response.data?.message || 'Failed to update category title');
                }
                setEditingCategoryId(null);
                setLoading(false);
            })
            .catch(err => {
                console.error('Error updating category title:', err);
                setError('Failed to update category title');
                setEditingCategoryId(null);
                setLoading(false);
            });
    };

    const handleNewJournal = (categoryId) => {
        setNewJournalName('');
        setSelectedCategoryId(categoryId);
        setShowJournalModal(true);
    }

    const handleCloseJournalModal = () => {
        setShowJournalModal(false);
    }

    const handleEditJournalTitle = (journalId, currentTitle) => {
        setEditingJournalId(journalId);
        setEditingJournalTitle(currentTitle);
    };

    const handleJournalTitleChange = (e) => {
        setEditingJournalTitle(e.target.value);
    };

    const handleJournalTitleKeyDown = (e, journalId) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            updateJournalTitle(journalId);
        } else if (e.key === 'Escape') {
            e.preventDefault();
            setEditingJournalId(null);
            setEditingJournalTitle('');
        }
    };

    const handleJournalTitleBlur = (journalId) => {
        updateJournalTitle(journalId);
    };

    const updateJournalTitle = (journalId) => {
        if (editingJournalTitle.trim() === '') return;

        // Find the current journal to compare titles
        let currentJournal = null;
        for (const category of categories) {
            if (category.journals) {
                const found = category.journals.find(j => j.id === journalId);
                if (found) {
                    currentJournal = found;
                    break;
                }
            }
        }

        if (!currentJournal || currentJournal.title === editingJournalTitle.trim()) {
            // Title hasn't changed, just exit edit mode
            setEditingJournalId(null);
            return;
        }

        setLoading(true);
        Journals(session).renameJournal(journalId, editingJournalTitle.trim())
            .then(response => {
                if (response.data && response.data.success) {
                    // Update the journal in the state
                    const updatedCategories = categories.map(cat => {
                        if (cat.journals) {
                            cat.journals = cat.journals.map(journal => {
                                if (journal.id === journalId) {
                                    return { ...journal, title: editingJournalTitle.trim() };
                                }
                                return journal;
                            });
                        }
                        return cat;
                    });
                    setCategories(updatedCategories);
                } else {
                    setError(response.data?.message || 'Failed to update journal title');
                }
                setEditingJournalId(null);
                setLoading(false);
            })
            .catch(err => {
                console.error('Error updating journal title:', err);
                setError('Failed to update journal title');
                setEditingJournalId(null);
                setLoading(false);
            });
    };

    const handleCreateJournal = () => {
        if (!newJournalName.trim()) {
            return; // Don't create empty journals
        }

        setLoading(true);
        const journal = {
            title: newJournalName.trim(),
            color: newJournalColor.replace('#', ''), // Remove # if present
            categoryId: selectedCategoryId
        };

        Journals(session).addJournal(journal).then(response => {
            if (response.data && response.data.success) {
                // Close modal and navigate to the new journal
                setShowJournalModal(false);
                fetchCategories();

                // Navigate to the new journal
                if (response.data.data && response.data.data.id) {
                    navigate(`/journal/${response.data.data.id}`);
                }
            } else {
                setError(response.data?.message || 'Failed to create journal');
            }
            setLoading(false);
        });
    }

    const handleToggleEntryTypeDropdown = () => {
        setShowEntryTypeDropdown(!showEntryTypeDropdown);
    };

    const handleSelectEntryType = (entryType) => {
        setShowEntryTypeDropdown(false);
        setActiveEntryTypeModal(entryType);
    };

    const handleCloseEntryTypeModal = () => {
        setActiveEntryTypeModal(null);
    };

    // Close dropdown when clicking outside
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
        <div className="app-container">
            <header className="main-header">
                <Navigation />
            </header>
            {isAuth ?
                <main className={"journal-layout has-sidebar" + (session.hasRole('admin') ? ' is-admin' : ' is-user')}>
                    <div className="sidebar-mobile-toggle" onClick={handleToggleSidebar}>
                        <Icon name="menu"></Icon>
                    </div>
                    <div className="sidebar">
                        <div className="sidebar-top">
                            {!loading && !error && categories.length > 0 &&
                                <div className="tool-bar entry-type-dropdown" ref={entryTypeDropdownRef}>
                                    <button className="button" onClick={handleToggleEntryTypeDropdown}>
                                        <Icon name="add"></Icon>New Entry
                                    </button>
                                    {showEntryTypeDropdown && (
                                        <div className="dropdown-menu">
                                            {entryTypes.map(entryType => (
                                                <div
                                                    key={entryType.id}
                                                    className="dropdown-item"
                                                    onClick={() => handleSelectEntryType(entryType)}
                                                >
                                                    <Icon name={entryType.icon}></Icon>
                                                    <span>{entryType.name}</span>
                                                </div>
                                            ))}
                                        </div>
                                    )}
                                </div>
                            }

                            <div className="journal-tree">
                                {loading ? (
                                    <div className="loading">Loading categories...</div>
                                ) : error ? (
                                    <div className="error">{error}</div>
                                ) : categories.length === 0 ? (
                                    <div className="empty">No journal categories found</div>
                                ) : (
                                    categories.map(category => (
                                        <TreeView
                                            key={`category-${category.id}`}
                                            defaultOpen={categoryToggles[`category${category.id}`] !== undefined ? categoryToggles[`category${category.id}`] : false}
                                            onClick={() => saveToggleState(category.id)}
                                            label={editingCategoryId === category.id ?
                                                <Input
                                                    name={`category-${category.id}`}
                                                    value={editingCategoryTitle}
                                                    onInput={handleCategoryTitleChange}
                                                    onKeyDown={(e) => handleCategoryTitleKeyDown(e, category.id)}
                                                    onBlur={() => handleCategoryTitleBlur(category.id)}
                                                    ref={categoryTitleInputRef}
                                                /> :
                                                category.title
                                            }
                                            className="treeview-category"
                                            style={{
                                                background: category.color ? `linear-gradient(90deg, ${category.color} 0%, transparent 12%)` : 'none'
                                            }}
                                            menu={<span className="tool-bar">
                                                <button className="icon edit-icon"
                                                    onClick={() => handleCategoryClick(category.id)}>
                                                    <Icon name="edit"></Icon>
                                                </button>
                                            </span>}
                                        >
                                            {category.journals && category.journals.length > 0 && (
                                                category.journals.map(journal => (
                                                    <div
                                                        key={`journal-${journal.id}`}
                                                        className={"treeview-journal" + (journalId == journal.id ? ' selected' : '')}
                                                    >
                                                        <div className="tree-view-header">
                                                            <div className="tree-view-toggle">
                                                                <span className="tree-view-spacer"></span>
                                                            </div>
                                                            <div
                                                                className="tree-view-label"
                                                                onClick={() => {
                                                                    if (document.body.classList.contains('is-mobile')) {
                                                                        document.querySelector('.sidebar').style.display = 'none';
                                                                    }
                                                                    saveSelectedJournal(journal.id);
                                                                }}
                                                            >
                                                                {editingJournalId === journal.id ?
                                                                    <Input
                                                                        name={`journal-${journal.id}`}
                                                                        value={editingJournalTitle}
                                                                        onInput={handleJournalTitleChange}
                                                                        onKeyDown={(e) => handleJournalTitleKeyDown(e, journal.id)}
                                                                        onBlur={() => handleJournalTitleBlur(journal.id)}
                                                                        ref={journalTitleInputRef}
                                                                    /> :
                                                                    <Link to={`/journal/${journal.id}`} className="tree-view-link">
                                                                        <span>{journal.title}</span>
                                                                    </Link>
                                                                }
                                                            </div>
                                                            <div className="tree-view-menu">
                                                                <span className="tool-bar">
                                                                    <button className="icon edit-icon"
                                                                        onClick={(e) => {
                                                                            e.stopPropagation();
                                                                            handleEditJournalTitle(journal.id, journal.title);
                                                                        }}>
                                                                        <Icon name="edit"></Icon>
                                                                    </button>
                                                                </span>
                                                            </div>
                                                        </div>
                                                    </div>
                                                ))
                                            )}
                                            <div className="tool-bar empty-journals">
                                                <Link className="button add-journal" onClick={() => handleNewJournal(category.id)}>
                                                    <Icon name="add"></Icon>New Journal
                                                </Link>
                                            </div>
                                        </TreeView>
                                    ))
                                )}

                                <div className="tool-bar">
                                    <Link className="button add-category" onClick={handleNewCategory}>
                                        <Icon name="add"></Icon>New Category
                                    </Link>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div className="body">
                        {children}
                    </div>
                </main>
                :
                <main className="main-content"></main>
            }

            {/* Category Creation Modal */}
            {showCategoryModal && (
                <Modal title="Create New Category" onClose={handleCloseModal}>
                    <div className="modal-form">
                        <div className="form-group">
                            <label htmlFor="categoryName">Category Name</label>
                            <Input
                                name="categoryName"
                                value={newCategoryName}
                                onInput={(e) => setNewCategoryName(e.target.value)}
                                placeholder="Enter category name"
                                autoFocus
                            />
                        </div>
                        <div className="form-group">
                            <label htmlFor="categoryColor">Color</label>
                            <Input
                                name="categoryColor"
                                value={newCategoryColor}
                                onInput={(e) => setNewCategoryColor(e.target.value)}
                            />
                        </div>
                        <div className="form-actions">
                            <button className="button secondary" onClick={handleCloseModal}>Cancel</button>
                            <button className="button primary" onClick={handleCreateCategory} disabled={!newCategoryName.trim()}>Create</button>
                        </div>
                    </div>
                </Modal>
            )}

            {/* Journal Creation Modal */}
            {showJournalModal && (
                <Modal title="Create New Journal" onClose={handleCloseJournalModal}>
                    <div className="modal-form">
                        <div className="form-group">
                            <label htmlFor="journalName">Journal Name</label>
                            <Input
                                name="journalName"
                                value={newJournalName}
                                onInput={(e) => setNewJournalName(e.target.value)}
                                placeholder="Enter journal name"
                                autoFocus
                            />
                        </div>
                        <div className="form-group">
                            <label htmlFor="journalColor">Color</label>
                            <Input
                                name="journalColor"
                                value={newJournalColor}
                                onInput={(e) => setNewJournalColor(e.target.value)}
                            />
                        </div>
                        <div className="form-actions">
                            <button className="button secondary" onClick={handleCloseJournalModal}>Cancel</button>
                            <button className="button primary" onClick={handleCreateJournal} disabled={!newJournalName.trim()}>Create</button>
                        </div>
                    </div>
                </Modal>
            )}

            {/* Entry Type Modal */}
            {activeEntryTypeModal && (
                <activeEntryTypeModal.modal onClose={handleCloseEntryTypeModal} />
            )}
        </div>
    );
}
