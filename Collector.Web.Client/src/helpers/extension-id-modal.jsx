import React, { useState } from 'react';
import Modal from '@/components/ui/modal';
import Input from '@/components/forms/input';

export default function ExtensionIdModal({ onSave, onCancel }) {
    const [extensionId, setExtensionId] = useState('');
    const [error, setError] = useState('');

    const handleExtensionIdInput = (e) => {
        setExtensionId(e.target.value);
        setError('');
    };

    const handleSave = () => {
        const id = extensionId.trim();
        if (!/^[a-p]{32}$/i.test(id)) {
            setError('Paste the 32-character extension ID from the extension details page');
            return;
        }
        onSave?.(id);
    };

    return (
        <Modal title="Collector Cookie Bridge Extension" onClose={onCancel}>
            <div className="extension-id-modal">
                <p>
                    Make sure you installed the Chrome Extension under <code>Collector.Chrome.Extension</code>.
                    Then in Chrome, go to <strong>Extensions &gt; Manage Extensions</strong>, find the
                    <strong> Collector Cookie Bridge</strong> extension, and copy the ID from the extension
                    details page before continuing.
                </p>
                <Input
                    label="Extension ID"
                    name="extension-id"
                    value={extensionId}
                    onInput={handleExtensionIdInput}
                    error={error}
                    wide={true}
                />
                <div className="buttons">
                    <button className="submit" onClick={handleSave}>Save &amp; Continue</button>
                    <button className="cancel" onClick={onCancel}>Cancel</button>
                </div>
            </div>
            <style>{`
                .extension-id-modal p {
                    margin: 0 0 1rem;
                    color: var(--global-text);
                    line-height: 1.5;
                }

                .extension-id-modal code {
                    background: var(--container-bg);
                    padding: 0.2rem 0.4rem;
                    border-radius: 3px;
                    font-family: monospace;
                    font-size: 0.9em;
                }

                .extension-id-modal .buttons {
                    margin-top: 1rem;
                }

                .extension-id-modal .buttons button,
                .extension-id-modal .buttons button:hover {
                    background: transparent;
                    border-color: var(--link-text);
                    color: var(--link-text);
                }

                .extension-id-modal .buttons button.cancel,
                .extension-id-modal .buttons button.cancel:hover {
                    border-color: var(--button-cancel-bg);
                    color: var(--button-cancel-bg);
                }
            `}</style>
        </Modal>
    );
}
