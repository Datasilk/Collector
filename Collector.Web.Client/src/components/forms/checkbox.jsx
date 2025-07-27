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
export default function Checkbox({ label, name, checked, onChange, onInput, required = false, error, isLabel = false, iconUnchecked = null, iconChecked = null }) {
    //render input
    const [checkedState, setCheckedState] = useState(checked);
    const handleChange = () => {
        onChange(!checkedState);
        setCheckedState(!checkedState);
    }
    
    return (
        <div className={"form-group has-checkbox input-" + name + (checkedState ? ' is-checked' : '')} onClick={handleChange}>
            <div className="form-label">
                {label && <label htmlFor={name}>
                    {!isLabel && !iconUnchecked && !iconChecked &&
                        <input
                            type="checkbox"
                            id={name}
                            name={name}
                            checked={checkedState}
                            onInput={onInput}
                            readOnly
                        />
                    }
                    {!isLabel && iconUnchecked && iconChecked && checkedState && iconChecked}
                    {!isLabel && iconUnchecked && iconChecked && !checkedState && iconUnchecked}
                    {label}{required ? ' *' : ''}
                </label>}
                {error ? <span className="error">{error}</span> : <></>}
            </div>

            {isLabel && <span className="input-islabel">{checkedState ? 'Yes' : 'No'}</span>}

        </div>
    );
}