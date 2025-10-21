import { useState, useEffect, useRef } from 'react';
//components
import Checkbox from '@/components/forms/checkbox';
import Input from '@/components/forms/input';
import Icon from '@/components/ui/icon';
//context
import { useSession } from '@/context/session';
//api
import { JournalChecklists } from '@/api/user/journalChecklists';

export default function ChecklistModule({ module, onUpdate, isEditable = true, manuallyAdded = false }) {
    //state
    const [checklistId, setChecklistId] = useState(module.checklistId);
    const [mounted, setMounted] = useState(false);
    const [items, setItems] = useState([]);
    const [editingItemId, setEditingItemId] = useState(null);
    const debounceTimer = useRef(null);

    //refs
    const titleInputRef = useRef(null);

    //context
    const session = useSession();
    const {
        getChecklist,
        addChecklist,
        addChecklistItem,
        updateChecklistItemTitle,
        updateChecklistItemStatus
    } = JournalChecklists(session);

    //effect
    useEffect(() => {
        if (mounted) return;
        setMounted(true);
        if (checklistId) {
            getChecklist(checklistId).then(response => {
                if (response.data.success) {
                    setItems(response.data.data.items);
                }
            });
        }
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
        if (!isEditable) return;
        e.stopPropagation();
        e.preventDefault();
        setEditingItemId(item.id);
    };

    const handleItemChange = (item, checked) => {
        if (!isEditable) return;

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
        if (!isEditable) return;

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

    return (
        <>
            <div className="checklist">
                {items && items.map((item, index) => (
                    <div key={item.id || index} className="checklist-item">
                        <Checkbox
                            name={`checklist-item-${item.id}`}
                            id={`${module.id}_${item.id}`}
                            checked={item.status === 1}
                            onChange={isEditable ? (checked) => handleItemChange(item, checked) : undefined}
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
                                        onClick={isEditable ? (e) => handleItemClick(e, item) : undefined}
                                        style={{ cursor: isEditable ? 'pointer' : 'default' }}
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
        </>
    );
}