import React from 'react';
import '@/styles/forms.css';
/**
 * <summary>Select component</summary>
 * <description>Renders a dropdown/select field with support for labels, notes, error messages, and custom children/buttons.</description>
 * @param {ReactNode} children - Content to display after the select field, such as actions or links.
 * @param {string} label - The label to display above or beside the select field.
 * @param {string} name - The name attribute for the select (used in forms).
 * @param {Array<{label: string, value: string|number}>} options - The options to display in the dropdown.
 * @param {string|number} value - The currently selected value.
 * @param {boolean} required - Whether the select is required for form submission.
 * @param {boolean} isLabel - If true, renders the selected value as plain text instead of a select field.
 * @param {function} onChange - Callback for the select's onChange event.
 * @param {function} onInput - Callback for the select's onInput event.
 * @param {string} error - Error message to display below the select.
 * @param {string} note - Additional note or helper text below the select.
 * @param {ReactNode} buttons - Button elements to display alongside the select field.
 */
export default function Select({ label, name, options, value, required = false, isLabel = false, title, onChange, onInput, error, note, buttons }) {
    //render select
    const optional = {};
    if (title) optional.title = title;

    return (
        <div className="form-group">
            <div className="form-label">
                {label && <label htmlFor={name}>{label + (required ? ' *' : '')}</label>}
                {error ? <span className="error">{error}</span> : <></>}
            </div>
            {!isLabel ?
                <select
                    id={name}
                    name={name}
                    value={value}
                    onChange={onChange}
                    onInput={onInput}
                    {...optional}
                >
                    {options?.map(a => <option key={a.value} value={a.value}>{a.label}</option>)}
                </select>
                : <span className="input-islabel">{options?.find(a => a.value == value)?.label}</span>}
            {note && <span className="note">{note}</span>}
            {buttons && <span className="buttons">{buttons}</span>}
        </div>
    );
}