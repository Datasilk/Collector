import React, { useRef, useState, useEffect } from 'react';
//components
import Icon from '@/components/ui/icon';
//context
import { useSession } from '@/context/session';
//api
import { Journals } from '@/api/user/journals';
//modules
import modules from './modules';

/**
 * <summary>Module List Component</summary>
 * <description>Renders a list of modules for a journal entry</description>
 */
export default function ModuleList({
    entryJson,
    entryId,
    journalId,
    isEditing,
    showHoverTab = true,
    showHoverOutline = true,
    showLabel = true,
    canAddAbove = true,
    canPin = true,
    canUnpin = true,
    canDelete = true,
    canResize = true,
    canDragDrop = false,
    updatedModule,
    addedModule,
    removedModule,
    onPinModule,
    onUnPinModule,
    onReorderedModules,
    modulesRegistry = null
}) {
    // context
    const session = useSession();
    // state
    const [showModuleAboveDropdown, setShowModuleAboveDropdown] = useState(false);
    const [currentModuleId, setCurrentModuleId] = useState(null);
    const [pinnedModules, setPinnedModules] = useState([]);

    // refs
    const moduleDropdownRef = useRef(null);
    const moduleDropdownButtonRef = useRef(null);
    const resizingModuleRef = useRef(null);
    const resizeStartXRef = useRef(0);
    const resizeStartWidthRef = useRef(0);
    const draggedModuleIdRef = useRef(null);
    const dragOverModuleIdRef = useRef(null);
    const dragStartIndexRef = useRef(null);
    const dropIndexRef = useRef(null);

    //effect
    useEffect(() => {
        if(entryId) fetchPinnedModules();
    }, [entryId]);

    useEffect(() => {
        setTimeout(getRightAlignedModules, 1);
    }, [entryJson]);

    useEffect(() => {
        const handleClickOutside = (event) => {
            if (showModuleAboveDropdown &&
                moduleDropdownRef.current &&
                !moduleDropdownRef.current.contains(event.target) &&
                moduleDropdownButtonRef.current &&
                !moduleDropdownButtonRef.current.contains(event.target)) {
                setShowModuleAboveDropdown(false);
            }
        };

        document.addEventListener('mousedown', handleClickOutside);

        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
        };
    }, [showModuleAboveDropdown]);

    // actions

    //#region Module Actions

    const generateRandomId = () => {
        return String(Math.floor(Math.random() * 1000000));
    };
    const addModuleAbove = (type) => {
        if (!currentModuleId) return;

        const newModuleId = generateRandomId();
        const newModule = {
            id: newModuleId,
            type: type,
            manuallyAdded: true
        };

        setShowModuleAboveDropdown(false);
        if (addedModule) {
            addedModule(newModule, currentModuleId);
        }
    };

    const removeModule = (moduleId) => {
        if (removedModule) {
            removedModule(moduleId);
        }
    };

    const handleUpdatedModule = (module) => {
        if (!updatedModule) return;

        // Remove manuallyAdded property before passing to parent
        const { manuallyAdded, ...cleanModule } = module;
        updatedModule(cleanModule);
    };
    //#endregion

    //#region Pin Module

    const fetchPinnedModules = async () => {
        try {
            const api = Journals(session);
            const response = await api.getModulesByEntry(entryId);
            if (response.data.success) {
                setPinnedModules(response.data.data || []);
            }
        } catch (err) {
            console.error('Error fetching pinned modules:', err);
        }
    };

    const isModulePinned = (moduleId) => {
        // First check pinnedModules array from API
        if (pinnedModules && pinnedModules.length > 0) {
            return pinnedModules.some(pm => pm.moduleId == moduleId);
        }
        
        // Fallback: check if module has pinned property set to true
        const module = entryJson.modules.find(m => m.id == moduleId);
        return module?.pinned === true;
    };

    const pinModule = async (module) => {
        try {
            let moduleId = module.id != null ? String(module.id) : '';

            if (moduleId == '') {
                moduleId = generateRandomId();
                const updatedModuleData = { ...module, id: moduleId };
                handleUpdatedModule(updatedModuleData);
            }

            // Add module to journal
            const api = Journals(session);
            const moduleData = {
                journalId: journalId,
                journalEntryId: entryId,
                moduleId: moduleId,
                sort: 0,
                width: 1,
                height: 1
            };

            const response = await api.addModule(moduleData);
            if (response.data.success) {
                if(onPinModule){
                    onPinModule(moduleId);
                }

                // Update pinned modules list
                await fetchPinnedModules();

                // Create and show tooltip
                const moduleElement = document.querySelector(`.module-id-${moduleId}`);
                if (moduleElement) {
                    const tabContainer = moduleElement.querySelector('.module-tab-container');
                    if (tabContainer) {
                        // Create tooltip element
                        const tooltip = document.createElement('div');
                        tooltip.className = 'pin-tooltip';
                        tooltip.textContent = 'Module added to journal';

                        // Insert tooltip at the beginning of tab container
                        tabContainer.insertBefore(tooltip, tabContainer.firstChild);

                        // Trigger fade-in animation
                        setTimeout(() => {
                            tooltip.style.opacity = '1';
                        }, 10);

                        // Fade out and remove after 3 seconds
                        setTimeout(() => {
                            tooltip.style.opacity = '0';
                            setTimeout(() => {
                                tooltip.remove();
                            }, 300);
                        }, 3000);
                    }
                }
            } else {
                console.error('Failed to pin module:', response.data.message);
            }
        } catch (err) {
            console.error('Error pinning module:', err);
        }
    };

    const unpinModule = async (module) => {
        try {
            const moduleId = String(module.id);
            const api = Journals(session);

            const response = await api.deleteModule(journalId, entryId ?? module.entryId, moduleId);
            if (response.data.success) {
                if(onUnPinModule){
                    onUnPinModule(moduleId);
                }

                // Update pinned modules list
                if(entryId) await fetchPinnedModules();

                // Create and show tooltip
                const moduleElement = document.querySelector(`.module-id-${moduleId}`);
                if (moduleElement) {
                    const tabContainer = moduleElement.querySelector('.module-tab-container');
                    if (tabContainer) {
                        // Create tooltip element
                        const tooltip = document.createElement('div');
                        tooltip.className = 'pin-tooltip';
                        tooltip.textContent = 'Module removed from journal';

                        // Insert tooltip at the beginning of tab container
                        tabContainer.insertBefore(tooltip, tabContainer.firstChild);

                        // Trigger fade-in animation
                        setTimeout(() => {
                            tooltip.style.opacity = '1';
                        }, 10);

                        // Fade out and remove after 3 seconds
                        setTimeout(() => {
                            tooltip.style.opacity = '0';
                            setTimeout(() => {
                                tooltip.remove();
                            }, 300);
                        }, 3000);
                    }
                }
            } else {
                console.error('Failed to unpin module:', response.data.message);
            }
        } catch (err) {
            console.error('Error unpinning module:', err);
        }
    };
    //#endregion
    
    //#region Resize Width
    const getWidthClass = (width) => {
        if (!width || width >= 0.95) return 'width-100';
        if (width >= 0.85) return 'width-90';
        if (width >= 0.75) return 'width-80';
        if (width >= 0.65) return 'width-70';
        if (width >= 0.55) return 'width-60';
        if (width >= 0.45) return 'width-50';
        if (width >= 0.35) return 'width-40';
        if (width >= 0.25) return 'width-30';
        if (width >= 0.15) return 'width-20';
        return 'width-10';
    };

    const snapToWidth = (percentage) => {
        if (percentage >= 95) return 1.0;
        if (percentage >= 85) return 0.9;
        if (percentage >= 75) return 0.8;
        if (percentage >= 65) return 0.7;
        if (percentage >= 55) return 0.6;
        if (percentage >= 45) return 0.5;
        if (percentage >= 35) return 0.4;
        if (percentage >= 25) return 0.3;
        if (percentage >= 15) return 0.2;
        return 0.1;
    };

    const getRightAlignedModules = () => {
        //get all modules that are less than 100% width and are on the right side of the row
        const moduleDivs = document.querySelectorAll('.module');
        const updatedModules = [...entryJson.modules];
        let hasChanges = false;

        moduleDivs.forEach(moduleDiv => {
            const rect = moduleDiv.getBoundingClientRect();
            const moduleId = moduleDiv.getAttribute('data-id');
            
            //check next module to see if it is to the right of this module
            //if not, add this module to the right aligned modules list
            const nextModule = moduleDiv.nextElementSibling;
            let rightAligned = false;
            if (nextModule) {
                const nextRect = nextModule.getBoundingClientRect();
                if (nextRect.left < rect.right) {
                    rightAligned = true;
                }
            } else {
                rightAligned = true;
            }

            // Update CSS class
            if (rightAligned) {
                moduleDiv.classList.add('right');
            } else {
                moduleDiv.classList.remove('right');
            }

            // Update module state
            const moduleIndex = updatedModules.findIndex(m => m.id == moduleId);
            if (moduleIndex !== -1) {
                const currentModule = updatedModules[moduleIndex];
                const hadRightProperty = currentModule.right === true;
                
                if (rightAligned && !hadRightProperty) {
                    updatedModules[moduleIndex] = { ...currentModule, right: true };
                    hasChanges = true;
                } else if (!rightAligned && hadRightProperty) {
                    const { right, ...moduleWithoutRight } = currentModule;
                    updatedModules[moduleIndex] = moduleWithoutRight;
                    hasChanges = true;
                }
            }
        });

        // Only call updatedModule if there were changes
        if (hasChanges && updatedModule) {
            const updatedEntryJson = { ...entryJson, modules: updatedModules };
            updatedModule(updatedEntryJson);
        }
    };

    const handleResizeStart = (e, moduleId, side) => {
        e.preventDefault();
        e.stopPropagation();
        
        const moduleElement = document.querySelector(`.module-id-${moduleId}`);
        if (!moduleElement) return;
        
        const currentWidth = moduleElement.offsetWidth;
        const containerWidth = moduleElement.parentElement.offsetWidth;
        
        resizingModuleRef.current = { id: moduleId, side };
        resizeStartXRef.current = e.clientX;
        resizeStartWidthRef.current = currentWidth / containerWidth;
        
        // Add resizing class to handle
        const handle = e.target;
        handle.classList.add('resizing');
        
        // Add event listeners
        document.addEventListener('mousemove', handleResizeMove);
        document.addEventListener('mouseup', handleResizeEnd);
    };

    const handleResizeMove = (e) => {
        if (!resizingModuleRef.current) return;
        
        const moduleElement = document.querySelector(`.module-id-${resizingModuleRef.current.id}`);
        if (!moduleElement) return;
        
        const containerWidth = moduleElement.parentElement.offsetWidth;
        const deltaX = e.clientX - resizeStartXRef.current;
        const deltaPercentage = (deltaX / containerWidth) * 100;
        
        let newPercentage;
        if (resizingModuleRef.current.side === 'right') {
            newPercentage = (resizeStartWidthRef.current * 100) + deltaPercentage;
        } else {
            newPercentage = (resizeStartWidthRef.current * 100) - deltaPercentage;
        }
        
        // Clamp between 10% and 100%
        newPercentage = Math.max(10, Math.min(100, newPercentage));
        
        // Apply width
        const snappedWidth = snapToWidth(newPercentage);
        moduleElement.style.width = `${snappedWidth * 100}%`;
    };

    const handleResizeEnd = async (e) => {
        if (!resizingModuleRef.current) return;
        
        const moduleElement = document.querySelector(`.module-id-${resizingModuleRef.current.id}`);
        if (moduleElement) {
            const containerWidth = moduleElement.parentElement.offsetWidth;
            const finalWidth = moduleElement.offsetWidth / containerWidth;
            const snappedWidth = snapToWidth(finalWidth * 100);
            
            // Update module width via API
            const module = entryJson.modules.find(m => m.id == resizingModuleRef.current.id);
            if (module && journalId) {
                try {
                    const api = Journals(session);
                    const updateData = {
                        JournalId: parseInt(journalId, 10),
                        JournalEntryId: entryId || module.entryId || '00000000-0000-0000-0000-000000000000',
                        ModuleId: String(resizingModuleRef.current.id),
                        Width: parseFloat(snappedWidth),
                        Height: parseFloat(module.height || 1),
                        Sort: parseInt(module.sort || 999, 10)
                    };
                    
                    const response = await api.updateModule(updateData);
                    if (response.data.success) {
                        
                        // Update local module data - this triggers re-render with new className
                        if (updatedModule) {
                            updatedModule({ ...module, width: snappedWidth });
                        }
                    } else {
                        // If API call fails, revert inline style
                        moduleElement.style.width = '';
                    }
                } catch (err) {
                    console.error('Error updating module width:', err);
                    // Revert inline style on error
                    moduleElement.style.width = '';
                }
            } else {
                // Remove inline style if no API call needed
                moduleElement.style.width = '';
            }
            
            // Remove resizing class from all handles
            document.querySelectorAll('.module-resize-handle.resizing').forEach(handle => {
                handle.classList.remove('resizing');
            });
        }
        
        // Remove event listeners
        document.removeEventListener('mousemove', handleResizeMove);
        document.removeEventListener('mouseup', handleResizeEnd);
        
        resizingModuleRef.current = null;
    };
    //#endregion

    //#region Drag and Drop
    const handleDragStart = (e, moduleId, index) => {
        if (!canDragDrop) return;
        
        draggedModuleIdRef.current = moduleId;
        dragStartIndexRef.current = index;
        e.dataTransfer.effectAllowed = 'move';
        e.dataTransfer.setData('text/html', e.currentTarget);
        
        // Add dragging class
        e.currentTarget.classList.add('dragging');
    };

    const handleDragOver = (e, moduleId) => {
        if (!canDragDrop || !draggedModuleIdRef.current) return;
        
        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';
        
        if (moduleId !== draggedModuleIdRef.current) {
            // Remove drag-over classes from previous element
            if (dragOverModuleIdRef.current) {
                const prevElement = document.querySelector(`.module-id-${dragOverModuleIdRef.current}`);
                if (prevElement) {
                    prevElement.classList.remove('drag-over-left');
                    prevElement.classList.remove('drag-over-right');
                }
            }
            
            // Find the index of the module being dragged over
            const dragOverIndex = entryJson.modules.findIndex(m => m.id == moduleId);
            
            // Determine which side of the module we're hovering over
            const rect = e.currentTarget.getBoundingClientRect();
            const mouseX = e.clientX;
            const elementMiddle = rect.left + (rect.width / 2);
            const isLeftSide = mouseX < elementMiddle;
            
            // Add appropriate drag-over class to current element
            dragOverModuleIdRef.current = moduleId;
            if (isLeftSide) {
                e.currentTarget.classList.add('drag-over-left');
                dropIndexRef.current = dragOverIndex;
            } else {
                e.currentTarget.classList.add('drag-over-right');
                dropIndexRef.current = dragOverIndex + 1;
            }
        }
    };

    const handleDragLeave = (e) => {
        if (!canDragDrop) return;
        try {
            e.target.classList.remove('drag-over-left');
            e.target.classList.remove('drag-over-right');
        } catch (err) { }
    };

    const handleDrop = async (e, dropModuleId) => {
        if (!canDragDrop || !draggedModuleIdRef.current) return;
        
        e.preventDefault();
        e.stopPropagation();
        
        const dragIndex = dragStartIndexRef.current;
        const dropIndex = dropIndexRef.current;
        
        // Only update if the position actually changed
        if (dragIndex !== dropIndex && draggedModuleIdRef.current !== dropModuleId) {
            const newModules = [...entryJson.modules];
            const draggedModule = newModules[dragIndex];
            
            // Remove from old position
            newModules.splice(dragIndex, 1);
            
            // Insert at new position
            newModules.splice(dropIndex, 0, draggedModule);
            
            // Update the entry JSON with new module order
            if (updatedModule) {
                const updatedEntryJson = { ...entryJson, modules: newModules };
                updatedModule(updatedEntryJson);
            }

            // Call API to resort modules in the database
            if (journalId) {
                try {
                    const api = Journals(session);
                    const modulesToSort = newModules.map(m => ({
                        JournalEntryId: m.entryId || entryId,
                        ModuleId: String(m.id)
                    }));
                    
                    await api.resortModules(parseInt(journalId, 10), modulesToSort);
                } catch (err) {
                    console.error('Error resorting modules:', err);
                }
            }
        }
        
        // Remove drag-over classes
        try {
            e.target.classList.remove('drag-over-left');
            e.target.classList.remove('drag-over-right');
        } catch (err) { }
        
        // Reset drag state
        draggedModuleIdRef.current = null;
        dragOverModuleIdRef.current = null;
        dragStartIndexRef.current = null;
    };

    const handleDragEnd = (e) => {
        if (!canDragDrop) return;
        
        // Remove dragging class
        e.currentTarget.classList.remove('dragging');
        
        // Remove any remaining drag-over classes
        document.querySelectorAll('.module.drag-over-left, .module.drag-over-right').forEach(el => {
            el.classList.remove('drag-over-left');
            el.classList.remove('drag-over-right');
        });
        
        // Reset drag state
        draggedModuleIdRef.current = null;
        dragOverModuleIdRef.current = null;
        dragStartIndexRef.current = null;
    };
    //#endregion

    return (
        <div className="entry-modules">
            {entryJson.modules.map((module, index) => {
                if (!module.type) return;

                // Use custom modules registry if provided, otherwise use default
                const modulesList = modulesRegistry ? [...modules, ...modulesRegistry] : modules;
                const moduleType = modulesList.find(m => m.type === module.type);
                const ModuleComponent = moduleType?.module;
                return (
                    <div
                        key={'module-' + module.id}
                        className={
                            `module module-${module.type?.replace(' ', '-') ?? ''} ` +
                            `module-id-${module.id} ${isEditing ? 'editable' : ''} ` +
                            `${!showHoverOutline ? 'no-hover-outline' : ''} ` +
                            `${getWidthClass(module.width)} ` +
                            `${module.right ? 'right' : ''} ` +
                            `${canDragDrop ? 'draggable' : ''} ` +
                            (module.manuallyAdded ? 'manually-added' : '')
                        }
                        data-id={module.id}
                        draggable={canDragDrop}
                        onDragStart={canDragDrop ? (e) => handleDragStart(e, module.id, index) : undefined}
                        onDragOver={canDragDrop ? (e) => handleDragOver(e, module.id) : undefined}
                        onDragLeave={canDragDrop ? handleDragLeave : undefined}
                        onDrop={canDragDrop ? (e) => handleDrop(e, module.id) : undefined}
                        onDragEnd={canDragDrop ? handleDragEnd : undefined}
                    >
                        {isEditing && canResize && (
                            <>
                                <div 
                                    className="module-resize-handle left"
                                    onMouseDown={(e) => handleResizeStart(e, module.id, 'left')}
                                />
                                <div 
                                    className="module-resize-handle right"
                                    onMouseDown={(e) => handleResizeStart(e, module.id, 'right')}
                                />
                            </>
                        )}
                        {isEditing && showHoverTab && module.showTab !== false && (
                            <div className="module-tab-container">
                                <div className="module-tab">
                                    {showLabel && <div className="module-type">{moduleType?.name}</div>}
                                    <div className="box">
                                        <div className="tool-bar vertical">
                                            {canAddAbove == true && (<>
                                                <button
                                                    className="icon"
                                                    ref={module.id == currentModuleId ? moduleDropdownButtonRef : null}
                                                    onClick={() => {
                                                        setCurrentModuleId(module.id);
                                                        setShowModuleAboveDropdown(!showModuleAboveDropdown || currentModuleId != module.id);
                                                    }}
                                                >
                                                    <Icon name="add" />
                                                </button>
                                                {showModuleAboveDropdown && currentModuleId != module.id && (
                                                    <div
                                                        className="module-dropdown module-dropdown-left"
                                                        ref={moduleDropdownRef}
                                                    >
                                                        {modules.map(moduleOption => (
                                                            <div
                                                                key={moduleOption.id}
                                                                className="module-option"
                                                                onClick={() => addModuleAbove(moduleOption.type)}
                                                            >
                                                                <Icon name={moduleOption.icon} />
                                                                <span>{moduleOption.name}</span>
                                                            </div>
                                                        ))}
                                                    </div>
                                                )}
                                            </>)}
                                            {canUnpin && (isModulePinned(module.id) && (
                                                <button className="icon" onClick={() => unpinModule(module)} title="Unpin from journal">
                                                    <Icon name="keep_off" />
                                                </button>
                                            ))}
                                            {canPin && (!isModulePinned(module.id) && (
                                                <button className="icon" onClick={() => pinModule(module)} title="Pin to journal">
                                                    <Icon name="push_pin" />
                                                </button>
                                            ))}
                                            {canDelete && (
                                                <button className="icon" onClick={() => removeModule(module.id)} title="Delete module">
                                                    <Icon name="delete" />
                                                </button>
                                            )}
                                        </div>
                                    </div>
                                </div>
                            </div>
                        )}
                        <ModuleComponent
                            module={module}
                            entryId={entryId}
                            journalId={journalId}
                            onUpdate={handleUpdatedModule}
                            isEditable={isEditing}
                            manuallyAdded={module.manuallyAdded}
                        />
                    </div>
                )
            })}
        </div>
    );
}
