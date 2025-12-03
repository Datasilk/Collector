import React, { useRef, useState, useEffect, useCallback } from 'react';
//components
import Icon from '@/components/ui/icon';
//context
import { useSession } from '@/context/session';
//api
import { Journals } from '@/api/user/journals';
//helpers
import { uploadImageForModule, uploadPdfForModule, uploadFileForModule } from '@/helpers/files';
//modules
import modules from './modules';

/**
 * <summary>Module List Component</summary>
 * <description>Renders a list of modules for a journal entry</description>
 */
export default function ModuleList({
    entryJson,
    entryId,
    entry,
    journalId,
    journal = null,
    chapters = [],
    isEditing,
    showHoverTab = true,
    showHoverOutline = true,
    showLabel = false,
    canAddAbove = true,
    canDelete = true,
    canResize = true,
    canDragDrop = false,
    updatedModule,
    addedModule,
    removedModule,
    droppedModule,
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
    const [hasUpdated, setHasUpdated] = useState(null);

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
    const tabButtonHandlersRef = useRef(new Map());
    const tabButtonsTimer = useRef(null);
    const deleteListenersRef = useRef([]);
    const hoveredModuleIdRef = useRef(null);
    const isJournalEntry = journal?.entryId == entryId;

    //effect

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

    useEffect(() => {
        setHasUpdated(1 + (999999 * Math.random()));
    }, [entryJson]);

    // actions

    //#region Module Actions

    const generateRandomId = () => {
        return String(Math.floor(Math.random() * 1000000));
    };

    const addModuleAbove = (type, options, targetModuleId = null) => {
        const moduleId = targetModuleId ?? currentModuleId;
        if (!moduleId) return;

        const newModuleId = generateRandomId();
        const newModule = {
            id: newModuleId,
            type: type,
            manuallyAdded: true,
            ...(options ?? {})
        };

        setShowModuleAboveDropdown(false);
        if (addedModule) {
            addedModule(newModule, moduleId);
        }

        return newModule;
    };

    const removeModule = async (moduleId) => {
        const listener = deleteListenersRef.current.find(listener => listener.moduleId == moduleId);
        if (listener) {
            const moduleItem = findModuleInHierarchy(entryJson.modules, moduleId);
            if (!moduleItem) {
                console.error(`Module with id ${moduleId} not found in hierarchy`);
                return;
            }
            listener.callback(moduleItem);
            deleteListenersRef.current = deleteListenersRef.current.filter(listener => listener.moduleId != moduleId);
            const updatedModules = removeModuleFromHierarchy(entryJson.modules, moduleId);

            if (removedModule) {
                removedModule(moduleId, updatedModules); 
            }
        } else if (removedModule) {
            const updatedModules = removeModuleFromHierarchy(entryJson.modules, moduleId);
            removedModule(moduleId, updatedModules);
        }
    };

    const handleUpdatedModule = (module) => {
        if (!updatedModule) return;
        const { manuallyAdded, ...cleanModule } = module;
        console.log('updated module', cleanModule);
        updatedModule(cleanModule);
    };
    //#endregion

    //#region Clipboard
    const handleContainerPaste = (event) => {
        if (!isEditing) return;
        if (containerId !== 'main') return;

        const clipboardData = event.clipboardData || window?.clipboardData;
        if (!clipboardData?.items?.length) return;

        const clipboardFiles = Array.from(clipboardData.items)
            .filter(item => item.kind === 'file')
            .map(item => item.getAsFile())
            .filter(file => !!file);

        if (!clipboardFiles.length) return;

        event.preventDefault();

        if (typeof window === 'undefined') return;

        const ensureClipboardStores = () => {
            window.clipboardImages = window.clipboardImages || {};
            window.clipboardFileBuffer = window.clipboardFileBuffer || {};
            window.__droppedVideoFiles = window.__droppedVideoFiles || {};
        };
        ensureClipboardStores();

        const createDescriptor = (file) => {
            const fileType = (file.type || '').toLowerCase();
            const fileName = (file.name || '').toLowerCase();

            if (fileType.startsWith('image/')) {
                return {
                    moduleType: 'image',
                    options: { uploadFromClipboard: true, manuallyAdded: false },
                    storeFile: (moduleId) => {
                        window.clipboardImages[moduleId] = file;
                    }
                };
            }

            if (fileType.startsWith('video/')) {
                return {
                    moduleType: 'video-player',
                    options: { autoUploadDroppedVideo: true, manuallyAdded: false },
                    storeFile: (moduleId) => {
                        window.__droppedVideoFiles[moduleId] = file;
                    }
                };
            }

            const isPdf = fileType === 'application/pdf' || fileName.endsWith('.pdf');
            if (isPdf) {
                return {
                    moduleType: 'pdf-viewer',
                    options: { uploadFromClipboard: true, manuallyAdded: false },
                    storeFile: (moduleId) => {
                        window.clipboardFileBuffer[moduleId] = file;
                    }
                };
            }

            return {
                moduleType: 'file-download',
                options: { uploadFromClipboard: true, manuallyAdded: false },
                storeFile: (moduleId) => {
                    window.clipboardFileBuffer[moduleId] = file;
                }
            };
        };

        const descriptors = clipboardFiles
            .map(file => createDescriptor(file))
            .filter(descriptor => !!descriptor);

        if (!descriptors.length) return;

        const hoveredModuleId = window.moduleHovered ?? hoveredModuleIdRef.current ?? null;

        const insertAboveModule = (descriptorList, targetModuleId) => {
            for (let i = descriptorList.length - 1; i >= 0; i--) {
                const descriptor = descriptorList[i];
                const newModule = addModuleAbove(
                    descriptor.moduleType,
                    { ...descriptor.options },
                    targetModuleId
                );
                if (newModule) {
                    descriptor.storeFile(newModule.id);
                }
            }
        };

        if (hoveredModuleId) {
            insertAboveModule(descriptors, hoveredModuleId);
            return;
        }

        if (!droppedModule) return;

        const updatedModules = [...(entryJson.modules || [])];

        descriptors.forEach(descriptor => {
            const newModuleId = generateRandomId();
            const moduleData = {
                id: newModuleId,
                type: descriptor.moduleType,
                manuallyAdded: descriptor.options?.manuallyAdded ?? true,
                ...descriptor.options
            };
            updatedModules.push(moduleData);
            descriptor.storeFile(newModuleId);
        });

        const updatedEntryJson = {
            ...entryJson,
            modules: updatedModules
        };

        droppedModule(updatedEntryJson, updatedModules);
    };

    useEffect(() => {
        if (!isEditing || containerId !== 'main') return;

        const handleDocumentPaste = (event) => {
            handleContainerPaste(event);
        };

        document.addEventListener('paste', handleDocumentPaste);
        return () => {
            document.removeEventListener('paste', handleDocumentPaste);
        };
    }, [isEditing, containerId, entryJson]);

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
            const module = entryJson.modules.find(m => m.id == resizingModuleRef.current.id);
            if (module) {
                updatedModule({ ...module, width: snappedWidth });
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
        // Always prevent default browser behavior (which can open the file in a new tab)
        e.preventDefault();

        if (!canDragDrop || window.noDrag == true) {
            return;
        }
        // Remove event listener
        document.removeEventListener('drop', handleDrop);
        e.stopPropagation();

        // Handle external file drop from filesystem
        const files = e.dataTransfer?.files;
        if (files && files.length > 0) {
            const fileArray = Array.from(files);
            const imageFile = fileArray.find(f => f.type && f.type.startsWith('image/'));
            const pdfFile = fileArray.find(f => f.type === 'application/pdf' || f.name.toLowerCase().endsWith('.pdf'));
            const videoFile = fileArray.find(f => f.type && f.type.startsWith('video/'));
            const otherFile = fileArray.find(f =>
                !f.type ||
                (!f.type.startsWith('image/') &&
                 f.type !== 'application/pdf' &&
                 !f.name.toLowerCase().endsWith('.pdf') &&
                 !f.type.startsWith('video/'))
            );

            // Handle image file drop
            if (imageFile && entryId && journalId && droppedModule) {
                try {
                    const newModuleId = generateRandomId();

                    // Base image module to insert
                    const newImageModule = {
                        id: newModuleId,
                        type: 'image',
                        manuallyAdded: true
                    };

                    // Start from the full module hierarchy
                    let newModules = [...entryJson.modules];

                    // Determine drop index within target container
                    const dropIndex = (dropIndexRef.current !== undefined && dropIndexRef.current !== null)
                        ? dropIndexRef.current
                        : newModules.length;

                    // Determine target container path, mirroring cross-container logic
                    if (!window.dragOverContainerId || window.dragOverContainerId === 'main') {
                        // Drop into root/main container
                        newModules.splice(dropIndex, 0, newImageModule);
                    } else {
                        const dropContainer = document.querySelector(`.entry-modules[data-id="${window.dragOverContainerId}"]`);
                        if (!dropContainer) {
                            console.error('Drop container not found for image drop');
                        } else {
                            const allContainers = getModuleHierarchyFromNode(dropContainer);
                            const allContainerModules = getAllContainerModules(allContainers);
                            newModules = addModuleToHierarchy(newModules, allContainerModules, newImageModule, dropIndex);
                        }
                    }

                    // Upload image and metadata via helper
                    const uploadResult = await uploadImageForModule(session, {
                        imageFile,
                        journalId,
                        entryId,
                        moduleId: newModuleId
                    });

                    if (uploadResult.success) {
                        const updatedImageModule = {
                            ...newImageModule,
                            image: uploadResult.fileName
                        };
                        newModules = updateModuleInHierarchy(newModules, newModuleId, updatedImageModule);
                    } else {
                        console.error('Error uploading image after drop:', uploadResult.message);
                    }

                    const updatedEntryJson = { ...entryJson, modules: newModules };
                    droppedModule(updatedEntryJson, newModules);
                } catch (err) {
                    console.error('Error handling dropped image file:', err);
                }

                // Clean up drag state/classes similar to normal drop
                try {
                    e.target.classList.remove('drag-over-left');
                    e.target.classList.remove('drag-over-right');
                } catch (err) { }

                draggedModuleIdRef.current = null;
                dragOverModuleIdRef.current = null;
                dragStartIndexRef.current = null;
                dropIndexRef.current = null;
                window.dragOverContainerModuleId = null;
                window.dragOverContainerId = null;
                document.querySelectorAll('.entry-modules.drag-over-container').forEach(el => {
                    el.classList.remove('drag-over-container');
                });

                return;
            }

            // Handle PDF file drop using pdf-viewer module
            if (!imageFile && pdfFile && entryId && journalId && droppedModule) {
                try {
                    const newModuleId = generateRandomId();

                    const newPdfModule = {
                        id: newModuleId,
                        type: 'pdf-viewer',
                        manuallyAdded: false
                    };

                    let newModules = [...entryJson.modules];

                    const dropIndex = (dropIndexRef.current !== undefined && dropIndexRef.current !== null)
                        ? dropIndexRef.current
                        : newModules.length;

                    if (!window.dragOverContainerId || window.dragOverContainerId === 'main') {
                        newModules.splice(dropIndex, 0, newPdfModule);
                    } else {
                        const dropContainer = document.querySelector(`.entry-modules[data-id="${window.dragOverContainerId}"]`);
                        if (!dropContainer) {
                            console.error('Drop container not found for PDF drop');
                        } else {
                            const allContainers = getModuleHierarchyFromNode(dropContainer);
                            const allContainerModules = getAllContainerModules(allContainers);
                            newModules = addModuleToHierarchy(newModules, allContainerModules, newPdfModule, dropIndex);
                        }
                    }

                    const uploadResult = await uploadPdfForModule(session, {
                        pdfFile,
                        journalId,
                        entryId,
                        moduleId: newModuleId
                    });

                    if (uploadResult.success) {
                        const updatedPdfModule = {
                            ...newPdfModule,
                            filename: uploadResult.fileName,
                            originalFilename: uploadResult.originalName,
                            fileSize: uploadResult.fileSize
                        };
                        newModules = updateModuleInHierarchy(newModules, newModuleId, updatedPdfModule);
                    } else {
                        console.error('Error uploading PDF after drop:', uploadResult.message);
                    }

                    const updatedEntryJson = { ...entryJson, modules: newModules };
                    droppedModule(updatedEntryJson, newModules);
                } catch (err) {
                    console.error('Error handling dropped PDF file:', err);
                }

                // Clean up drag state/classes similar to normal drop
                try {
                    e.target.classList.remove('drag-over-left');
                    e.target.classList.remove('drag-over-right');
                } catch (err) { }

                draggedModuleIdRef.current = null;
                dragOverModuleIdRef.current = null;
                dragStartIndexRef.current = null;
                dropIndexRef.current = null;
                window.dragOverContainerModuleId = null;
                window.dragOverContainerId = null;
                document.querySelectorAll('.entry-modules.drag-over-container').forEach(el => {
                    el.classList.remove('drag-over-container');
                });

                return;
            }

            // Handle video file drop using video-player module
            // Note: Do not upload here; instead, store the File in a global map so
            // the VideoPlayerModule can perform the upload itself.
            if (!imageFile && !pdfFile && videoFile && entryId && journalId && droppedModule) {
                try {
                    const newModuleId = generateRandomId();

                    const newVideoModule = {
                        id: newModuleId,
                        type: 'video-player',
                        manuallyAdded: false,
                        autoUploadDroppedVideo: true
                    };

                    // Store the dropped File so the video-player module can access it
                    window.__droppedVideoFiles = window.__droppedVideoFiles || {};
                    window.__droppedVideoFiles[newModuleId] = videoFile;

                    let newModules = [...entryJson.modules];

                    const dropIndex = (dropIndexRef.current !== undefined && dropIndexRef.current !== null)
                        ? dropIndexRef.current
                        : newModules.length;

                    if (!window.dragOverContainerId || window.dragOverContainerId === 'main') {
                        newModules.splice(dropIndex, 0, newVideoModule);
                    } else {
                        const dropContainer = document.querySelector(`.entry-modules[data-id="${window.dragOverContainerId}"]`);
                        if (!dropContainer) {
                            console.error('Drop container not found for video drop');
                        } else {
                            const allContainers = getModuleHierarchyFromNode(dropContainer);
                            const allContainerModules = getAllContainerModules(allContainers);
                            newModules = addModuleToHierarchy(newModules, allContainerModules, newVideoModule, dropIndex);
                        }
                    }

                    const updatedEntryJson = { ...entryJson, modules: newModules };
                    droppedModule(updatedEntryJson, newModules);
                } catch (err) {
                    console.error('Error handling dropped video file:', err);
                }

                // Clean up drag state/classes similar to normal drop
                try {
                    e.target.classList.remove('drag-over-left');
                    e.target.classList.remove('drag-over-right');
                } catch (err) { }

                draggedModuleIdRef.current = null;
                dragOverModuleIdRef.current = null;
                dragStartIndexRef.current = null;
                dropIndexRef.current = null;
                window.dragOverContainerModuleId = null;
                window.dragOverContainerId = null;
                document.querySelectorAll('.entry-modules.drag-over-container').forEach(el => {
                    el.classList.remove('drag-over-container');
                });

                return;
            }

            // Handle non-image, non-PDF, non-video file drop using file-download module
            if (!imageFile && !pdfFile && !videoFile && otherFile && entryId && journalId && droppedModule) {
                try {
                    const newModuleId = generateRandomId();

                    const newFileModule = {
                        id: newModuleId,
                        type: 'file-download',
                        manuallyAdded: true
                    };

                    let newModules = [...entryJson.modules];

                    const dropIndex = (dropIndexRef.current !== undefined && dropIndexRef.current !== null)
                        ? dropIndexRef.current
                        : newModules.length;

                    if (!window.dragOverContainerId || window.dragOverContainerId === 'main') {
                        newModules.splice(dropIndex, 0, newFileModule);
                    } else {
                        const dropContainer = document.querySelector(`.entry-modules[data-id="${window.dragOverContainerId}"]`);
                        if (!dropContainer) {
                            console.error('Drop container not found for file drop');
                        } else {
                            const allContainers = getModuleHierarchyFromNode(dropContainer);
                            const allContainerModules = getAllContainerModules(allContainers);
                            newModules = addModuleToHierarchy(newModules, allContainerModules, newFileModule, dropIndex);
                        }
                    }

                    const uploadResult = await uploadFileForModule(session, {
                        file: otherFile,
                        journalId,
                        entryId,
                        moduleId: newModuleId
                    });

                    if (uploadResult.success) {
                        const updatedFileModule = {
                            ...newFileModule,
                            filename: uploadResult.fileName,
                            originalFilename: uploadResult.originalName,
                            fileSize: uploadResult.fileSize
                        };
                        newModules = updateModuleInHierarchy(newModules, newModuleId, updatedFileModule);
                    } else {
                        console.error('Error uploading file after drop:', uploadResult.message);
                    }

                    const updatedEntryJson = { ...entryJson, modules: newModules };
                    droppedModule(updatedEntryJson, newModules);
                } catch (err) {
                    console.error('Error handling dropped file:', err);
                }

                // Clean up drag state/classes similar to normal drop
                try {
                    e.target.classList.remove('drag-over-left');
                    e.target.classList.remove('drag-over-right');
                } catch (err) { }

                draggedModuleIdRef.current = null;
                dragOverModuleIdRef.current = null;
                dragStartIndexRef.current = null;
                dropIndexRef.current = null;
                window.dragOverContainerModuleId = null;
                window.dragOverContainerId = null;
                document.querySelectorAll('.entry-modules.drag-over-container').forEach(el => {
                    el.classList.remove('drag-over-container');
                });

                return;
            }
        }

        // Try to get drag data for cross-container drops
        let dragData = window.dragData;
        // Handle cross-container drop
        if (dragData && dragData.sourceContainerId != window.dragOverContainerId) {
            // If dropping into a nested container (tab or module-list)
            let newModules = [...entryJson.modules];
            const dropIndex = dropIndexRef.current !== undefined ? dropIndexRef.current : newModules.length;


            //remove module from source container
            newModules = removeModuleFromHierarchy(newModules, dragData.moduleId);
            
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
                droppedModule(updatedEntryJson, newModules);
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
        const containerId = node?.getAttribute('data-id') ?? 'main';
        if(containerId == 'main') node = document.querySelector('.container-main');
        // Only handle if dragging over the container itself, not over a module
        //find parent module
            let moduleNode = node;
            while (moduleNode && !moduleNode.classList?.contains('module')) {
                moduleNode = moduleNode.parentNode;
            }
            if (moduleNode || containerId == 'main') {
                const id = moduleNode?.getAttribute('data-id') || 'main';
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
    };
    //#endregion

    //#region Add/Remove/Update Module in Hierarchy

    const findModuleInHierarchy = (modules, moduleIdToFind) => {
        // Recursive function to search for a module at any level
        for (const module of modules) {
            if (module.id == moduleIdToFind) {
                return module;
            }

            // Check tabs module
            if (module.type === 'tabs' && module.tabs && Array.isArray(module.tabs)) {
                for (const tab of module.tabs) {
                    if (tab.modules && Array.isArray(tab.modules)) {
                        const found = findModuleInHierarchy(tab.modules, moduleIdToFind);
                        if (found) return found;
                    }
                }
            }

            // Check module-list module
            if (module.type === 'module-list' && module.modules && Array.isArray(module.modules)) {
                const found = findModuleInHierarchy(module.modules, moduleIdToFind);
                if (found) return found;
            }
        }

        return null;
    };

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
                if(targetModule.type == 'tabs'){
                    //tabs module type has an array of module lists
                    const tabIndex = [...document.querySelectorAll('.module[data-id="' + moduleId + '"] > .tabs-module > .tabs-toolbar .tabs-list .tab')].findIndex(a => a.classList.contains('active'));
                    if(tabIndex > -1){
                        targetModule.tabs[tabIndex].modules.splice(dropIndex, 0, moduleToAdd);
                    }
                }else{
                    //all other module types will only have one modules list
                    if (!targetModule.modules) {
                        targetModule.modules = [];
                    }
                    targetModule.modules.splice(dropIndex, 0, moduleToAdd);
                }
            } else {
                // Otherwise, continue traversing
                if (targetModule.type === 'tabs' && targetModule.tabs && Array.isArray(targetModule.tabs)) {
                    // Traverse into the active tab's modules for intermediate tabs modules
                    const tabIndex = [...document.querySelectorAll('.module[data-id="' + moduleId + '"] > .tabs-module > .tabs-toolbar .tabs-list .tab')].findIndex(a => a.classList.contains('active'));
                    if (tabIndex > -1) {
                        if (!targetModule.tabs[tabIndex].modules) {
                            targetModule.tabs[tabIndex].modules = [];
                        }
                        currentLevel = targetModule.tabs[tabIndex].modules;
                    } else {
                        console.error(`Tabs module ${moduleId} has no active tab to traverse`);
                        return modules;
                    }
                } else {
                    if (!targetModule.modules) {
                        console.error(`Module ${moduleId} has no modules array to traverse`);
                        return modules;
                    }
                    currentLevel = targetModule.modules;
                }
            }
        }

        return newModules;
    };

    const removeModuleFromHierarchy = (modules, moduleIdToRemove) => {
        // Recursive function to search and remove module from any level
        const removeFromLevel = (modulesList) => {
            // First, check if the module exists at this level
            const filteredModules = modulesList.filter(m => m.id != moduleIdToRemove);

            // If we removed a module, return the filtered list
            if (filteredModules.length !== modulesList.length) {
                return filteredModules;
            }

            // Otherwise, recursively search in nested modules
            return filteredModules.map(module => {
                // Check tabs module
                if (module.type === 'tabs' && module.tabs && Array.isArray(module.tabs)) {
                    const updatedTabs = module.tabs.map(tab => {
                        if (tab.modules && Array.isArray(tab.modules)) {
                            return {
                                ...tab,
                                modules: removeFromLevel(tab.modules)
                            };
                        }
                        return tab;
                    });
                    return { ...module, tabs: updatedTabs };
                }

                // Check module-list module
                if (module.type === 'module-list' && module.modules && Array.isArray(module.modules)) {
                    return {
                        ...module,
                        modules: removeFromLevel(module.modules)
                    };
                }

                return module;
            });
        };

        return removeFromLevel(modules);
    };

    const updateModuleInHierarchy = (modules, moduleIdToUpdate, updatedModule) => {
        // Recursive function to search and update module at any level
        const updateAtLevel = (modulesList) => {
            return modulesList.map(module => {
                // If this is the module to update, replace it
                if (module.id == moduleIdToUpdate) {
                    return updatedModule;
                }

                // Check tabs module
                if (module.type === 'tabs' && module.tabs && Array.isArray(module.tabs)) {
                    const updatedTabs = module.tabs.map(tab => {
                        if (tab.modules && Array.isArray(tab.modules)) {
                            return {
                                ...tab,
                                modules: updateAtLevel(tab.modules)
                            };
                        }
                        return tab;
                    });
                    return { ...module, tabs: updatedTabs };
                }

                // Check module-list module
                if (module.type === 'module-list' && module.modules && Array.isArray(module.modules)) {
                    return {
                        ...module,
                        modules: updateAtLevel(module.modules)
                    };
                }

                return module;
            });
        };

        return updateAtLevel(modules);
    };

    const getModuleHierarchyFromNode = (moduleNode) => {
        let node = moduleNode;
        const allContainers = [];
        while (node != null) {
            //get all containers in the hierarchy
            if (node.classList?.contains('entry-modules')) {
                const id = node.getAttribute('data-id');
                if (id != 'main') {
                    // unshift so the outermost container ends up at the start of the array
                    allContainers.unshift(id);
                }
            }
            node = node.parentNode;
        }
        // Now allContainers is ordered from outermost -> innermost, which
        // is what addModuleToHierarchy expects for traversing deeply nested modules
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
        const moduleId = node.getAttribute('data-id');
        hoveredModuleIdRef.current = moduleId;
        if (typeof window !== 'undefined') {
            window.moduleHovered = moduleId;
        }
    };

    const handleMouseLeaveContainer = (e) => {
        e.stopPropagation();
        window.mouseOverElem = null;
        window.mouseOverNode = null;
        document.querySelectorAll('.module.hover').forEach(el => {
            el.classList.remove('hover');
        });
        hoveredModuleIdRef.current = null;
        if (typeof window !== 'undefined') {
            window.moduleHovered = null;
        }
    };
    //#endregion

    //#region Events
    const areButtonSetsEqual = (prevButtons = [], nextButtons = []) => {
        if (prevButtons.length !== nextButtons.length) return false;
        for (let i = 0; i < prevButtons.length; i++) {
            const prev = prevButtons[i];
            const next = nextButtons[i];
            if (
                prev.icon !== next.icon ||
                prev.title !== next.title ||
                prev.callback !== next.callback ||
                prev.disabled !== next.disabled
            ) {
                return false;
            }
        }
        return true;
    };

    const handleSetTabButtons = useCallback((buttons, moduleId) => {
        if (!moduleId) return;
        const normalizedButtons = Array.isArray(buttons) ? buttons : [];
        const existingIndex = tabButtonsRef.current.findIndex(item => item.moduleId === moduleId);
        const nextEntry = { moduleId, buttons: normalizedButtons };

        if (existingIndex >= 0) {
            const existingEntry = tabButtonsRef.current[existingIndex];
            if (areButtonSetsEqual(existingEntry.buttons, normalizedButtons)) {
                return;
            }
            tabButtonsRef.current = [
                ...tabButtonsRef.current.slice(0, existingIndex),
                nextEntry,
                ...tabButtonsRef.current.slice(existingIndex + 1)
            ];
        } else {
            tabButtonsRef.current = [...tabButtonsRef.current, nextEntry];
        }

        if (tabButtonsTimer.current) {
            clearTimeout(tabButtonsTimer.current);
        }

        tabButtonsTimer.current = setTimeout(() => {
            setTabButtons(tabButtonsRef.current);
            tabButtonsTimer.current = null;
        }, 100);
    }, []);

    const getTabButtonsHandler = useCallback((moduleId) => {
        if (!moduleId) return () => {};
        if (!tabButtonHandlersRef.current.has(moduleId)) {
            tabButtonHandlersRef.current.set(moduleId, (buttons) => handleSetTabButtons(buttons, moduleId));
        }
        return tabButtonHandlersRef.current.get(moduleId);
    }, [handleSetTabButtons]);

    const handleDeleteListener = (module, callback) => {
        deleteListenersRef.current = [...deleteListenersRef.current, { moduleId: module.id, callback: callback }];
    };

    //#endregion

    //#region Render
    if(!entryJson) return null;
    return (
        <div
            className={`entry-modules container-${containerId}`}
            ref={containerRef}
            data-id={containerId}
            onDragOver={handleContainerDragOver}
            onPaste={isEditing && containerId === 'main' ? handleContainerPaste : undefined}
            onMouseLeave={isEditing ? handleMouseLeaveContainer : undefined}
            onDrop={isEditing && containerId == 'main' ? handleDrop : undefined}
        >
            {entryJson.modules.map((module, index) => {
                if (!module.type) return;

                // Use custom modules registry if provided, otherwise use default
                const modulesList = modulesRegistry ? [...modules, ...modulesRegistry] : modules;
                const moduleType = modulesList.find(m => m.type === module.type);
                const ModuleComponent = moduleType?.module;
                
                // Don't render if module type doesn't exist in registry
                if (!ModuleComponent) return null;
                
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
                                                        title="Add new module above"
                                                    >
                                                        <Icon name="add" />
                                                    </button>
                                                    {showModuleAboveDropdown && (
                                                        <div
                                                            className="module-dropdown"
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
                                            {filteredButtons && ( //module-defined buttons
                                                filteredButtons.buttons.map((button, index) => {
                                                    const hasLabel = button.hasLabel === true;
                                                    return (
                                                        <button
                                                            key={'module-' + module.id + '-usertab-' + module.id + '_' + index}
                                                            className={hasLabel ? '' : 'icon'}
                                                            onClick={() => button.callback()}
                                                            title={button.title}
                                                        >
                                                            <Icon name={button.icon} />
                                                            {hasLabel && (
                                                                <span>{button.title}</span>
                                                            )}
                                                        </button>
                                                    );
                                                })
                                            )}
                                            {canDelete && ( //Delete button
                                                <button className="icon" onClick={(e) => { e.stopPropagation(); removeModule(module.id); }} title="Delete module">
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
                            entry={entry}
                            journalId={journalId}
                            journal={journal}
                            chapters={chapters}
                            onUpdate={handleUpdatedModule}
                            hasUpdated={hasUpdated}
                            isEditable={isEditing}
                            manuallyAdded={module.manuallyAdded}
                            tabButtons={getTabButtonsHandler(module.id)}
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
