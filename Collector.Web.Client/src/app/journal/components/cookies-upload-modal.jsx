import React, { useState, useRef } from 'react';
import Modal from '@/components/ui/modal';
import Icon from '@/components/ui/icon';
import { useSession } from '@/context/session';
import { Cookies } from '@/api/user/cookies';
import { getYouTubeCookies, cookiesToNetscapeFormat, getCookiesForDomain } from '@/helpers/cookies';

export default function CookiesUploadModal({ onClose, onSuccess }) {
    const [isDragging, setIsDragging] = useState(false);
    const [isUploading, setIsUploading] = useState(false);
    const [error, setError] = useState(null);
    const [success, setSuccess] = useState(false);
    const fileInputRef = useRef(null);

    const session = useSession();
    const { uploadCookies } = Cookies(session);
    
    // Get the current API server URL
    const apiServerUrl = window.location.origin;

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

    const handleFetchFromExtension = async () => {
        setIsUploading(true);
        setError(null);

        try {
            // Get cookies from Chrome Extension
            const cookies = await getCookiesForDomain('youtube.com');
            
            if (!cookies || cookies.length === 0) {
                setError('No YouTube cookies found. Make sure you are logged into YouTube and the Chrome Extension is installed.');
                setIsUploading(false);
                return;
            }

            // Convert to Netscape format
            const cookieContent = cookiesToNetscapeFormat(cookies);
            
            // Create a File object from the cookie content
            const blob = new Blob([cookieContent], { type: 'text/plain' });
            const file = new File([blob], 'youtube-cookies.txt', { type: 'text/plain' });

            // Upload the cookies
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
            setError(err.message || 'Failed to fetch cookies from extension');
        } finally {
            setIsUploading(false);
        }
    };

    return (
        <Modal title="Upload Browser Cookies" onClose={onClose}>
            <div className="cookies-upload-modal">
                <div className="extension-option">
                    <h4>Option 1: Use Collector Cookie Bridge Extension</h4>
                    <p>If you have the Collector Cookie Bridge extension installed and are logged into YouTube, click below to automatically fetch and upload your cookies.</p>
                    <button 
                        className="fetch-button" 
                        onClick={handleFetchFromExtension}
                        disabled={isUploading}
                    >
                        <Icon name="extension" />
                        <span>Fetch Cookies from Extension</span>
                    </button>
                    
                    <div className="extension-setup">
                        <details>
                            <summary>Don't have the extension? Click here for setup instructions</summary>
                            <div className="setup-content">
                                <h5>Installing the Collector Cookie Bridge Extension:</h5>
                                <ol>
                                    <li>Download or locate the extension folder at <code>Collector.Chrome.Extension</code></li>
                                    <li>Open Chrome and go to <code>chrome://extensions</code></li>
                                    <li>Enable <strong>"Developer mode"</strong> (toggle in top right)</li>
                                    <li>Click <strong>"Load unpacked"</strong></li>
                                    <li>Select the <code>Collector.Chrome.Extension</code> folder</li>
                                    <li>Click the extension icon in Chrome toolbar</li>
                                    <li>Click <strong>"Login"</strong> and enter your credentials</li>
                                    <li>Use this API Server URL: <code className="api-url">{apiServerUrl}</code></li>
                                    <li>Make sure you're logged into <a href="https://www.youtube.com" target="_blank" rel="noopener noreferrer">YouTube</a></li>
                                    <li>Return here and click "Fetch Cookies from Extension"</li>
                                </ol>
                            </div>
                        </details>
                    </div>
                </div>

                <div className="divider">
                    <span>OR</span>
                </div>

                <div className="instructions">
                    <h4>Option 2: Manual Export</h4>
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

                .cookies-upload-modal .extension-option {
                    margin-bottom: 1.5rem;
                    padding: 1.5rem;
                    background: var(--container-bg);
                    border: 1px solid var(--container-border);
                    border-radius: 8px;
                }

                .cookies-upload-modal .extension-option h4 {
                    margin: 0 0 0.5rem;
                    font-weight: 600;
                    color: var(--link-text);
                }

                .cookies-upload-modal .extension-option p {
                    margin: 0 0 1rem;
                    color: var(--global-text);
                    opacity: 0.8;
                }

                .cookies-upload-modal .fetch-button {
                    display: flex;
                    align-items: center;
                    gap: 0.5rem;
                    padding: 0.75rem 1.5rem;
                    background: var(--button-bg);
                    color: var(--button-text);
                    border: none;
                    border-radius: 6px;
                    font-size: 1rem;
                    font-weight: 500;
                    cursor: pointer;
                    transition: all 0.2s ease;
                }

                .cookies-upload-modal .fetch-button:hover:not(:disabled) {
                    background: var(--button-hover-bg);
                    transform: translateY(-1px);
                }

                .cookies-upload-modal .fetch-button:disabled {
                    opacity: 0.6;
                    cursor: not-allowed;
                }

                .cookies-upload-modal .extension-setup {
                    margin-top: 1rem;
                }

                .cookies-upload-modal .extension-setup details {
                    border: 1px solid var(--container-border);
                    border-radius: 6px;
                    padding: 0.75rem;
                    background: var(--page-bg);
                }

                .cookies-upload-modal .extension-setup summary {
                    cursor: pointer;
                    font-weight: 500;
                    color: var(--link-text);
                    list-style: none;
                    display: flex;
                    align-items: center;
                    gap: 0.5rem;
                }

                .cookies-upload-modal .extension-setup summary::-webkit-details-marker {
                    display: none;
                }

                .cookies-upload-modal .extension-setup summary::before {
                    content: '▶';
                    display: inline-block;
                    transition: transform 0.2s;
                }

                .cookies-upload-modal .extension-setup details[open] summary::before {
                    transform: rotate(90deg);
                }

                .cookies-upload-modal .extension-setup .setup-content {
                    margin-top: 1rem;
                    padding-top: 1rem;
                    border-top: 1px solid var(--container-border);
                }

                .cookies-upload-modal .extension-setup h5 {
                    margin: 0 0 0.75rem;
                    font-weight: 600;
                    font-size: 0.95rem;
                }

                .cookies-upload-modal .extension-setup ol {
                    margin: 0;
                    padding-left: 1.25rem;
                    line-height: 1.8;
                }

                .cookies-upload-modal .extension-setup code {
                    background: var(--container-bg);
                    padding: 0.2rem 0.4rem;
                    border-radius: 3px;
                    font-family: monospace;
                    font-size: 0.9em;
                }

                .cookies-upload-modal .extension-setup .api-url {
                    color: var(--link-text);
                    font-weight: 600;
                    user-select: all;
                }

                .cookies-upload-modal .divider {
                    display: flex;
                    align-items: center;
                    margin: 1.5rem 0;
                    text-align: center;
                }

                .cookies-upload-modal .divider::before,
                .cookies-upload-modal .divider::after {
                    content: '';
                    flex: 1;
                    border-bottom: 1px solid var(--container-border);
                }

                .cookies-upload-modal .divider span {
                    padding: 0 1rem;
                    color: var(--global-text);
                    opacity: 0.5;
                    font-size: 0.875rem;
                    font-weight: 500;
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
                    color: var(--link-text);
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
