import { useState, useEffect, useRef } from 'react';
//components
import Checkbox from '@/components/forms/checkbox';
import Input from '@/components/forms/input';
import TextArea from '@/components/forms/textarea';
import Icon from '@/components/ui/icon';
import Modal from '@/components/ui/modal';
//context
import { useSession } from '@/context/session';
//api
import { JournalChecklists } from '@/api/user/journalChecklists';

export default function ChecklistModule({ module, onUpdate, isEditable = true, manuallyAdded = false, tabButtons }) {
    //state
    const [checklistId, setChecklistId] = useState(module.checklistId);
    const [mounted, setMounted] = useState(false);
    const [items, setItems] = useState([]);
    const [editingItemId, setEditingItemId] = useState(null);
    const [showSettingsModal, setShowSettingsModal] = useState(false);
    const [checklistTitle, setChecklistTitle] = useState('');
    const [checklistDescription, setChecklistDescription] = useState('');
    const [tempTitle, setTempTitle] = useState('');
    const [tempDescription, setTempDescription] = useState('');
    const debounceTimer = useRef(null);

    //refs
    const titleInputRef = useRef(null);
    const draggedItemRef = useRef(null);
    const dragOverItemRef = useRef(null);

    //context
    const session = useSession();
    const {
        getChecklist,
        addChecklist,
        addChecklistItem,
        updateChecklistItemTitle,
        updateChecklistItemStatus,
        updateChecklistTitle,
        updateChecklistDescription,
        resortChecklistItems
    } = JournalChecklists(session);

    //effect
    useEffect(() => {
        if (mounted) return;
        setMounted(true);
        if (checklistId) {
            getChecklist(checklistId).then(response => {
                if (response.data.success) {
                    const checklist = response.data.data;
                    setItems(checklist.items);
                    setChecklistTitle(checklist.title || '');
                    setChecklistDescription(checklist.description || '');
                }
            });
        }
        tabButtons([
            {
                icon: 'settings',
                title: 'Settings',
                callback: handleShowSettingsModal
            }
        ]);
    }, []);

    const handleAddChecklistItem = async () => {
        if (!isEditable) return;
        const isNew = !checklistId;
        var id = checklistId;
        if (!checklistId) {
            const response = await addChecklist({ EntryId: module.entryId, Title: '' });
            if (response.data.success) {
                id = response.data.data;
                setChecklistId(id);
                onUpdate({ ...module, checklistId: id });
            }
        }
        if (!id) {
            console.error('Error getting checklistId. Failed creating checklist.');
            return;
        }
        const response = await addChecklistItem({ CheckListId: id, Title: 'New task' });
        if (response.data.success) {
            const newItem = response.data.data;
            setItems([...items, newItem]);
            // Start editing the new item immediately
            setEditingItemId(newItem.id);
        }
    };

    const handleItemClick = (e, item) => {
        e.stopPropagation();
        e.preventDefault();
        setEditingItemId(item.id);
    };

    const handleItemChange = (item, checked) => {
        const newStatus = checked ? 1 : 0;
        updateChecklistItemStatus(item.id, newStatus).then(response => {
            if (response.data.success) {
                const updatedItems = items.map(i =>
                    i.id === item.id ? { ...i, status: newStatus } : i
                );
                setItems(updatedItems);
            }
        });
    };

    const handleItemTitleChange = (item, newTitle) => {
        // Clear any existing timer
        if (debounceTimer.current) {
            clearTimeout(debounceTimer.current);
        }

        // Create a new timer
        debounceTimer.current = setTimeout(() => handleUpdateChecklistItemTitle(item.id, newTitle), 1500); // 500ms debounce delay
    };

    const getSelectedItem = () => {
        return items.find(item => item.id === editingItemId);
    };

    const handleTitleKeyDown = (e) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            const item = getSelectedItem();
            const value = titleInputRef.current.value;
            handleUpdateChecklistItemTitle(item.id, value);
            setEditingItemId(null);
        } else if (e.key === 'Escape') {
            e.preventDefault();
            setEditingItemId(null);
        }
    };

    const handleUpdateChecklistItemTitle = (id, title) => {
        updateChecklistItemTitle(id, title).then(response => {
            if (response.data.success) {
                const updatedItems = items.map(i =>
                    i.id === id ? { ...i, title: title } : i
                );
                setItems(updatedItems);
            }
        });
    }

    const handleItemTitleChangeInput = (e, item) => {
        const updatedItems = items.map(i =>
            i.id === item.id ? { ...i, title: e.target.value } : i
        );
        setItems(updatedItems);
        handleItemTitleChange(item, e.target.value);
    }

    const handleInputBlur = () => {
        setEditingItemId(null);
    };

    //#region  Drag & Drop

    // Drag and Drop handlers
    const handleDragStart = (e, item, index) => {
        if (!isEditable) return;
        e.stopPropagation(); // Prevent module drag
        window.noDrag = true;
        draggedItemRef.current = { item, index };
        e.dataTransfer.effectAllowed = 'move';
        e.currentTarget.classList.add('dragging');
    };

    const handleDragOver = (e, item, index) => {
        if (!isEditable || !draggedItemRef.current) return;
        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';

        if (draggedItemRef.current.item.id !== item.id) {
            dragOverItemRef.current = { item, index };
            
            // Add visual indicator
            const rect = e.currentTarget.getBoundingClientRect();
            const mouseY = e.clientY;
            const elementMiddle = rect.top + (rect.height / 2);
            const isAbove = mouseY < elementMiddle;
            
            // Remove previous indicators
            document.querySelectorAll('.checklist-item.drag-over-top, .checklist-item.drag-over-bottom').forEach(el => {
                el.classList.remove('drag-over-top', 'drag-over-bottom');
            });
            
            // Add new indicator
            if (isAbove) {
                e.currentTarget.classList.add('drag-over-top');
            } else {
                e.currentTarget.classList.add('drag-over-bottom');
            }
        }
    };

    const handleDragLeave = (e) => {
        e.currentTarget.classList.remove('drag-over-top', 'drag-over-bottom');
    };

    const handleDrop = async (e) => {
        if (!isEditable || !draggedItemRef.current || !dragOverItemRef.current) return;
        e.preventDefault();

        const draggedIndex = draggedItemRef.current.index;
        const dropIndex = dragOverItemRef.current.index;

        if (draggedIndex === dropIndex) {
            handleDragEnd(e);
            return;
        }

        // Reorder items
        const newItems = [...items];
        const [draggedItem] = newItems.splice(draggedIndex, 1);
        newItems.splice(dropIndex, 0, draggedItem);

        // Assign sort values based on new order
        const itemsWithSort = newItems.map((item, index) => ({
            ...item,
            sort: index + 1
        }));

        setItems(itemsWithSort);

        // Only send items that changed their sort order
        const minIndex = Math.min(draggedIndex, dropIndex);
        const maxIndex = Math.max(draggedIndex, dropIndex);
        const affectedItems = itemsWithSort.slice(minIndex, maxIndex + 1);

        // Call API to update sort order for affected items only
        try {
            const sortData = affectedItems.map((item, i) => ({
                Id: item.id,
                Sort: item.sort
            }));
            await resortChecklistItems(sortData);
        } catch (err) {
            console.error('Error resorting checklist items:', err);
        }

        handleDragEnd(e);
    };

    const handleDragEnd = (e) => {
        if (e && e.currentTarget) {
            e.currentTarget.classList.remove('dragging');
        }
        
        // Remove all drag indicators
        document.querySelectorAll('.checklist-item.dragging, .checklist-item.drag-over-top, .checklist-item.drag-over-bottom').forEach(el => {
            el.classList.remove('dragging', 'drag-over-top', 'drag-over-bottom');
        });
        
        draggedItemRef.current = null;
        dragOverItemRef.current = null;
    };

    //#endregion

    //#region  Settings Modal

    const handleShowSettingsModal = () => {
        setTempTitle(checklistTitle);
        setTempDescription(checklistDescription);
        setShowSettingsModal(true);
    };

    const handleCloseSettingsModal = () => {
        setShowSettingsModal(false);
    };

    const handleSaveSettings = async () => {
        if (!checklistId) return;

        try {
            await updateChecklistTitle(checklistId, tempTitle);
            await updateChecklistDescription(checklistId, tempDescription);
            setChecklistTitle(tempTitle);
            setChecklistDescription(tempDescription);
            setShowSettingsModal(false);
        } catch (err) {
            console.error('Error saving checklist settings:', err);
        }
    };

    //#endregion

    return (
        <>
            {checklistTitle && <h4 className="checklist-title">{checklistTitle}</h4>}
            <div className="checklist">
                {items && items.map((item, index) => (
                    <div 
                        key={item.id || index} 
                        className="checklist-item no-drag"
                        draggable={isEditable}
                        onDragStart={(e) => handleDragStart(e, item, index)}
                        onDragOver={(e) => handleDragOver(e, item, index)}
                        onDragLeave={handleDragLeave}
                        onDrop={handleDrop}
                        onDragEnd={handleDragEnd}
                    >
                        <Checkbox
                            name={`checklist-item-${item.id}`}
                            id={`${module.id}_${item.id}`}
                            checked={item.status === 1}
                            onChange={(checked) => handleItemChange(item, checked)}
                            label={<>
                                {editingItemId === item.id ? (
                                    <Input
                                        ref={titleInputRef}
                                        name={`checklist-item-input-${item.id}`}
                                        value={item.title}
                                        onChange={(e) => handleItemTitleChangeInput(e, item)}
                                        onBlur={handleInputBlur}
                                        autoFocus
                                        onKeyDown={handleTitleKeyDown}
                                    />
                                ) : (
                                    <span
                                        className="checklist-item-label"
                                        onClick={(e) => handleItemClick(e, item)}
                                        style={{ cursor: 'pointer' }}
                                    >
                                        {item.title !== '' ? item.title : <>&nbsp;</>}
                                    </span>
                                )}
                            </>}
                        />
                    </div>
                ))}
                {isEditable && (<div className="tool-bar">  
                    <div className="left-side">
                        <button onClick={handleAddChecklistItem}><Icon name="add" />Add Task</button>
                    </div>
                </div>)}
            </div>
            {showSettingsModal && (
                <Modal title="Checklist Settings" onClose={handleCloseSettingsModal}>
                    <Input
                        label="Title"
                        name="checklist-title"
                        value={tempTitle}
                        onChange={(e) => setTempTitle(e.target.value)}
                        placeholder="Enter checklist title"
                    />
                    <TextArea
                        label="Description"
                        name="checklist-description"
                        defaultValue={tempDescription}
                        onInput={(e) => setTempDescription(e.target.value)}
                        placeholder="Enter checklist description"
                        rows={3}
                        autoResize={true}
                    />
                    <div className="buttons">
                        <button onClick={handleSaveSettings}>Save</button>
                        <button className="cancel" onClick={handleCloseSettingsModal}>Cancel</button>
                    </div>
                </Modal>
            )}
        </>
    );
}