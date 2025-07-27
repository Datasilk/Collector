import React from 'react';
import './container.css';
/**
 * <summary>Container component</summary>
 * <description>Displays a styled container with optional title, subtitle, and error message in the header.</description>
 * @param {ReactNode} children - The content to display inside the container.
 * @param {string} title - The main title displayed at the top of the container.
 * @param {string} subtitle - A subtitle displayed below the title.
 * @param {string} error - An error message displayed in the container header.
 */
export default function Container({ children, title, subtitle, error, className }) {
    return (
        <div className={"form-wrap" + (className ? ' ' + className : '')}>
            <div className="form-container">
                <h1>{title}</h1>
                <p className="subtitle">{subtitle}</p>
                {error && <div className="error-msg">{error}</div>}
                {children}
            </div>
        </div>
    )
}

