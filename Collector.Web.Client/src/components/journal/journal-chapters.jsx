import React, { useState, useEffect, useRef } from 'react';
import Input from '@/components/forms/input';
import TextArea from '@/components/forms/textarea';
import Icon from '@/components/ui/icon';
import { useSession } from '@/context/session';
import { Journals } from '@/api/user/journals';

/**
 * <summary>Journal Chapters Management Component</summary>
 * <description>Reusable component for managing journal chapters with add, edit, delete, and drag-to-reorder functionality</description>
 */
export default function JournalChapters({ journalId, onChaptersChanged }) {
    const session = useSession();
    const [chapters, setChapters] = useState([]);
    const [entryCounts, setEntryCounts] = useState({});
    const [draggedIndex, setDraggedIndex] = useState(null);
    const [dragOverIndex, setDragOverIndex] = useState(null);
    const [showAddChapterForm, setShowAddChapterForm] = useState(false);
    const [newChapterTitle, setNewChapterTitle] = useState('');
    const [newChapterDescription, setNewChapterDescription] = useState('');
    const [editingChapterId, setEditingChapterId] = useState(null);
    const dragItem = useRef(null);
    const dragOverItem = useRef(null);

    useEffect(() => {
        if (journalId) {
            loadChapters();
        }
    }, [journalId]);

    const loadChapters = async () => {
        if (!journalId) return;

        try {
            const api = Journals(session);
            const response = await api.getChapters(journalId);
            
            if (response.data?.success && response.data.data) {
                setChapters(response.data.data);
                // TODO: Load entry counts for each chapter
                // For now, set all to 0
                const counts = {};
                response.data.data.forEach(chapter => {
                    counts[chapter.chapterId] = 0;
                });
                setEntryCounts(counts);
                
                if (onChaptersChanged) {
                    onChaptersChanged(response.data.data);
                }
            }
        } catch (err) {
            console.error('Error loading chapters:', err);
        }
    };

    const handleShowAddChapterForm = () => {
        setNewChapterTitle('');
        setNewChapterDescription('');
        setShowAddChapterForm(true);
    };

    const handleCancelAddChapter = () => {
        setShowAddChapterForm(false);
        setNewChapterTitle('');
        setNewChapterDescription('');
    };

    const handleCreateChapter = async () => {
        if (!journalId || !newChapterTitle.trim()) return;

        try {
            const api = Journals(session);
            const newChapter = {
                Title: newChapterTitle.trim(),
                Icon: 0,
                Color: 0,
                Description: newChapterDescription.trim()
            };
            
            const response = await api.addChapter(journalId, newChapter);
            if (response.data?.success) {
                await loadChapters();
                setShowAddChapterForm(false);
                setNewChapterTitle('');
                setNewChapterDescription('');
            }
        } catch (err) {
            console.error('Error adding chapter:', err);
        }
    };

    const handleDragStart = (index) => {
        dragItem.current = index;
        setDraggedIndex(index);
    };

    const handleDragEnter = (index) => {
        dragOverItem.current = index;
        setDragOverIndex(index);
    };

    const handleDragEnd = async () => {
        if (dragItem.current === null || dragOverItem.current === null) {
            setDraggedIndex(null);
            setDragOverIndex(null);
            return;
        }

        const draggedItemIndex = dragItem.current;
        const draggedOverItemIndex = dragOverItem.current;

        if (draggedItemIndex === draggedOverItemIndex) {
            setDraggedIndex(null);
            setDragOverIndex(null);
            return;
        }

        // Reorder chapters
        const newChapters = [...chapters];
        const draggedItem = newChapters[draggedItemIndex];
        newChapters.splice(draggedItemIndex, 1);
        newChapters.splice(draggedOverItemIndex, 0, draggedItem);

        // Update sort values
        const updatedChapters = newChapters.map((chapter, index) => ({
            ...chapter,
            sort: index + 1
        }));

        setChapters(updatedChapters);

        // Save sort order to API
        try {
            const api = Journals(session);
            for (const chapter of updatedChapters) {
                await api.updateChapterSort(journalId, chapter.chapterId, chapter.sort);
            }
            
            if (onChaptersChanged) {
                onChaptersChanged(updatedChapters);
            }
        } catch (err) {
            console.error('Error updating chapter sort:', err);
        }

        dragItem.current = null;
        dragOverItem.current = null;
        setDraggedIndex(null);
        setDragOverIndex(null);
    };

    const handleRenameChapter = async (chapterId, newTitle) => {
        try {
            const api = Journals(session);
            await api.renameChapter(journalId, chapterId, newTitle);
            
            // Update local state
            const updatedChapters = chapters.map(ch => 
                ch.chapterId === chapterId ? { ...ch, title: newTitle } : ch
            );
            setChapters(updatedChapters);
            setEditingChapterId(null);
            
            if (onChaptersChanged) {
                onChaptersChanged(updatedChapters);
            }
        } catch (err) {
            console.error('Error renaming chapter:', err);
        }
    };

    const handleChapterTitleClick = (chapterId) => {
        setEditingChapterId(chapterId);
    };

    const handleChapterTitleKeyDown = (e, chapterId, title) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            handleRenameChapter(chapterId, title);
        } else if (e.key === 'Escape') {
            setEditingChapterId(null);
            // Revert to original title
            const originalChapter = chapters.find(ch => ch.chapterId === chapterId);
            if (originalChapter) {
                setChapters(chapters.map(ch => 
                    ch.chapterId === chapterId ? originalChapter : ch
                ));
            }
        }
    };

    const handleDeleteChapter = async (chapterId) => {
        if (!confirm('Are you sure you want to delete this chapter?')) return;

        try {
            const api = Journals(session);
            await api.deleteChapter(journalId, chapterId);
            await loadChapters();
        } catch (err) {
            console.error('Error deleting chapter:', err);
        }
    };

    return (
        <div className="journal-chapters-component">
            {!showAddChapterForm ? (
                <>
                    <div className="filters tool-bar">
                        <button onClick={handleShowAddChapterForm}>
                            <Icon name="add" /> Add Chapter
                        </button>
                    </div>
                    <table className="spreadsheet">
                        <thead>
                            <tr>
                                <th>#</th>
                                <th>Title</th>
                                <th>Entries</th>
                                <th></th>
                            </tr>
                        </thead>
                        <tbody>
                            {chapters.map((chapter, index) => {
                                const isDragging = draggedIndex === index;
                                const isDragOver = dragOverIndex === index && draggedIndex !== null && draggedIndex !== index;
                                const dragPosition = isDragOver && draggedIndex !== null && draggedIndex < index ? 'drag-over-bottom' : isDragOver ? 'drag-over-top' : '';
                                
                                return (
                                <tr
                                    key={'journal-' + journalId + 'chapter-' + chapter.chapterId}
                                    draggable
                                    onDragStart={() => handleDragStart(index)}
                                    onDragEnter={() => handleDragEnter(index)}
                                    onDragEnd={handleDragEnd}
                                    onDragOver={(e) => e.preventDefault()}
                                    className={`${isDragging ? 'dragging' : ''} ${dragPosition}`}
                                >
                                    <td>{chapter.sort}</td>
                                    <td onClick={() => handleChapterTitleClick(chapter.chapterId)}>
                                        {editingChapterId === chapter.chapterId ? (
                                            <Input
                                                type="text"
                                                value={chapter.title}
                                                onChange={(e) => {
                                                    const newTitle = e.target.value;
                                                    setChapters(chapters.map(ch => 
                                                        ch.chapterId === chapter.chapterId 
                                                            ? { ...ch, title: newTitle } 
                                                            : ch
                                                    ));
                                                }}
                                                onBlur={(e) => handleRenameChapter(chapter.chapterId, e.target.value)}
                                                onKeyDown={(e) => handleChapterTitleKeyDown(e, chapter.chapterId, chapter.title)}
                                                autoFocus
                                            />
                                        ) : (
                                            <span style={{ cursor: 'pointer' }}>{chapter.title}</span>
                                        )}
                                    </td>
                                    <td>{entryCounts[chapter.chapterId] || 0}</td>
                                    <td className="buttons">
                                        <div className="tool-bar align-right">
                                            <button 
                                                className="icon"
                                                onClick={() => handleDeleteChapter(chapter.chapterId)}
                                                title="Delete chapter"
                                            >
                                                <Icon name="delete" />
                                            </button>
                                        </div>
                                    </td>
                                </tr>
                                );
                            })}
                        </tbody>
                    </table>
                </>
            ) : (
                <div className="add-chapter-form">
                    <Input
                        name="chapter-title"
                        label="Chapter Title"
                        type="text"
                        value={newChapterTitle}
                        onChange={(e) => setNewChapterTitle(e.target.value)}
                        placeholder="Enter chapter title"
                        required
                    />
                    <TextArea
                        name="chapter-description"
                        label="Description"
                        defaultValue={newChapterDescription}
                        onInput={(e) => setNewChapterDescription(e.target.value)}
                        placeholder="Enter chapter description (optional)"
                        rows={3}
                        autoResize={true}
                    />
                    <div className="buttons">
                        <button onClick={handleCancelAddChapter} className="cancel">
                            Cancel
                        </button>
                        <button 
                            onClick={handleCreateChapter}
                            disabled={!newChapterTitle.trim()}
                        >
                            <Icon name="add" /> Create Chapter
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
}
