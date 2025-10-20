import React, { useRef, useState, useEffect } from 'react';
//components
import Icon from '@/components/ui/icon';
//modules
import modules from './modules';

/**
 * <summary>Module List Component</summary>
 * <description>Renders a list of modules for a journal entry</description>
 */
export default function ModuleList({ 
    entryJson, 
    entryId, 
    isEditing, 
    updatedModule,
    addedModule,
    removedModule
}) {
    // state
    const [showModuleAboveDropdown, setShowModuleAboveDropdown] = useState(false);
    const [currentModuleId, setCurrentModuleId] = useState(null);

    // refs
    const moduleDropdownRef = useRef(null);
    const moduleDropdownButtonRef = useRef(null);

    // effect - handle click outside to close dropdown
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
    const generateRandomId = () => {
        return Math.floor(Math.random() * 1000000);
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

    return (
        <div className="entry-modules">
            {entryJson.modules.map((module) => {
                if (!module.type) return;
                const moduleType = modules.find(m => m.type === module.type);
                const ModuleComponent = moduleType?.module;
                return (
                    <div 
                        key={'module-' + module.id} 
                        className={
                            `module module-${module.type?.replace(' ', '-') ?? ''} ` +
                            `module-id-${module.id} ${isEditing ? 'editable' : ''}` +
                            (module.manuallyAdded ? ' manually-added' : '')
                        }
                    >
                        {isEditing && (
                            <div className="module-tab-container">
                                <div className="module-tab">
                                    <div className="module-type">{moduleType?.name}</div>
                                    <div className="box">
                                        <div className="tool-bar vertical">
                                            <button
                                                className="icon"
                                                ref={module.id === currentModuleId ? moduleDropdownButtonRef : null}
                                                onClick={() => {
                                                    setCurrentModuleId(module.id);
                                                    setShowModuleAboveDropdown(!showModuleAboveDropdown || currentModuleId !== module.id);
                                                }}
                                            >
                                                <Icon name="add" />
                                            </button>
                                            {showModuleAboveDropdown && currentModuleId === module.id && (
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
                                            <button className="icon" onClick={() => removeModule(module.id)}>
                                                <Icon name="delete" />
                                            </button>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        )}
                        <ModuleComponent 
                            module={module} 
                            entryId={entryId} 
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
