import React, { useEffect, useRef } from 'react';
import Icon from './icon';
import './cli.css';

const MAX_LINES = 5000;

/**
 * <summary>CLI Terminal Component</summary>
 * <description>Read-only terminal-like output with a black background, monospace font, auto-scroll and a 5000-line buffer.</description>
 */
export default function Cli({ lines = [], title = '', expanded = true, onToggle, processed = 0, failed = 0 }) {
    const containerRef = useRef(null);

    const trimmedLines = lines.length > MAX_LINES ? lines.slice(lines.length - MAX_LINES) : lines;

    useEffect(() => {
        if (containerRef.current) {
            containerRef.current.scrollTop = containerRef.current.scrollHeight;
        }
    }, [trimmedLines]);

    const getLine = (line) => {
        if (line == null) return { text: '', type: 'info' };
        if (typeof line === 'string') return { text: line, type: 'info' };
        return { text: line.text || '', type: line.type || 'info' };
    };

    const handleToggle = () => {
        if (typeof onToggle === 'function') {
            onToggle(!expanded);
        }
    };

    return (
        <div className={`cli-wrapper ${expanded ? 'expanded' : 'collapsed'}`}>
            {title && (
                <div className="cli-title-bar">
                    <span className="cli-title">{title}</span>
                    <div className="cli-title-meta">
                        {processed > 0 && <span className="cli-counter processed" title="Processed">Processed: {processed}</span>}
                        {failed > 0 && <span className="cli-counter failed" title="Failed">Failed: {failed}</span>}
                        <div className="cli-toggle icon" onClick={handleToggle} title={expanded ? 'Minimize' : 'Maximize'}>
                            <Icon name={expanded ? 'minimize' : 'maximize'} />
                        </div>
                    </div>
                </div>
            )}
            <div className="cli" ref={containerRef}>
                {trimmedLines.length === 0 && (
                    <div className="cli-line info cli-empty">Ready. Start the download worker to see output.</div>
                )}
                {trimmedLines.map((line, index) => {
                    const { text, type } = getLine(line);
                    return (
                        <div key={index} className={`cli-line ${type}`}>
                            {text}
                        </div>
                    );
                })}
            </div>
        </div>
    );
}
