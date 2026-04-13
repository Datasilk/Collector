import { useState, useEffect, useRef, useCallback } from 'react';
//components
import Icon from '@/components/ui/icon';
import Input from '@/components/forms/input';
import ModuleList from '../module-list';
import TabsSettingsModal from './tabs-settings-modal';
//modules
import modules from '../modules';
//context
import { useSession } from '@/context/session';

export default function TabsModule({ module, entryId, entry, journalId, journal, chapters, onUpdate, isEditable = true, manuallyAdded = false, tabButtons }) {
    //state
    const [activeTabId, setActiveTabId] = useState(null);
    const [editingTabId, setEditingTabId] = useState(null);
    const [showAddModuleDropdown, setShowAddModuleDropdown] = useState(false);

    //refs
    const tabInputRef = useRef(null);
    const addModuleDropdownRef = useRef(null);
    const addModuleButtonRef = useRef(null);
    const dragDelayTimerRef = useRef(null);
    const draggingTabRef = useRef(null);
    const dragStartPosRef = useRef({ x: 0, y: 0 });
    const tabIdsBeforeDragRef = useRef(null);
    const reorderedTabsRef = useRef(null);

    // Get tabs directly from module
    const tabs = module.tabs || [];

    //context
    const session = useSession();

    //effect
    useEffect(() => {
        // Set the first tab as active if no active tab is set
        if (tabs.length > 0 && !activeTabId) {
            setActiveTabId(tabs[0].id);
        }
    }, [tabs, activeTabId]);

    useEffect(() => {
        if (editingTabId && tabInputRef.current) {
            tabInputRef.current.focus();
        }
    }, [editingTabId]);

    useEffect(() => {
        const handleClickOutside = (event) => {
            if (showAddModuleDropdown &&
                addModuleDropdownRef.current &&
                !addModuleDropdownRef.current.contains(event.target) &&
                addModuleButtonRef.current &&
                !addModuleButtonRef.current.contains(event.target)) {
                setShowAddModuleDropdown(false);
            }
        };

        document.addEventListener('mousedown', handleClickOutside);

        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
        };
    }, [showAddModuleDropdown]);



    //actions
    const generateRandomId = () => {
        return String(Math.floor(Math.random() * 1000000));
    };

    const cancelDragEvent = (e) => {
        window.noDrag = true;
        e.stopPropagation();
        return;
    };

    const resetDragEvent = () => {
        window.noDrag = false;
        return;
    };

    useEffect(() => {
        if (!manuallyAdded) return;
        if (tabs && tabs.length > 0) return;

        const newTab = {
            id: generateRandomId(),
            title: 'New Tab',
            modules: []
        };

        const updatedTabs = [newTab];
        setActiveTabId(newTab.id);
        onUpdate({ ...module, tabs: updatedTabs });
    }, [manuallyAdded]);

    const handleAddTab = () => {
        if (!isEditable) return;

        const newTab = {
            id: generateRandomId(),
            title: 'New Tab',
            modules: []
        };

        const updatedTabs = [...tabs, newTab];
        setActiveTabId(newTab.id);
        setEditingTabId(newTab.id);
        onUpdate({ ...module, tabs: updatedTabs });
    };

    const handleTabClick = (tabId) => {
        if (!draggingTabRef.current) {
            setActiveTabId(tabId);
        }
    };

    const handleTabMouseDown = (e, tabId, tabElement) => {
        if (!isEditable || editingTabId) return;
        
        // Prevent the module from being dragged when dragging tabs
        e.stopPropagation();
        window.noDrag = true;
        
        dragStartPosRef.current = { x: e.clientX, y: e.clientY };
        
        // Add temporary mouseup listener to cancel drag if released before 333ms
        const handleEarlyMouseUp = () => {
            if (dragDelayTimerRef.current) {
                clearTimeout(dragDelayTimerRef.current);
                dragDelayTimerRef.current = null;
            }
            document.removeEventListener('mouseup', handleEarlyMouseUp);
        };
        
        document.addEventListener('mouseup', handleEarlyMouseUp);
        
        // Start timer for drag delay
        dragDelayTimerRef.current = setTimeout(() => {
            draggingTabRef.current = tabId;
            tabElement.classList.add('dragging');
            document.body.style.cursor = 'grabbing';
            document.body.style.userSelect = 'none';
            
            // Save the current tab IDs order before dragging starts
            tabIdsBeforeDragRef.current = module.tabs.map(t => t.id);
            reorderedTabsRef.current = null;
            
            // Remove early mouseup listener and add drag listeners
            document.removeEventListener('mouseup', handleEarlyMouseUp);
            document.addEventListener('mousemove', handleGlobalMouseMove);
            document.addEventListener('mouseup', handleGlobalMouseUp);
        }, 333);
    };

    const handleGlobalMouseMove = (e) => {
        if (!draggingTabRef.current) return;
        e.preventDefault();
    };

    const handleGlobalMouseUp = () => {
        // Clear the drag delay timer
        window.noDrag = false;
        if (dragDelayTimerRef.current) {
            clearTimeout(dragDelayTimerRef.current);
            dragDelayTimerRef.current = null;
        }
        
        // Clean up dragging state
        if (draggingTabRef.current) {
            const draggingElements = document.querySelectorAll('.tab.dragging');
            draggingElements.forEach(el => el.classList.remove('dragging'));
            draggingTabRef.current = null;
            document.body.style.cursor = '';
            document.body.style.userSelect = '';
            
            // Apply the reordered tabs and update parent module if order changed
            if (reorderedTabsRef.current) {
                const newTabIds = reorderedTabsRef.current.map(t => t.id);
                const orderChanged = tabIdsBeforeDragRef.current.some((id, index) => id !== newTabIds[index]);
                
                if (orderChanged) {
                    onUpdate({ ...module, tabs: reorderedTabsRef.current });
                }
                
                reorderedTabsRef.current = null;
            }
            
            tabIdsBeforeDragRef.current = null;
        }
        
        // Remove global listeners
        document.removeEventListener('mousemove', handleGlobalMouseMove);
        document.removeEventListener('mouseup', handleGlobalMouseUp);
    };

    const handleTabDragOver = (e, tabId) => {
        if (!draggingTabRef.current || draggingTabRef.current === tabId) return;
        
        // Calculate new tab order (for storing, not for state)
        const currentTabs = reorderedTabsRef.current || module.tabs;
        const dragIndex = currentTabs.findIndex(t => t.id === draggingTabRef.current);
        const hoverIndex = currentTabs.findIndex(t => t.id === tabId);
        
        if (dragIndex === -1 || hoverIndex === -1) return;
        
        const reorderedTabs = [...currentTabs];
        const [draggedTab] = reorderedTabs.splice(dragIndex, 1);
        reorderedTabs.splice(hoverIndex, 0, draggedTab);
        
        // Store the reordered tabs for saving on mouse up
        reorderedTabsRef.current = reorderedTabs;
        
        // Manipulate DOM directly for visual feedback
        const hoverElement = e.currentTarget;
        const draggingElement = document.querySelector('.tab.dragging');
        
        if (draggingElement && hoverElement && draggingElement !== hoverElement) {
            const parent = hoverElement.parentNode;
            const allTabs = Array.from(parent.children).filter(el => el.classList.contains('tab'));
            const dragCurrentIndex = allTabs.indexOf(draggingElement);
            const hoverCurrentIndex = allTabs.indexOf(hoverElement);
            
            if (dragCurrentIndex < hoverCurrentIndex) {
                parent.insertBefore(draggingElement, hoverElement.nextSibling);
            } else {
                parent.insertBefore(draggingElement, hoverElement);
            }
        }
    };

    const handleEditTabTitle = (e, tabId) => {
        if (!isEditable) return;
        e.stopPropagation();
        setEditingTabId(tabId);
    };

    const handleTabTitleChange = (tabId, newTitle) => {
        const updatedTabs = module.tabs.map(tab =>
            tab.id === tabId ? { ...tab, title: newTitle } : tab
        );
        onUpdate({ ...module, tabs: updatedTabs });
    };

    const handleTabTitleBlur = () => {
        setEditingTabId(null);
    };

    const handleTabTitleKeyDown = (e, tabId) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            setEditingTabId(null);
        } else if (e.key === 'Escape') {
            e.preventDefault();
            setEditingTabId(null);
        }
    };

    const handleRemoveTab = (e, tabId) => {
        if (!isEditable) return;
        e.stopPropagation();
        
        const updatedTabs = module.tabs.filter(tab => tab.id !== tabId);
        
        // If the active tab was removed, set the first tab as active
        if (activeTabId === tabId && updatedTabs.length > 0) {
            setActiveTabId(updatedTabs[0].id);
        }
        
        onUpdate({ ...module, tabs: updatedTabs });
    };

    const getActiveTab = () => {
        if (!module.tabs || module.tabs.length === 0) return null;
        return module.tabs.find(tab => tab.id === activeTabId);
    };

    const handleUpdatedModule = (updatedChildModule) => {
        const activeTab = module.tabs.find(tab => tab.id === activeTabId);
        if (!activeTab) return;

        const childModules = [...(activeTab.modules || [])];
        const index = childModules.findIndex(m => m.id === updatedChildModule.id);
        
        if (index > -1) {
            childModules[index] = {...childModules[index], ...updatedChildModule};
            const updatedTabs = module.tabs.map(tab =>
                tab.id === activeTabId ? { ...tab, modules: childModules } : tab
            );
            onUpdate({ ...module, tabs: updatedTabs });
        }
    };

    const handleRemovedModule = (moduleId, updatedModules) => {
        updateActiveTabModules(updatedModules);
    };

    const updateActiveTabModules = (updatedModules) => {
        const normalizedModules = Array.isArray(updatedModules) ? [...updatedModules] : [];
        const updatedTabs = module.tabs.map(tab =>
            tab.id === activeTabId ? { ...tab, modules: normalizedModules } : tab
        );
        onUpdate({ ...module, tabs: updatedTabs });
    };

    const handleAddedModule = (newModule, targetModuleId) => {
        const activeTab = module.tabs.find(tab => tab.id === activeTabId);
        if (!activeTab) return;

        const childModules = activeTab.modules || [];
        const moduleIndex = childModules.findIndex(m => m.id === targetModuleId);
        if (moduleIndex === -1) return;

        const updatedModules = [...childModules];
        updatedModules.splice(moduleIndex, 0, newModule);
        
        updateActiveTabModules(updatedModules);
    };

    const addModuleToBottom = (type) => {
        const activeTab = module.tabs.find(tab => tab.id === activeTabId);
        if (!activeTab) return;

        const newModuleId = generateRandomId();
        const newModule = {
            id: newModuleId,
            type: type,
            manuallyAdded: true
        };

        const updatedModules = [...(activeTab.modules || []), newModule];
        updateActiveTabModules(updatedModules);
        setShowAddModuleDropdown(false);
    };

    const handleShowSettingsModal = useCallback(() => {
        session.showModal(() => (
            <TabsSettingsModal
                module={module}
                onUpdate={onUpdate}
            />
        ));
    }, [session, onUpdate, module]);

    useEffect(() => {
        if (!tabButtons) return;
        tabButtons([
            {
                icon: 'settings',
                title: 'Tabs Settings',
                callback: handleShowSettingsModal
            }
        ]);
    }, [tabButtons]);

    const handleDroppedModule = (updatedEntryJson) => {
        updateActiveTabModules(updatedEntryJson.modules);
    };

    const activeTab = getActiveTab();

    return (
        <div className={`tabs-module ${module.style === 1 ? 'side-menu' : ''}`}>
            <div className="tabs-toolbar tool-bar">
                <div className="tabs-list">
                    {tabs.map(tab => (
                        <div
                            key={tab.id}
                            className={`tab ${activeTabId === tab.id ? 'active' : ''}`}
                            onClick={() => handleTabClick(tab.id)}
                            onMouseDown={(e) => handleTabMouseDown(e, tab.id, e.currentTarget)}
                            onMouseEnter={(e) => handleTabDragOver(e, tab.id)}
                        >
                            {editingTabId === tab.id ? (
                                <Input
                                    ref={tabInputRef}
                                    name={`tab-title-${tab.id}`}
                                    value={tab.title}
                                    onInput={(e) => handleTabTitleChange(tab.id, e.target.value)}
                                    onBlur={handleTabTitleBlur}
                                    onKeyDown={(e) => handleTabTitleKeyDown(e, tab.id)}
                                    onClick={(e) => e.stopPropagation()}
                                    onMouseDown={cancelDragEvent}
                                    onMouseUp={resetDragEvent}
                                />
                            ) : (
                                <span className="tab-title">
                                    {tab.title}
                                </span>
                            )}
                            {isEditable && (
                                <button
                                    className="icon tab-edit"
                                    onClick={(e) => handleEditTabTitle(e, tab.id)}
                                    title="Edit tab title"
                                >
                                    <Icon name="edit" />
                                </button>
                            )}
                            {isEditable && tabs.length > 1 && (
                                <button
                                    className="icon tab-close"
                                    onClick={(e) => handleRemoveTab(e, tab.id)}
                                    title="Remove tab"
                                >
                                    <Icon name="close" />
                                </button>
                            )}
                        </div>
                    ))}
                    {isEditable && (
                        <button
                            className="icon add-tab"
                            onClick={handleAddTab}
                            title="Add new tab"
                        >
                            <Icon name="add" />
                        </button>
                    )}
                </div>
            </div>
            <div className="tab-content">
                {activeTab && (<>
                {isEditable && (
                    <div className="add-module-container tool-bar">
                        <button
                            ref={addModuleButtonRef}
                            onClick={() => setShowAddModuleDropdown(!showAddModuleDropdown)}
                        >
                            <Icon name="add" /> Add Content
                        </button>
                        {showAddModuleDropdown && (
                            <div
                                className="module-dropdown"
                                ref={addModuleDropdownRef}
                            >
                                {modules.map(module => (
                                    <div
                                        key={module.id}
                                        className="module-option"
                                        onClick={() => addModuleToBottom(module.type)}
                                    >
                                        <Icon name={module.icon} />
                                        <span>{module.name}</span>
                                    </div>
                                ))}
                            </div>
                        )}
                    </div>
                )}
                    <ModuleList
                        entryJson={{ modules: activeTab.modules || [] }}
                        entryId={entryId}
                        entry={entry}
                        journalId={journalId}
                        journal={journal}
                        chapters={chapters}
                        isEditing={isEditable}
                        canDragDrop={isEditable}
                        updatedModule={handleUpdatedModule}
                        addedModule={handleAddedModule}
                        removedModule={handleRemovedModule}
                        droppedModule={handleDroppedModule}
                        containerId={`tab-${module.id}-${activeTab.id}`}
                    />
                </>)}
            </div>
        </div>
    );
}
