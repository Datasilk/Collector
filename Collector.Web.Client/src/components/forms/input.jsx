import React from 'react';
import '@/styles/forms.css';

/**
 * <summary>Input component for text fields</summary>
 * <description>Renders a text input field with support for labels, notes, error messages, and custom children/buttons.</description>
 * @param {ReactNode} children - Content to display after the input field, such as actions or links.
 * @param {string} label - The label to display above or beside the input field.
 * @param {string} name - The name attribute for the input (used in forms).
 * @param {string} type - The HTML input type (e.g., text, email, password, number, date, etc.).
 * @param {string|number} value - The current value of the input field.
 * @param {string} placeholder - Placeholder text shown when the input is empty.
 * @param {number} maxLength - Maximum number of characters allowed.
 * @param {string|boolean} autocomplete - The autocomplete attribute for browser autofill (true for default, or a string value).
 * @param {boolean} required - Whether the input is required for form submission.
 * @param {boolean} isLabel - If true, renders the value as plain text instead of an input field.
 * @param {function} onChange - Callback for the input's onChange event.
 * @param {function} onInput - Callback for the input's onInput event.
 * @param {string} error - Error message to display below the input.
 * @param {string} note - Additional note or helper text below the input.
 * @param {ReactNode} buttons - Button elements to display alongside the input field.
 */
export default function Input({ children, label, name, type = 'text', value, placeholder, maxLength = null, autocomplete = null, required = false, isLabel = false, onChange, onInput, error, note, buttons }) {
    //set up optional properties
    const optional = {};
    if (autocomplete === true || (['email', 'password'].some(a => a == type) && autocomplete === null)) {
        optional.autoComplete = 'true';
    }
    if (maxLength) {
        optional.maxLength = maxLength;
    }

    //render input
    return (
        <div className={"form-group input-" + name}>
            <div className="form-label">
                {label && <label htmlFor={name}>{label}{required ? ' *' : ''}</label>}
                {error && <span className="error">{error}</span>}
            </div>
            <div className="form-field">
                {!isLabel ?
                    <input
                        type={type}
                        id={name}
                        name={name}
                        value={value}
                        onChange={onChange}
                        onInput={onInput}
                        placeholder={placeholder}
                        {...optional}
                    />
                    : <span className="input-islabel">{value}</span>}
                {note && <span className="note">{note}</span>}
                {children}
                {buttons && <span className="buttons">{buttons}</span>}
            </div>
        </div>
    );
}