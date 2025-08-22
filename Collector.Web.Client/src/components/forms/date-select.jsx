import React, { useState } from 'react';
import '@/styles/forms.css';
import Input from '@/components/forms/input';
/**
 * <summary>Date select component</summary>
 * <description>Displays a dropdown for selecting preset or custom date ranges.</description>
 * @param {function} onChange - Callback when the selected date range changes.
 * @param {string} label - Label to display above the date select.
 * @param {string} error - Error message to display below the date select.
 */
export default function DateSelect({ onChange, label, error }) {
    const [customDates, setCustomDates] = useState({ startDate: '', endDate: '' });
    const [isCustomSelected, setIsCustomSelected] = useState(false); // Track if "Custom..." is selected
    const [customError, setCustomError] = useState(''); // Track custom error message

    const options = [
        { name: '30 Days', startDate: new Date(new Date().setDate(new Date().getDate() - 30)), endDate: null },
        { name: 'This Month', startDate: new Date(new Date().getFullYear(), new Date().getMonth(), 1), endDate: null },
        { name: '3 Months', startDate: new Date(new Date().setDate(new Date().getDate() - 90)), endDate: null },
        { name: '1 Year', startDate: new Date(new Date().setFullYear(new Date().getFullYear() - 1)), endDate: null },
        { name: 'Year to Date', startDate: new Date(new Date().getFullYear(), 0, 1), endDate: null },
        { name: 'Custom...', startDate: null, endDate: null },
    ];

    const handleOptionChange = (e) => {
        const selectedOption = options[e.target.value];
        if (selectedOption.name === 'Custom...') {
            setCustomDates({ startDate: '', endDate: '' });
            setIsCustomSelected(true); // Mark "Custom..." as selected
            setCustomError(''); // Clear any previous error
        } else {
            setIsCustomSelected(false); // Reset custom selection
            setCustomError(''); // Clear any previous error
            onChange({ startDate: selectedOption.startDate, endDate: selectedOption.endDate });
        }
    };

    const handleCustomDateChange = (field, value) => {
        setCustomDates((prevDates) => ({ ...prevDates, [field]: value }));
        setCustomError(''); // Clear error when user modifies input
    };

    const handleCustomDateSubmit = () => {
        const { startDate, endDate } = customDates;
        if (startDate && endDate) {
            if (new Date(endDate) >= new Date(startDate)) {
                onChange({ startDate, endDate });
                setCustomError(''); // Clear error on successful validation
            } else {
                setCustomError('End date cannot be before start date.');
            }
        } else {
            setCustomError('Both start date and end date are required.');
        }
    };

    return (
        <div className="form-group">
            <div className="form-label">
                {label && <label>{label}</label>}
                {error ? <span className="error">{error}</span> : null}
            </div>
            <select onChange={handleOptionChange}>
                {options.map((option, index) => (
                    <option key={index} value={index}>
                        {option.name}
                    </option>
                ))}
            </select>
            {isCustomSelected && (
                <>
                    <Input  
                        type="date"
                        value={customDates.startDate}
                        onInput={(e) => handleCustomDateChange('startDate', e.target.value)}
                        placeholder="Start Date"
                    />
                    <Input
                        type="date"
                        value={customDates.endDate}
                        onInput={(e) => handleCustomDateChange('endDate', e.target.value)}
                        placeholder="End Date"
                    />
                    <button onClick={handleCustomDateSubmit}>Submit</button>
                    {customError && <span className="error">{customError}</span>}
                </>
            )}
        </div>
    );
}
