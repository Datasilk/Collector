import React, { useState, useRef } from 'react';
import Modal from '@/components/ui/modal';
import Icon from '@/components/ui/icon';
import { useSession } from '@/context/session';
import { Cookies } from '@/api/user/cookies';

export default function CookiesUploadModal({ onClose, onSuccess }) {
    const [isDragging, setIsDragging] = useState(false);
    const [isUploading, setIsUploading] = useState(false);
    const [error, setError] = useState(null);
    const [success, setSuccess] = useState(false);
    const fileInputRef = useRef(null);

    const session = useSession();
    const { uploadCookies } = Cookies(session);

    const handleDragOver = (e) => {
        e.preventDefault();
        e.stopPropagation();
        setIsDragging(true);
    };

    const handleDragLeave = (e) => {
        e.preventDefault();
        e.stopPropagation();
        setIsDragging(false);
    };

    const handleDrop = (e) => {
        e.preventDefault();
        e.stopPropagation();
        setIsDragging(false);

        const files = e.dataTransfer.files;
        if (files && files.length > 0) {
            handleFileUpload(files[0]);
        }
    };

    const handleFileSelect = (e) => {
        const files = e.target.files;
        if (files && files.length > 0) {
            handleFileUpload(files[0]);
        }
    };

    const handleFileUpload = async (file) => {
        // Validate file type (should be .txt)
        if (!file.name.endsWith('.txt')) {
            setError('Please upload a .txt file exported from your browser');
            return;
        }

        setIsUploading(true);
        setError(null);

        try {
            const response = await uploadCookies(file);
            if (response.data?.success) {
                setSuccess(true);
                setTimeout(() => {
                    onSuccess?.();
                    onClose();
                }, 1500);
            } else {
                setError(response.data?.message || 'Failed to upload cookies');
            }
        } catch (err) {
            setError(err.message || 'Failed to upload cookies');
        } finally {
            setIsUploading(false);
        }
    };

    const handleDropAreaClick = () => {
        fileInputRef.current?.click();
    };

    return (
        <Modal title="Upload Browser Cookies" onClose={onClose}>
            <div className="cookies-upload-modal">
                <div className="instructions">
                    <h4>How to export your Chrome cookies:</h4>
                    <ol>
                        <li>Install the <a href="https://chromewebstore.google.com/detail/get-cookiestxt-locally/cclelndahbckbenkjhflpdbgdldlbecc" target="_blank"><strong>"Get cookies.txt LOCALLY"</strong></a> extension from the Chrome Web Store</li>
                        <li>Go to <a href="https://www.youtube.com" target="_blank" rel="noopener noreferrer">youtube.com</a> and make sure you're logged in</li>
                        <li>Click the extension icon and select <strong>"Export"</strong></li>
                        <li>Save the cookies.txt file to your computer</li>
                        <li>Drag and drop the file below or click to select it</li>
                    </ol>
                </div>

                <div 
                    className={`drop-area ${isDragging ? 'dragging' : ''} ${isUploading ? 'uploading' : ''} ${success ? 'success' : ''}`}
                    onDragOver={handleDragOver}
                    onDragLeave={handleDragLeave}
                    onDrop={handleDrop}
                    onClick={handleDropAreaClick}
                >
                    <input
                        type="file"
                        ref={fileInputRef}
                        onChange={handleFileSelect}
                        accept=".txt"
                        style={{ display: 'none' }}
                    />
                    {isUploading ? (
                        <div className="drop-content">
                            <Icon name="hourglass_empty" />
                            <span>Uploading...</span>
                        </div>
                    ) : success ? (
                        <div className="drop-content success">
                            <Icon name="check_circle" />
                            <span>Cookies uploaded successfully!</span>
                        </div>
                    ) : (
                        <div className="drop-content">
                            <Icon name="upload_file" />
                            <span>Drop Chrome cookies export file here</span>
                            <small>or click to select file</small>
                        </div>
                    )}
                </div>

                {error && (
                    <div className="error-message">
                        <Icon name="error" />
                        <span>{error}</span>
                    </div>
                )}

                <div className="buttons">
                    <button className="cancel" onClick={onClose}>Cancel</button>
                </div>
            </div>

            <style>{`
                .cookies-upload-modal {
                    padding: 0 1rem 1rem;
                }

                .cookies-upload-modal .instructions {
                    margin-bottom: 1.5rem;
                }

                .cookies-upload-modal .instructions h4 {
                    margin: 0 0 0.75rem;
                    font-weight: 600;
                }

                .cookies-upload-modal .instructions ol {
                    margin: 0;
                    padding-left: 1.25rem;
                    line-height: 1.8;
                }

                .cookies-upload-modal .instructions a {
                    color: var(--color-link);
                }

                .cookies-upload-modal .drop-area {
                    border: 2px dashed var(--color-border);
                    border-radius: 8px;
                    padding: 3rem 2rem;
                    text-align: center;
                    cursor: pointer;
                    transition: all 0.2s ease;
                    background: var(--color-bg-secondary);
                }

                .cookies-upload-modal .drop-area:hover {
                    border-color: var(--color-primary);
                    background: var(--color-bg-hover);
                }

                .cookies-upload-modal .drop-area.dragging {
                    border-color: var(--color-primary);
                    background: var(--color-bg-hover);
                    border-style: solid;
                }

                .cookies-upload-modal .drop-area.uploading {
                    opacity: 0.7;
                    cursor: wait;
                }

                .cookies-upload-modal .drop-area.success {
                    border-color: var(--color-success);
                    background: var(--color-success-bg);
                }

                .cookies-upload-modal .drop-content {
                    display: flex;
                    flex-direction: column;
                    align-items: center;
                    gap: 0.5rem;
                    color: var(--color-text-secondary);
                }

                .cookies-upload-modal .drop-content .icon {
                    font-size: 3rem;
                    opacity: 0.6;
                }

                .cookies-upload-modal .drop-content span {
                    font-size: 1rem;
                    font-weight: 500;
                }

                .cookies-upload-modal .drop-content small {
                    font-size: 0.85rem;
                    opacity: 0.7;
                }

                .cookies-upload-modal .drop-content.success {
                    color: var(--color-success);
                }

                .cookies-upload-modal .drop-content.success .icon {
                    opacity: 1;
                }

                .cookies-upload-modal .error-message {
                    display: flex;
                    align-items: center;
                    gap: 0.5rem;
                    margin-top: 1rem;
                    padding: 0.75rem 1rem;
                    background: var(--color-error-bg);
                    color: var(--color-error);
                    border-radius: 4px;
                }

                .cookies-upload-modal .error-message .icon {
                    font-size: 1.25rem;
                }

                .cookies-upload-modal .buttons {
                    margin-top: 1.5rem;
                    display: flex;
                    justify-content: flex-end;
                }
            `}</style>
        </Modal>
    );
}
