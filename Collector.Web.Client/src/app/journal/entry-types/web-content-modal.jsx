import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import Modal from '@/components/ui/modal';
import Input from '@/components/forms/input';

/**
 * <summary>Web Content Modal</summary>
 * <description>Modal for creating a new entry from web content by providing a URL to scrape.</description>
 */
export default function WebContentModal({ onClose }) {
    const navigate = useNavigate();
    const [url, setUrl] = useState('');

    const handleScrape = () => {
        if (url.trim()) {
            // Navigate to new entry page with URL as query parameter
            const encodedUrl = encodeURIComponent(url.trim());
            navigate(`/journal/entry/new?url=${encodedUrl}`);
            onClose();
        }
    };

    const handleKeyDown = (e) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            handleScrape();
        }
    };

    return (
        <Modal title="Scrape Web Content" onClose={onClose}>
            <div className="modal-form">
                <Input
                    label="URL"
                    name="url"
                    value={url}
                    onInput={(e) => setUrl(e.target.value)}
                    onKeyDown={handleKeyDown}
                    placeholder="https://..."
                    autoFocus
                />

                <div className="buttons">
                    <button className="cancel" onClick={onClose}>Cancel</button>
                    <button 
                        onClick={handleScrape} 
                        disabled={!url.trim()}
                    >
                        Scrape
                    </button>
                </div>
            </div>
        </Modal>
    );
}
