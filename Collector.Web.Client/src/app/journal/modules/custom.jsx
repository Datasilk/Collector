import React, { useState, useEffect, useRef, useCallback } from 'react';
import Icon from '@/components/ui/icon';
import Select from '@/components/forms/select';
import Modal from '@/components/ui/modal';
import ModuleList from '@/app/journal/module-list';
import { useSession } from '@/context/session';
import { Journals } from '@/api/user/journals';

export default function CustomModule({ module, onUpdate, isEditable = true, entryId, journalId, entry, journal, chapters = [], tabButtons, fromSnapshotId = null }) {
    const session = useSession();
    const { getEntries, getEntryContent } = Journals(session);

    const [customModuleEntries, setCustomModuleEntries] = useState([]);
    const [selectedEntryId, setSelectedEntryId] = useState(module.customId || null);
    const [editingIndex, setEditingIndex] = useState(null);
    const [hoverIndex, setHoverIndex] = useState(null);
    const [columnsPerRow, setColumnsPerRow] = useState(module.columnsPerRow || 1);
    const hasLoadedInitialContent = useRef(false);
    const hasLoadedTemplate = useRef(false);
    const customIdRef = useRef(null);
    const moduleRef = useRef(module);
    
    // Keep moduleRef in sync with module prop
    useEffect(() => {
        moduleRef.current = module;
    }, [module]);

    useEffect(() => {
        // Load entries when journal ID becomes available and module doesn't have a customId yet
        if (session.customModulesJournalId && !module.customId && customModuleEntries.length === 0) {
            loadCustomModuleEntries(session.customModulesJournalId);
        }
    }, [session.customModulesJournalId, module.customId]);

    useEffect(() => {
        if (!module.customId) return;
        if(customIdRef.current === module.customId) return;
        customIdRef.current = module.customId;
        
        // If items is empty, load the entry content (only once)
        if ((!module.items || module.items.length === 0) && !hasLoadedInitialContent.current) {
            hasLoadedInitialContent.current = true;
            loadAndAddEntryContent(module.customId);
            return;
        }
        
        // If items have parentId, load template and merge (only once)
        const hasItemsWithParentId = module.items && module.items.length > 0 && 
            module.items.some(item => 
                item.modules && item.modules.some(m => m.parentId)
            );
        
        if (hasItemsWithParentId && !hasLoadedTemplate.current) {
            hasLoadedTemplate.current = true;
            loadTemplateAndMerge(module.customId);
        }
    }, [module.customId]);


    const loadTemplateAndMerge = async (eId) => {
        try {
            const response = await getEntryContent(eId);
            if (response.data.success) {
                const content = JSON.parse(response.data.data);
                const templateModules = content && content.modules ? content.modules : [];
                if (templateModules.length === 0) return;

                // Apply template to existing items
                const mergedItems = applyTemplateToItems(templateModules);

                // Update parent module with merged items
                onUpdate({
                    ...module,
                    items: mergedItems
                });
            }
        } catch (err) {
            console.error('Error loading template and merging:', err);
        }
    };

    const loadCustomModuleEntries = async (jId) => {
        try {
            const response = await getEntries(jId);
            if (response.data.success) {
                setCustomModuleEntries(response.data.data || []);
            } else {
                console.error('getEntries failed:', response.data.message);
            }
        } catch (err) {
            console.error('Error loading custom module entries:', err);
        }
    };

    const generateRandomId = () => {
        return String(Math.floor(Math.random() * 999999));
    };

    const replaceModuleIdsRecursively = (modules) => {
        if (!modules || !Array.isArray(modules)) return modules;
        
        return modules.map(module => {
            const newModule = {
                ...module,
                id: generateRandomId(),
                parentId: module.id
            };
            
            // Recursively replace IDs in nested modules array
            if (newModule.modules && Array.isArray(newModule.modules)) {
                newModule.modules = replaceModuleIdsRecursively(newModule.modules);
            }
            
            return newModule;
        });
    };

    // Find a module by ID in the hierarchy recursively
    const findModuleById = (modules, targetId) => {
        if (!modules || !Array.isArray(modules)) return null;
        
        for (const module of modules) {
            if (module.id === targetId) {
                return module;
            }
            
            // Search in nested modules
            if (module.modules && Array.isArray(module.modules)) {
                const found = findModuleById(module.modules, targetId);
                if (found) return found;
            }
        }
        
        return null;
    };

    // Find a custom module by parentId in the custom modules array
    const findCustomModuleByParentId = (customModules, parentId) => {
        if (!customModules || !Array.isArray(customModules)) return null;
        
        for (const customModule of customModules) {
            if (customModule.parentId === parentId) {
                return customModule;
            }
            
            // Search in nested modules
            if (customModule.modules && Array.isArray(customModule.modules)) {
                const found = findCustomModuleByParentId(customModule.modules, parentId);
                if (found) return found;
            }
        }
        
        return null;
    };

    // Merge template module with matching custom module data based on parentId
    const mergeTemplateWithCustom = (templateModule, customModules) => {
        // Find matching custom module by parentId (where parentId matches template module's id)
        const customModule = findCustomModuleByParentId(customModules, templateModule.id);
        
        if (!customModule) {
            // No matching custom module, return template as-is
            return {
                ...templateModule,
                id: generateRandomId(),
                parentId: templateModule.id
            };
        }
        
        // Replace template module contents with custom module contents
        const merged = {
            ...templateModule,
            ...customModule,
            parentId: templateModule.id
        };
        
        // Recursively merge nested modules
        if (templateModule.modules && Array.isArray(templateModule.modules)) {
            merged.modules = templateModule.modules.map(childTemplate => 
                mergeTemplateWithCustom(childTemplate, customModules)
            );
        }
        
        return merged;
    };

    // Apply template to all items in the custom module
    const applyTemplateToItems = (templateModules) => {
        if (!module.items || !Array.isArray(module.items)) return module.items;
        
        return module.items.map(item => {
            // Use template as ground truth, merge with custom modules from this item
            const mergedModules = templateModules.map(templateModule => 
                mergeTemplateWithCustom(templateModule, item.modules)
            );

            return {
                ...item,
                modules: mergedModules
            };
        });
    };

    const loadAndAddEntryContent = useCallback(async (eId) => {
        try {
            const response = await getEntryContent(eId);
            if (response.data.success) {
                const content = JSON.parse(response.data.data);
                const modules = content && content.modules ? content.modules : [];

                // Replace all module IDs with random IDs recursively
                const modulesWithNewIds = replaceModuleIdsRecursively(modules);

                const newItem = {
                    modules: modulesWithNewIds
                };

                const currentModule = moduleRef.current;
                const updatedItems = [...(currentModule.items || []), newItem];

                // Update parent module
                onUpdate({
                    ...currentModule,
                    customId: eId,
                    items: updatedItems
                });
            }
        } catch (err) {
            console.error('Error loading and adding entry content:', err);
        }
    }, [getEntryContent, onUpdate]);

    const handleNewCustomModule = useCallback(() => {
        if (!session.customModulesJournalId) {
            console.error('Custom modules journal not loaded yet');
            return;
        }

        session.showModal(() => (
            <Modal
                title="New Custom Module"
                onClose={() => {
                    loadCustomModuleEntries(session.customModulesJournalId);
                    session.hideModal();
                }}
                wide={true}
                className="custom-module-modal"
            >
                <IframeCustomModuleModal
                    journalId={session.customModulesJournalId}
                    entryId="new"
                    onSave={handleSaveNewCustomModule}
                    onCancel={() => {
                        loadCustomModuleEntries(session.customModulesJournalId);
                        session.hideModal();
                    }}
                />
            </Modal>
        ));
    }, [session]);

    const handleSaveNewCustomModule = async (eId) => {
        try {
            await loadCustomModuleEntries(session.customModulesJournalId);
            await loadAndAddEntryContent(eId);
            session.hideModal();
        } catch (err) {
            console.error('Error saving custom module:', err);
        }
    };

    const handleSelectChange = async (e) => {
        const eId = e.target.value;
        setSelectedEntryId(eId);
        await loadAndAddEntryContent(eId);
    };

    const handleUpdatedModule = useCallback((itemIndex, updatedChildModule) => {
        if (!onUpdate) return;
        
        const currentModule = moduleRef.current;
        if (!currentModule.items || !currentModule.items[itemIndex]) return;
        
        const { manuallyAdded, ...cleanModule } = updatedChildModule;
        const moduleIndex = currentModule.items[itemIndex].modules.findIndex(m => m.id === cleanModule.id);
        
        if (moduleIndex === -1) return;
        
        const updatedModules = [...currentModule.items[itemIndex].modules];
        updatedModules[moduleIndex] = cleanModule;
        
        const updatedItems = [...currentModule.items];
        updatedItems[itemIndex] = {
            ...currentModule.items[itemIndex],
            modules: updatedModules
        };
        
        onUpdate({
            ...currentModule,
            items: updatedItems
        });
    }, [onUpdate]);

    const handleAddModuleList = useCallback(() => {
        const currentModule = moduleRef.current;
        if (currentModule.customId) {
            loadAndAddEntryContent(currentModule.customId);
        }
    }, [loadAndAddEntryContent]);

    const handleShowSettingsModal = useCallback(async () => {
        // Load custom module entries when opening settings modal
        let entries = customModuleEntries;
        if (session.customModulesJournalId) {
            try {
                const response = await getEntries(session.customModulesJournalId);
                if (response.data.success) {
                    entries = response.data.data || [];
                    setCustomModuleEntries(entries);
                }
            } catch (err) {
                console.error('Error loading custom module entries:', err);
            }
        }
        
        session.showModal(() => (
            <Modal
                title="Custom Module Settings"
                onClose={() => session.hideModal()}
            >
                <div className="custom-module-settings">
                    <Select
                        label="Columns Per Row"
                        name="columnsPerRow"
                        options={[
                            { label: '1 Column', value: 1 },
                            { label: '2 Columns', value: 2 },
                            { label: '3 Columns', value: 3 },
                            { label: '4 Columns', value: 4 },
                            { label: '5 Columns', value: 5 },
                            { label: '6 Columns', value: 6 },
                            { label: '7 Columns', value: 7 },
                            { label: '8 Columns', value: 8 },
                            { label: '9 Columns', value: 9 },
                            { label: '10 Columns', value: 10 },
                            { label: '11 Columns', value: 11 },
                            { label: '12 Columns', value: 12 }
                        ]}
                        value={columnsPerRow}
                        onChange={(e) => handleSaveSettings(parseInt(e.target.value))}
                    />

                    <div className="custom-module-templates">
                        <h3>Available Templates</h3>
                        {entries.length === 0 ? (
                            <p>No custom module templates found.</p>
                        ) : (
                            <table className="spreadsheet">
                                <thead>
                                    <tr>
                                        <th>Module Name</th>
                                        <th>Actions</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {entries.map(entry => (
                                        <tr key={entry.id}>
                                            <td>{entry.title || 'Untitled'}</td>
                                            <td className="tool-bar">
                                                <a 
                                                    href={`/journal/${session.customModulesJournalId}/entry/${entry.id}`}
                                                    target="_blank"
                                                    rel="noopener noreferrer"
                                                    className="button icon"
                                                >
                                                    <Icon name="open_in_new" />
                                                </a>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        )}
                    </div>
                </div>
            </Modal>
        ));
    }, [session, columnsPerRow, customModuleEntries]);

    const handleSaveSettings = useCallback((newColumnsPerRow) => {
        setColumnsPerRow(newColumnsPerRow);
        onUpdate({
            ...moduleRef.current,
            columnsPerRow: newColumnsPerRow
        });
        session.hideModal();
    }, [onUpdate, session]);

    const handleModuleListClick = (index) => {
        if (isEditable) {
            setEditingIndex(index);
            document.addEventListener('click', deselectModuleListItem);
        }
    };

    const deselectModuleListItem = useCallback((e) => {
        var node = e.target;
        while (node && node !== document.body) {
            if (node.classList && (
                node.classList.contains('custom-module-' + module.id) ||
                node.classList.contains('ck')
            )) {
                return;
            }
            node = node.parentNode;
        }
        document.removeEventListener('click', deselectModuleListItem);
        setEditingIndex(null);
    }, [module.id]);

    const handleModuleListOver = (index) => {
        setHoverIndex(index);
    };

    const handleModuleListLeave = () => {
        setHoverIndex(null);
    };

    useEffect(() => {
        if (typeof tabButtons !== 'function') return;
        const buttons = [];
        if (isEditable) {
            buttons.push({
                icon: 'add_photo_alternate',
                title: 'Add custom module item',
                callback: handleAddModuleList
            });
            buttons.push({
                icon: 'settings',
                title: 'Custom module settings',
                callback: handleShowSettingsModal
            });
        }
        tabButtons(buttons);
    }, [isEditable]);

    const hasCustomModules = customModuleEntries.length > 0;
    const hasSelectedModule = selectedEntryId && module.items && module.items.length > 0;

    if (!hasSelectedModule) {
        return (
            <div className="custom-module-empty">
                <div className="custom-module-empty-message">
                    {hasCustomModules
                        ? 'Create a new custom module or choose one from the drop down below.'
                        : 'Create a new custom module'}
                </div>
                <div className="custom-module-controls">
                    <button onClick={handleNewCustomModule}>
                        <Icon name="add" /> New Custom Module
                    </button>
                    {hasCustomModules && (
                        <Select
                            name="custom-module-select"
                            options={[
                                { label: 'Select a custom module...', value: '' },
                                ...customModuleEntries.map(entry => ({ label: entry.title, value: entry.id }))
                            ]}
                            value={selectedEntryId || ''}
                            onChange={handleSelectChange}
                        />
                    )}
                </div>
            </div>
        );
    }
    
    return (
        <div className="custom-module-container">
            {(module.items || []).map((item, index) => (
                <div 
                    key={index} 
                    className={
                        `custom-module-list-wrapper cols-${columnsPerRow}` + 
                        `${editingIndex === index ? ' editing' : ''}` +
                        `${hoverIndex === index ? ' hover' : ''} ` +
                        `custom-module-${module.id}`}
                    onClick={isEditable ? () => handleModuleListClick(index) : null}
                    onMouseOver={isEditable ? () => handleModuleListOver(index) : null}
                    onMouseLeave={isEditable ? () => handleModuleListLeave() : null}
                >
                    <ModuleList
                        entryJson={item}
                        entryId={entryId}
                        entry={entry}
                        journalId={journalId}
                        journal={journal}
                        chapters={chapters}
                        isEditing={isEditable && editingIndex === index}
                        showHoverTab={true}
                        showHoverOutline={true}
                        canAddAbove={false}
                        canDelete={false}
                        canResize={false}
                        canDragDrop={false}
                        updatedModule={(updatedModule) => handleUpdatedModule(index, updatedModule)}
                        containerId={`custom-${module.id}-${index}`}
                        fromSnapshotId={fromSnapshotId}
                    />
                </div>
            ))}
        </div>
    );
}

function IframeCustomModuleModal({ journalId, entryId, onSave, onCancel }) {
    const iframeRef = useRef(null);
    const [currentEntryId, setCurrentEntryId] = useState(entryId);

    useEffect(() => {
        // Listen for URL changes in the iframe
        const checkIframeUrl = setInterval(() => {
            try {
                if (iframeRef.current && iframeRef.current.contentWindow) {
                    const iframeUrl = iframeRef.current.contentWindow.location.href;
                    const match = iframeUrl.match(/\/journal\/\d+\/entry\/([a-f0-9-]+)/i);
                    if (match && match[1] && match[1] !== 'new') {
                        setCurrentEntryId(match[1]);
                    }
                }
            } catch (err) {
                // Cross-origin error, ignore
            }
        }, 500);

        return () => clearInterval(checkIframeUrl);
    }, []);

    const handleSave = () => {
        if (currentEntryId && currentEntryId !== 'new') {
            onSave(currentEntryId);
        } else {
            alert('Please wait for the entry to be created');
        }
    };

    const iframeUrl = `/journal/${journalId}/entry/${entryId}?noui&edit`;

    return (
        <div className="custom-module-modal-container">
            <div className="custom-module-modal-content" style={{ padding: 0, height: '70vh' }}>
                <iframe
                    ref={iframeRef}
                    src={iframeUrl}
                    style={{
                        width: '100%',
                        height: '100%',
                        border: 'none'
                    }}
                    title="Custom Module Editor"
                />
            </div>
            <div className="buttons custom-module-modal-footer">
                <button onClick={handleSave}>Save Custom Module</button>
                <button className="cancel" onClick={onCancel}>Cancel</button>
            </div>
        </div>
    );
}
