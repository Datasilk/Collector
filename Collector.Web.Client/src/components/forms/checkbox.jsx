import React, { useState } from 'react';
/**
 * <summary>Checkbox component</summary>
 * <description>Renders a checkbox input field with label, error, and other features.</description>
 * @param {string} name - The name attribute for the checkbox (used in forms).
 * @param {string} label - The label to display next to the checkbox.
 * @param {boolean} checked - Whether the checkbox is checked.
 * @param {function} onChange - Callback for the checkbox's onChange event.
 * @param {function} onInput - Callback for the checkbox's onInput event.
 * @param {boolean} required - Whether the checkbox is required for form submission.
 * @param {string} error - Error message to display below the checkbox.
 * @param {boolean} isLabel - If true, renders the checked state as plain text instead of a checkbox.
 */
export default function Checkbox({ 
    label, 
    name, 
    id, 
    checked, 
    onChange, 
    onInput, 
    required = false, 
    error, 
    isLabel = false, 
    iconUnchecked = null, 
    iconChecked = null 
}) {
    const inputId = id || name;
    
    const handleChange = (e) => {
        if (onChange) {
            onChange(e.target.checked);
        }
    };
    
    return (
        <div className={"form-group has-checkbox input-" + name + (checked ? ' is-checked' : '')}>
            <div className="form-label">
                {label && (
                    <label htmlFor={inputId}>
                        {!isLabel && !iconUnchecked && !iconChecked && (
                            <input
                                type="checkbox"
                                id={inputId}
                                name={name}
                                checked={checked}
                                onChange={handleChange}
                                onInput={onInput}
                                className="checkbox-input"
                            />
                        )}
                        {!isLabel && iconUnchecked && iconChecked && (checked ? iconChecked : iconUnchecked)}
                        <span className="checkbox-label">
                            {label}{required ? ' *' : ''}
                        </span>
                    </label>
                )}
                {error && <span className="error">{error}</span>}
            </div>

            {isLabel && <span className="input-islabel">{checked ? 'Yes' : 'No'}</span>}
        </div>
    );
}