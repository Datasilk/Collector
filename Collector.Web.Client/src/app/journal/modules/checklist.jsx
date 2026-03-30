import { useState, useEffect, useRef } from 'react';
//components
import Checkbox from '@/components/forms/checkbox';
import Input from '@/components/forms/input';
import TextArea from '@/components/forms/textarea';
import Icon from '@/components/ui/icon';
import ChecklistSettingsModal from './checklist-settings-modal';
//context
import { useSession } from '@/context/session';
//api
import { JournalChecklists } from '@/api/user/journalChecklists';

export default function ChecklistModule({ module, entryId, onUpdate, isEditable = true, manuallyAdded = false, tabButtons, fromSnapshotId = null }) {
    //state
    const [checklistId, setChecklistId] = useState(module.checklistId);
    const [checklistEntryId, setChecklistEntryId] = useState(null);
    const [mounted, setMounted] = useState(false);
    const [items, setItems] = useState(null);
    const [editingItemId, setEditingItemId] = useState(null);
    const [checklistTitle, setChecklistTitle] = useState('');
    const [checklistDescription, setChecklistDescription] = useState('');

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
        updateChecklistEntryId,
        resortChecklistItems
    } = JournalChecklists(session);

    //effect
    useEffect(() => {
        if (mounted) return;
        setMounted(true);

        if (items == null) {
            if (checklistId) {
                // If viewing from snapshot, load from module.checklist instead of API
                if (fromSnapshotId && module.checklist) {
                    setItems(module.checklist.items || []);
                    setChecklistTitle(module.checklist.title || '');
                    setChecklistDescription(module.checklist.description || '');
                } else {
                    // Load from API for current/live entries
                    getChecklist(checklistId).then(response => {
                        if (response.data.success) {
                            const checklist = response.data.data;
                            setItems(checklist.items);
                            setChecklistEntryId(checklist.entryId);
                            setChecklistTitle(checklist.title || '');
                            setChecklistDescription(checklist.description || '');
                            // Update module with loaded checklist data
                            if(entryId && module.checklist == null){
                                onUpdate({ ...module, checklistId: checklistId, checklist: { items: checklist.items, title: checklist.title || '', description: checklist.description || '' } });
                            }
                            if (entryId && checklist.entryId != entryId) {
                                updateChecklistEntryId(checklistId, entryId);
                            }
                        }
                    });
                }
            } else if (module.checklist) {
                // For pinned modules on journal details, hydrate directly from module JSON
                setItems(module.checklist.items || []);
                setChecklistTitle(module.checklist.title || '');
                setChecklistDescription(module.checklist.description || '');
            }
        }

        tabButtons([
            {
                icon: 'settings',
                title: 'Checklist Settings',
                callback: handleShowSettingsModal
            }
        ]);
    }, []);

    useEffect(() => {
        if (!manuallyAdded) return;
        if (checklistId) return;
        if (!entryId) return;

        const createChecklist = async () => {
            const response = await addChecklist({ EntryId: entryId, Title: '' });
            if (response.data && response.data.success) {
                const id = response.data.data;
                setChecklistId(id);
                setChecklistEntryId(entryId);
                onUpdate({ ...module, checklistId: id });
            }
        };

        createChecklist();
    }, [manuallyAdded, checklistId, entryId, module]);

    const handleAddChecklistItem = async () => {
        if (!isEditable) return;
        const isNew = !checklistId;
        var id = checklistId;
        if (!checklistId) {
            const response = await addChecklist({ EntryId: entryId, Title: '' });
            if (response.data.success) {
                id = response.data.data;
                setChecklistId(id);
                onUpdate({ ...module, checklistId: id });
            }
        }
        //if entryId is null, save entryId to checklist
        if (!checklistEntryId && id) {
            const response = await updateChecklistEntryId(id, entryId);
            if (response.data.success) {
                setChecklistEntryId(entryId);
            }
        }
        if (!id) {
            console.error('Error getting checklistId. Failed creating checklist.');
            return;
        }
        const response = await addChecklistItem({ CheckListId: id, Title: 'New task' });
        if (response.data.success) {
            const newItem = response.data.data;
            const updatedItems = [...(items || []), newItem];
            setItems(updatedItems);
            // Update module with new checklist data
            onUpdate({ ...module, checklistId: id, checklist: { items: updatedItems, title: checklistTitle, description: checklistDescription } });
            // Start editing the new item immediately
            setEditingItemId(newItem.id);
        }
    };

    const handleItemClick = (e, item) => {
        if (!isEditable) return;
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
                // Update module with new checklist data
                onUpdate({ ...module, checklist: { items: updatedItems, title: checklistTitle, description: checklistDescription } });
            }
        });
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
                // Update module with new checklist data
                onUpdate({ ...module, checklist: { items: updatedItems, title: checklistTitle, description: checklistDescription } });
            }
        });
    }

    const handleItemTitleChangeInput = (e, item) => {
        const updatedItems = items.map(i =>
            i.id === item.id ? { ...i, title: e.target.value } : i
        );
        setItems(updatedItems);
    }

    const handleInputBlur = () => {
        const item = getSelectedItem();
        if (item) {
            handleUpdateChecklistItemTitle(item.id, item.title);
        }
        setEditingItemId(null);
    };

    //#region  Drag & Drop

    // Drag and Drop handlers
    const handleDragStart = (e, item, index) => {
        if (!isEditable) return;
        if(window.noDragChecklistItem == true) {
            e.stopPropagation();
            e.preventDefault();
            return;
        };
        window.noDragChecklistItem = false;
        e.stopPropagation();
        draggedItemRef.current = { item, index };
        e.dataTransfer.effectAllowed = 'move';
        e.currentTarget.classList.add('dragging');
    };

    const handleDragOver = (e, item, index) => {
        if (!isEditable || !draggedItemRef.current || window.noDragChecklistItem) return;
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
        // Update module with new checklist data
        onUpdate({ ...module, checklist: { items: itemsWithSort, title: checklistTitle, description: checklistDescription } });

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
        window.noDragChecklistItem = false;
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

    const cancelDragEvent = (e) => {
        window.noDragChecklistItem = true;
        e.target.setAttribute('draggable', false);
        e.stopPropagation();
        e.preventDefault();
        return;
    };

    const resetDragEvent = (e) => {
        window.noDragChecklistItem = false;
        e.target.setAttribute('draggable', true);
        return;
    };

    //#endregion

    //#region  Settings Modal

    const handleShowSettingsModal = async () => {
        let id = checklistId;

        // If no checklist exists yet, create one and save the id to module
        if (!id) {
            if (!entryId) return;

            const response = await addChecklist({ EntryId: entryId, Title: checklistTitle || '' });
            if (!response.data || !response.data.success) return;

            id = response.data.data;
            setChecklistId(id);
            setChecklistEntryId(entryId);
            onUpdate({ ...module, checklistId: id });
        }

        session.showModal(() => (
            <ChecklistSettingsModal
                checklistId={id}
                title={checklistTitle}
                description={checklistDescription}
                items={items}
                module={module}
                onUpdate={onUpdate}
                onSaved={(title, description) => {
                    setChecklistTitle(title || '');
                    setChecklistDescription(description || '');
                }}
            />
        ));
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
                        onMouseDown={resetDragEvent}
                        onMouseUp={resetDragEvent}
                    >
                        <Checkbox
                            name={`checklist-item-${item.id}`}
                            id={`${module.id}_${item.id}`}
                            checked={item.status === 1}
                            onChange={(checked) => handleItemChange(item, checked)}
                            label={<>
                                {isEditable && editingItemId === item.id ? (
                                    <Input
                                        ref={titleInputRef}
                                        name={`checklist-item-input-${item.id}`}
                                        value={item.title}
                                        onChange={(e) => handleItemTitleChangeInput(e, item)}
                                        onBlur={handleInputBlur}
                                        autoFocus
                                        onKeyDown={handleTitleKeyDown}
                                        onDragStart={cancelDragEvent}
                                        onDrag={cancelDragEvent}
                                        onDragOver={cancelDragEvent}
                                        onDragLeave={cancelDragEvent}
                                        onDrop={cancelDragEvent}
                                        onDragEnd={cancelDragEvent}
                                    />
                                ) : (
                                    <span
                                        className="checklist-item-label"
                                        onClick={(e) => handleItemClick(e, item)}
                                        style={{ cursor: isEditable ? 'pointer' : 'default' }}
                                    >
                                        {item.title !== '' ? item.title : <>&nbsp;</>}
                                    </span>
                                )}
                            </>}
                        />
                    </div>
                ))}
                {isEditable && (
                    <div className="tool-bar">  
                        {!entryId ? (
                            <div className="left-side">
                                <p style={{ margin: '0.5em 0', color: '#666', fontStyle: 'italic' }}>
                                    Please save this entry before creating checklist items.
                                </p>
                            </div>
                        ) : (
                            <div className="left-side">
                                <button onClick={handleAddChecklistItem}><Icon name="add" />Add Task</button>
                            </div>
                        )}
                    </div>
                )}
            </div>
        </>
    );
}