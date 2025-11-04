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
    droppedModule,
    onPinModule,
    onUnPinModule,
    modulesRegistry = null,
    containerId = 'main',
    fromSnapshotId = null
}) {
    // context
    const session = useSession();
    // state
    const [showModuleAboveDropdown, setShowModuleAboveDropdown] = useState(false);
    const [currentModuleId, setCurrentModuleId] = useState(null);
    const [pinnedModules, setPinnedModules] = useState([]);
    const [tabButtons, setTabButtons] = useState([]);

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
    const containerRef = useRef(null);
    const tabButtonsRef = useRef([]);
    const tabButtonsTimer = useRef(null);
    const deleteListenersRef = useRef([]);

    //effect
    useEffect(() => {
        if (entryId) fetchPinnedModules();
    }, [entryId]);

    useEffect(() => {
        const handleClickOutside = (event) => {
            var node = event.target;
            while (node && node != null) {
                if (node.classList?.contains('module-dropdown')) return;
                node = node.parentNode;
            }
            setShowModuleAboveDropdown(false);
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

    const removeModule = async (moduleId) => {
        //find listener associated with module used to call unmount method
        const listener = deleteListenersRef.current.find(listener => listener.moduleId == moduleId);
        if (listener) {
            // Wait for the callback promise to resolve (e.g., user confirmation)
            const moduleItem = entryJson.modules.find(module => module.id == moduleId);
            listener.callback(moduleItem, () => {
                deleteListenersRef.current = deleteListenersRef.current.filter(listener => listener.moduleId != moduleId);
                removedModule(moduleId); //notify parent
            });
        }else if (removedModule) {
            removedModule(moduleId); //notify parent
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
                if (onPinModule) {
                    onPinModule(moduleId);
                }

                // Update pinned modules list
                await fetchPinnedModules();

                // Wait for DOM to update before showing tooltip
                setTimeout(() => {
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
                }, 100);
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
                if (onUnPinModule) {
                    onUnPinModule(moduleId);
                }

                // Update pinned modules list
                if (entryId) await fetchPinnedModules();

                // Wait for DOM to update before showing tooltip
                setTimeout(() => {
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
                }, 100);
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
        if (!canDragDrop || window.noDrag == true) {
            e.preventDefault();
            return;
        }
        e.stopPropagation();

        draggedModuleIdRef.current = moduleId;
        dragStartIndexRef.current = index;
        e.dataTransfer.effectAllowed = 'move';
        //find container based on moduleId
        const node = e.target;
        const allContainers = getModuleHierarchyFromNode(node);
        const allContainerModules = getAllContainerModules(allContainers);

        // Store drag data including container info
        const dragData = {
            moduleId: moduleId,
            sourceContainerId: allContainers[0] ?? 'main',
            sourceIndex: index,
            module: entryJson.modules[index],
            allContainers: allContainers,
            allContainerModules: allContainerModules
        };
        window.dragData = dragData;

        // Add dragging class
        e.currentTarget.classList.add('dragging');
    };

    const handleDragOver = (e, moduleId) => {
        if (!canDragDrop || window.noDrag == true) {
            e.preventDefault();
            return;
        }
        //NOTE: Do not stop propogation!
        e.dataTransfer.dropEffect = 'move';

        // Allow drag over even if draggedModuleIdRef is not set (for cross-container drops)
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
        if (!canDragDrop || window.noDrag == true) {
            e.preventDefault();
            return;
        }
        try {
            e.target.classList.remove('drag-over-left');
            e.target.classList.remove('drag-over-right');
        } catch (err) { }
    };

    const handleDrop = async (e) => {
        if (!canDragDrop || window.noDrag == true) {
            e.preventDefault();
            return;
        }
        e.stopPropagation();
        // Try to get drag data for cross-container drops
        let dragData = window.dragData;
        // Handle cross-container drop

        if (dragData && dragData.sourceContainerId != window.dragOverContainerId) {
            // If dropping into a nested container (tab or module-list)
            let newModules = [...entryJson.modules];
            const dropIndex = dropIndexRef.current !== undefined ? dropIndexRef.current : newModules.length;

            if (window.dragOverContainerId == 'main') {
                // Dropping into main container
                newModules.splice(dropIndex, 0, dragData.module);
            } else {
                const dropContainer = document.querySelector(`.entry-modules[data-id="${window.dragOverContainerId}"]`);
                if (!dropContainer) return;
                const allContainers = getModuleHierarchyFromNode(dropContainer);
                const allContainerModules = getAllContainerModules(allContainers);
                newModules = addModuleToHierarchy(newModules, allContainerModules, dragData.module, dropIndex);
            }

            //remove module from source container
            if (dragData.sourceContainerId == 'main') {
                newModules = newModules.filter(m => m.id != dragData.moduleId);
            } else {
                newModules = removeModuleFromHierarchy(newModules, dragData.allContainerModules, dragData.moduleId);
            }

            // Update this container
            if (droppedModule) {
                const updatedEntryJson = { ...entryJson, modules: newModules };
                droppedModule(updatedEntryJson);
            }

            // Clean up
            handleDragEnd();
            return;
        }

        // Handle same-container drop
        if (!draggedModuleIdRef.current) return;

        const dragIndex = dragStartIndexRef.current;
        const dropIndex = dropIndexRef.current;

        // Only update if the position actually changed
        if (dragIndex !== dropIndex) {
            const newModules = [...entryJson.modules];
            const draggedModule = newModules[dragIndex];

            // Remove from old position
            newModules.splice(dragIndex, 1);

            // Adjust drop index if dragging down
            const adjustedDropIndex = dragIndex < dropIndex ? dropIndex - 1 : dropIndex;

            // Insert at new position
            newModules.splice(adjustedDropIndex, 0, draggedModule);

            // Update the entry JSON with new module order
            if (droppedModule) {
                const updatedEntryJson = { ...entryJson, modules: newModules };
                droppedModule(updatedEntryJson);
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
        if (e && e.currentTarget) {
            e.currentTarget.classList.remove('dragging');
        } else {
            // If no event, remove from all elements
            document.querySelectorAll('.module.dragging').forEach(el => {
                el.classList.remove('dragging');
            });
        }

        // Remove any remaining drag-over classes
        document.querySelectorAll('.module.drag-over-left, .module.drag-over-right').forEach(el => {
            el.classList.remove('drag-over-left');
            el.classList.remove('drag-over-right');
        });

        // Reset drag state
        draggedModuleIdRef.current = null;
        dragOverModuleIdRef.current = null;
        dragStartIndexRef.current = null;
        dropIndexRef.current = null;
        window.dragOverContainerModuleId = null;
        window.dragOverContainerId = null;

        // Remove all drag-over-container classes
        document.querySelectorAll('.entry-modules.drag-over-container').forEach(el => {
            el.classList.remove('drag-over-container');
        });
    };

    const handleContainerDragOver = (e) => {
        if (!canDragDrop) return;
        e.preventDefault();
        e.stopPropagation();

        //find container from e.target
        let node = e.target;
        while (node && !node.classList?.contains('entry-modules')) {
            node = node.parentNode;
        }
        const containerId = node?.getAttribute('data-id');
        // Only handle if dragging over the container itself, not over a module
        if (containerId) {
            //find parent module
            let moduleNode = node;
            while (moduleNode && !moduleNode.classList?.contains('module')) {
                moduleNode = moduleNode.parentNode;
            }
            if (moduleNode || containerId == 'main') {
                const id = moduleNode?.getAttribute('data-id') || null;
                if (window.dragOverContainerModuleId == id && window.dragOverContainerId == containerId) return;
                window.dragOverContainerModuleId = id;
                window.dragOverContainerId = containerId;

                // Remove drag-over-container class from all entry-modules
                document.querySelectorAll('.entry-modules.drag-over-container').forEach(el => {
                    el.classList.remove('drag-over-container');
                });

                // Add drag-over-container class to current container
                node.classList.add('drag-over-container');
            }
        }
    };
    //#endregion

    //#region Add/Remove Module in Hierarchy
    const addModuleToHierarchy = (modules, moduleIdPath, moduleToAdd, dropIndex) => {
        // Clone the modules array to avoid mutation
        const newModules = JSON.parse(JSON.stringify(modules));

        // If no path or empty path, add to root
        if (!moduleIdPath || moduleIdPath.length === 0) {
            newModules.splice(dropIndex, 0, moduleToAdd);
            return newModules;
        }

        // Traverse the hierarchy following the path
        let currentLevel = newModules;
        let targetModule = null;
        for (let i = 0; i < moduleIdPath.length; i++) {
            const moduleId = moduleIdPath[i];
            targetModule = currentLevel.find(m => m.id == moduleId);

            if (!targetModule) {
                console.error(`Module with id ${moduleId} not found in hierarchy`);
                return modules; // Return original if path is invalid
            }

            // If this is the last module in the path, add to its modules array
            if (i === moduleIdPath.length - 1) {
                if (!targetModule.modules) {
                    targetModule.modules = [];
                }
                targetModule.modules.splice(dropIndex, 0, moduleToAdd);
            } else {
                // Otherwise, continue traversing
                if (!targetModule.modules) {
                    console.error(`Module ${moduleId} has no modules array to traverse`);
                    return modules;
                }
                currentLevel = targetModule.modules;
            }
        }

        return newModules;
    };

    const removeModuleFromHierarchy = (modules, moduleIdPath, moduleIdToRemove) => {
        // Clone the modules array to avoid mutation
        const newModules = JSON.parse(JSON.stringify(modules));

        // If no path, remove from root
        if (!moduleIdPath || moduleIdPath.length === 0) {
            return newModules.filter(m => m.id != moduleIdToRemove);
        }

        // Traverse to the parent module
        let currentLevel = newModules;
        let targetModule = null;

        for (let i = 0; i < moduleIdPath.length; i++) {
            const moduleId = moduleIdPath[i];
            targetModule = currentLevel.find(m => m.id == moduleId);

            if (!targetModule) {
                console.error(`Module with id ${moduleId} not found in hierarchy`);
                return modules;
            }

            // If this is the last module in the path, remove from its modules array
            if (i === moduleIdPath.length - 1) {
                if (targetModule.modules) {
                    targetModule.modules = targetModule.modules.filter(m => m.id != moduleIdToRemove);
                }
            } else {
                // Otherwise, continue traversing
                if (!targetModule.modules) {
                    console.error(`Module ${moduleId} has no modules array to traverse`);
                    return modules;
                }
                currentLevel = targetModule.modules;
            }
        }

        return newModules;
    };

    const getModuleHierarchyFromNode = (moduleNode) => {
        let node = moduleNode;
        const allContainers = [];
        while (node != null) {
            //get all containers in the hierarchy
            if (node.classList?.contains('entry-modules')) {
                const id = node.getAttribute('data-id');
                if (id != 'main') allContainers.push(id);
            }
            node = node.parentNode;
        }
        return allContainers;
    };

    const getAllContainerModules = (containers) => {
        return containers.map(c => {
            let containerNode = document.querySelector(`.entry-modules[data-id="${c}"]`);
            while (containerNode) {
                if (containerNode.classList.contains('module')) {
                    return containerNode.getAttribute('data-id');
                }
                containerNode = containerNode.parentElement;
            }
            return null;
        }).filter(m => m != null);
    };
    //#endregion

    //#region Mouse Over / Leave
    const handleMouseOver = (e) => {
        e.stopPropagation();
        let node = e.target;
        if (window.mouseOverElem == node) return;
        window.mouseOverElem = node;
        let foundDrag = false;
        while (node && !node.classList?.contains('module')) {
            if (node?.classList?.contains('module-tab-container')) return;
            if (node?.classList?.contains('no-drag') ||
                node?.classList?.contains('ck')) {
                window.noDrag = true;
                foundDrag = true;
            }
            node = node.parentNode;
        }
        if (!foundDrag) window.noDrag = false;
        if (node && window.mouseOverNode?.getAttribute('data-id') == node.getAttribute('data-id')) return;
        window.mouseOverNode = node;
        document.querySelectorAll('.module.hover').forEach(el => {
            if (el != node) el.classList.remove('hover');
        });
        if (node == null) return;
        node.classList.add('hover');
    };

    const handleMouseLeave = (e) => {
        e.stopPropagation();
        window.mouseOverElem = null;
        window.mouseOverNode = null;
        document.querySelectorAll('.module.hover').forEach(el => {
            el.classList.remove('hover');
        });
    };
    //#endregion

    //#region Events
    const handleSetTabButtons = (buttons, moduleId) => {
        tabButtonsRef.current = [...tabButtonsRef.current, { moduleId: moduleId, buttons: buttons }];
        if (tabButtonsTimer.current) clearTimeout(tabButtonsTimer.current);
        tabButtonsTimer.current = setTimeout(() => {
            setTabButtons(tabButtonsRef.current);
            tabButtonsTimer.current = null;
        }, 100);
    };

    const handleDeleteListener = (module, callback) => {
        deleteListenersRef.current = [...deleteListenersRef.current, { moduleId: module.id, callback: callback }];
    };

    //#endregion

    //#region Render
    return (
        <div
            className={`entry-modules container-${containerId}`}
            ref={containerRef}
            data-id={containerId}
            onDragOver={handleContainerDragOver}
            onMouseLeave={isEditing ? handleMouseLeave : undefined}
        >
            {entryJson.modules.map((module, index) => {
                if (!module.type) return;

                // Use custom modules registry if provided, otherwise use default
                const modulesList = modulesRegistry ? [...modules, ...modulesRegistry] : modules;
                const moduleType = modulesList.find(m => m.type === module.type);
                const ModuleComponent = moduleType?.module;
                const filteredButtons = tabButtons.filter(a => a.moduleId == module.id)[0] ?? null;
                return (
                    <div
                        key={'module-' + module.id}
                        className={
                            `module module-${module.type?.replace(' ', '-') ?? ''} ` +
                            `module-id-${module.id} ${isEditing ? 'editable' : ''} ` +
                            `${!showHoverOutline ? 'no-hover-outline' : ''} ` +
                            `${getWidthClass(module.width)} ` +
                            //`${module.right ? 'right' : ''} ` +
                            `${canDragDrop ? 'draggable' : ''} ` +
                            `${showLabel ? 'show-label' : ''} ` +
                            (module.manuallyAdded ? 'manually-added' : '')
                        }
                        data-id={module.id}
                        draggable={canDragDrop}
                        onDragStart={canDragDrop ? (e) => handleDragStart(e, module.id, index) : undefined}
                        onDragOver={canDragDrop ? (e) => handleDragOver(e, module.id) : undefined}
                        onDragLeave={canDragDrop ? handleDragLeave : undefined}
                        onDrop={canDragDrop ? handleDrop : undefined}
                        onDragEnd={canDragDrop ? handleDragEnd : undefined}
                        onMouseOver={isEditing ? handleMouseOver : undefined}
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
                                        <div className="tool-bar">
                                            {canAddAbove == true &&
                                                //Add above button
                                                (<>
                                                    <button
                                                        className="icon"
                                                        ref={module.id == currentModuleId ? moduleDropdownButtonRef : null}
                                                        onClick={() => {
                                                            setCurrentModuleId(module.id);
                                                            setShowModuleAboveDropdown(true);
                                                        }}
                                                    >
                                                        <Icon name="add" />
                                                    </button>
                                                    {showModuleAboveDropdown && (
                                                        <div
                                                            className="module-dropdown module-dropdown-left"
                                                            ref={moduleDropdownRef}
                                                        >
                                                            {modules.map(moduleOption => (
                                                                <div
                                                                    key={'module-' + module.id + '-' + moduleOption.id}
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
                                            {canUnpin && module.showPinned !== false && (isModulePinned(module.id) && ( //unpin button
                                                <button className="icon" onClick={() => unpinModule(module)} title="Unpin from journal">
                                                    <Icon name="keep_off" />
                                                </button>
                                            ))}
                                            {canPin && module.showPinned !== false && (!isModulePinned(module.id) && ( //pin button
                                                <button className="icon" onClick={() => pinModule(module)} title="Pin to journal">
                                                    <Icon name="push_pin" />
                                                </button>
                                            ))}
                                            {filteredButtons && ( //module-defined buttons
                                                filteredButtons.buttons.map((button, index) => {
                                                    return (
                                                        <button key={'module-' + module.id + '-usertab-' + module.id + '_' + index} className="icon" onClick={() => button.callback()} title={button.title}>
                                                            <Icon name={button.icon} />
                                                        </button>
                                                    );
                                                })
                                            )}
                                            {canDelete && ( //Delete button
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
                            tabButtons={(buttons) => handleSetTabButtons(buttons, module.id)}
                            setDeleteListener={handleDeleteListener}
                            fromSnapshotId={fromSnapshotId}
                        />
                    </div>
                )
            })}
        </div>
    );
    //#endregion
}
