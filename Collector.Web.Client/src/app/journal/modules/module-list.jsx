import { useState, useEffect, useRef } from 'react';
//components
import Icon from '@/components/ui/icon';
import ModuleList from '../module-list';
//modules
import modules from '../modules';

export default function ModuleListModule({ module, entryId, entry, journalId, onUpdate, isEditable = true, manuallyAdded = false }) {
    //state
    const [childModules, setChildModules] = useState(module.modules || []);
    const [showAddModuleDropdown, setShowAddModuleDropdown] = useState(false);
    const [showBottomModuleDropdown, setShowBottomModuleDropdown] = useState(false);

    //refs
    const moduleRef = useRef(module);
    const addModuleDropdownRef = useRef(null);
    const addModuleButtonRef = useRef(null);
    const bottomDropdownRef = useRef(null);
    const bottomDropdownButtonRef = useRef(null);

    //effect
    useEffect(() => {
        const handleClickOutside = (event) => {
            // Check if click is outside the top dropdown and its button
            if (showAddModuleDropdown &&
                addModuleDropdownRef.current &&
                !addModuleDropdownRef.current.contains(event.target) &&
                addModuleButtonRef.current &&
                !addModuleButtonRef.current.contains(event.target)) {
                setShowAddModuleDropdown(false);
            }

            // Check if click is outside the bottom dropdown and its button
            if (showBottomModuleDropdown &&
                bottomDropdownRef.current &&
                !bottomDropdownRef.current.contains(event.target) &&
                bottomDropdownButtonRef.current &&
                !bottomDropdownButtonRef.current.contains(event.target)) {
                setShowBottomModuleDropdown(false);
            }
        };

        document.addEventListener('mousedown', handleClickOutside);

        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
        };
    }, [showAddModuleDropdown, showBottomModuleDropdown]);

    useEffect(() => {
        moduleRef.current = module;
        setChildModules(module.modules || []);
    }, [module]);

    //actions
    const generateRandomId = () => {
        return String(Math.floor(Math.random() * 1000000));
    };

    const handleUpdatedModule = (updatedChildModule) => {
        const index = childModules.findIndex(m => m.id === updatedChildModule.id);
        
        if (index > -1) {
            const updatedModules = [...childModules];
            updatedModules[index] = {...updatedModules[index], ...updatedChildModule};
            setChildModules(updatedModules);
            
            // Update the parent module
            const updatedModule = { ...moduleRef.current, modules: updatedModules };
            onUpdate(updatedModule);
        }
    };

    const handleRemovedModule = (moduleId, updatedModules) => {
        setChildModules(updatedModules);
        
        // Update the parent module
        const updatedModule = { ...moduleRef.current, modules: updatedModules };
        onUpdate(updatedModule);
    };

    const handleAddedModule = (newModule, targetModuleId) => {
        const moduleIndex = childModules.findIndex(m => m.id === targetModuleId);
        if (moduleIndex === -1) return;

        const updatedModules = [...childModules];
        updatedModules.splice(moduleIndex, 0, newModule);
        setChildModules(updatedModules);
        
        // Update the parent module
        const updatedModule = { ...moduleRef.current, modules: updatedModules };
        onUpdate(updatedModule);
    };

    const addModuleToBottom = (type, position = 'top') => {
        const newModuleId = generateRandomId();
        const newModule = {
            id: newModuleId,
            type: type,
            manuallyAdded: true
        };

        const updatedModules = [...childModules, newModule];
        setChildModules(updatedModules);
        
        // Update the parent module
        const updatedModule = { ...moduleRef.current, modules: updatedModules };
        onUpdate(updatedModule);
        
        // Close the appropriate dropdown based on which one was used
        if (position === 'top') {
            setShowAddModuleDropdown(false);
        } else {
            setShowBottomModuleDropdown(false);
        }
    };

    return (
        <div className="module-list-module">
            {isEditable && (
                <div className="add-module-container tool-bar top-add-module">
                    <div className="align-right">
                    <button
                        ref={addModuleButtonRef}
                        onClick={() => {
                            // Close bottom dropdown if it's open
                            if (showBottomModuleDropdown) {
                                setShowBottomModuleDropdown(false);
                            }
                            setShowAddModuleDropdown(!showAddModuleDropdown);
                        }}
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
                                    onClick={() => addModuleToBottom(module.type, 'top')}
                                >
                                    <Icon name={module.icon} />
                                    <span>{module.name}</span>
                                </div>
                            ))}
                        </div>
                    )}
                    </div>
                </div>
            )}
            <ModuleList
                entryJson={{ modules: childModules }}
                entryId={entryId}
                entry={entry}
                journalId={journalId}
                isEditing={isEditable}
                canDragDrop={isEditable}
                updatedModule={handleUpdatedModule}
                addedModule={handleAddedModule}
                removedModule={handleRemovedModule}
                containerId={`module-list-${module.id}`}
            />
            
            {/* Bottom Add Content button - only shows if there are modules and editing is enabled */}
            {childModules.length > 0 && isEditable && (
                <div className="add-module-container tool-bar bottom-add-module">
                    <div className="align-right">
                        <button
                            ref={bottomDropdownButtonRef}
                            onClick={() => {
                                // Close top dropdown if it's open
                                if (showAddModuleDropdown) {
                                    setShowAddModuleDropdown(false);
                                }
                                setShowBottomModuleDropdown(!showBottomModuleDropdown);
                            }}
                        >
                            <Icon name="add" /> Add Content
                        </button>
                        {showBottomModuleDropdown && (
                            <div
                                className="module-dropdown"
                                ref={bottomDropdownRef}
                            >
                                {modules.map(module => (
                                    <div
                                        key={module.id}
                                        className="module-option"
                                        onClick={() => addModuleToBottom(module.type, 'bottom')}
                                    >
                                        <Icon name={module.icon} />
                                        <span>{module.name}</span>
                                    </div>
                                ))}
                            </div>
                        )}
                    </div>
                </div>
            )}
        </div>
    );
}
