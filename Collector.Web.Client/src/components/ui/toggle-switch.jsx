import React from 'react';
import './toggle-switch.css';

/**
 * Toggle Switch Component
 * @param {boolean} checked - Whether the toggle is checked
 * @param {function} onChange - Function to call when toggle state changes
 * @param {string} label - Optional label for the toggle
 * @param {boolean} disabled - Whether the toggle is disabled
 * @param {string} className - Additional CSS classes
 */
export default function ToggleSwitch({ 
    checked = false, 
    onChange, 
    label = '', 
    disabled = false,
    className = '',
    name = ''
}) {
    const handleChange = (e) => {
        if (onChange && !disabled) {
            onChange(e.target.checked);
        }
    };

    return (
        <div className={`toggle-switch-container ${className} toggle-switch-${name}`}>
            {label && <span className="toggle-label">{label}</span>}
            <label className={`toggle-switch ${disabled ? 'disabled' : ''}`}>
                <input
                    type="checkbox"
                    checked={checked}
                    onChange={handleChange}
                    disabled={disabled}
                    name={name}
                />
                <span className="slider"></span>
            </label>
        </div>
    );
}
