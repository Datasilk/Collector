import { useState, useEffect, useRef } from 'react';
//components
import Icon from '@/components/ui/icon';
import ModuleList from '../module-list';
//modules
import modules from '../modules';

export default function ModuleListModule({ module, entryId, journalId, onUpdate, isEditable = true, manuallyAdded = false }) {
    //state
    const [childModules, setChildModules] = useState(module.modules || []);
    const [showAddModuleDropdown, setShowAddModuleDropdown] = useState(false);

    //refs
    const moduleRef = useRef(module);
    const addModuleDropdownRef = useRef(null);
    const addModuleButtonRef = useRef(null);

    //effect
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
            updatedModules[index] = updatedChildModule;
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

    const addModuleToBottom = (type) => {
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
        setShowAddModuleDropdown(false);
    };

    return (
        <div className="module-list-module">
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
                entryJson={{ modules: childModules }}
                entryId={entryId}
                journalId={journalId}
                isEditing={isEditable}
                canDragDrop={isEditable}
                updatedModule={handleUpdatedModule}
                addedModule={handleAddedModule}
                removedModule={handleRemovedModule}
                containerId={`module-list-${module.id}`}
            />
        </div>
    );
}
