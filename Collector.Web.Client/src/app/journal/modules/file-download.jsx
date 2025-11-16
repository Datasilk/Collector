import { useState, useRef, useEffect } from 'react';
//components
import Icon from '@/components/ui/icon';
import Modal from '@/components/ui/modal';
import FileDownloadModal from '../components/file-download-modal';
//context
import { useSession } from '@/context/session';
//api
import { Files } from '@/api/user/files';
import { JournalFiles } from '@/api/user/journalFiles';
//helpers
import { apiBasePath } from '@/helpers/endpoints';

export default function FileDownloadModule({ module, entryId, journalId, onUpdate, isEditable = true, manuallyAdded = false, setDeleteListener, tabButtons }) {
    //state
    const [isUploading, setIsUploading] = useState(false);
    const [uploadError, setUploadError] = useState(null);
    
    //refs
    const fileInputRef = useRef(null);
    const moduleRef = useRef(module);
    
    //context
    const session = useSession();
    const { upload } = Files(session);
    const { add: addFileMetadata, delete: deleteFileMetadata } = JournalFiles(session);
    
    // Auto-trigger file dialog when module is manually added
    useEffect(() => {
        if (manuallyAdded && isEditable && fileInputRef.current) {
            // Small delay to ensure the DOM is fully rendered
            const timer = setTimeout(() => {
                fileInputRef.current.click();
            }, 100);
            
            return () => clearTimeout(timer);
        }
    }, [manuallyAdded, isEditable]);

    useEffect(() => {
        if (!setDeleteListener) return;
        setDeleteListener(module, removeModule);
    }, [module.id]);

    useEffect(() => {
        moduleRef.current = module;
        if (tabButtons) {
            tabButtons([
                {
                    icon: 'settings',
                    title: 'Settings',
                    callback: handleShowSettingsModal
                }
            ]);
        }
    }, [module]);
    
    const handleFileChange = async (e) => {
        if (!isEditable) return;
        
        const file = e.target.files[0];
        if (!file) return;
        
        setIsUploading(true);
        setUploadError(null);
        
        try {
            // Generate a unique filename using timestamp and original filename
            const timestamp = new Date().getTime();
            const fileName = `${timestamp}-${file.name}`;
            
            // Path format: {journalId}/{entryId}/{filename}
            const path = `${journalId}/${entryId}/${fileName}`;
            
            const response = await upload(path, file);
            
            if (response.data.success) {
                try {
                    // Save file metadata to JournalFiles table
                    const fileMetadata = {
                        JournalId: journalId,
                        JournalEntryId: entryId,
                        ModuleId: module.id,
                        Filename: fileName,
                        FileSize: file.size,
                        DateUploaded: new Date().toISOString()
                    };
                    
                    await addFileMetadata(fileMetadata);
                    
                    // Update the module with the file info
                    onUpdate({ 
                        ...module, 
                        filename: fileName,
                        originalFilename: file.name,
                        fileSize: file.size
                    });
                } catch (error) {
                    console.error('Error saving file metadata:', error);
                    // Still update the module even if metadata save fails
                    onUpdate({ 
                        ...module, 
                        filename: fileName,
                        originalFilename: file.name,
                        fileSize: file.size
                    });
                }
            } else {
                setUploadError(response.data.message || 'Failed to upload file');
            }
        } catch (error) {
            console.error('Error uploading file:', error);
            setUploadError('An error occurred while uploading the file');
        } finally {
            setIsUploading(false);
        }
    };

    const removeModule = (moduleItem) => {
        if (deleteFileMetadata) {
            deleteFileMetadata(entryId, moduleItem.id);
        }
    };

    const formatFileSize = (bytes) => {
        if (!bytes) return '0 MB';
        const mb = bytes / (1024 * 1024);
        return mb.toFixed(2) + ' MB';
    };

    const handleShowSettingsModal = () => {
        // Pass a function that returns the component, using moduleRef to get the latest value
        session.showModal(() => (
            <FileDownloadModal 
                module={moduleRef.current}
                onUpdate={onUpdate}
                onClose={() => session.hideModal()}
            />
        ));
    };

    const handleDownloadClick = (e) => {
        e.preventDefault();
        
        const downloadUrl = `${apiBasePath()}/file/${journalId}/${entryId}/${module.filename}`;
        
        session.showModal(
            <Modal title="Download Warning" onClose={() => session.hideModal()}>
                <div style={{ padding: '1em' }}>
                    <p style={{ marginBottom: '1em' }}>
                        <strong>Please read the following warnings before downloading:</strong>
                    </p>
                    <ul style={{ marginLeft: '1.5em', marginBottom: '1em' }}>
                        <li style={{ marginBottom: '0.5em' }}>
                            This file has <strong>not been scanned for viruses or malware</strong>
                        </li>
                        <li style={{ marginBottom: '0.5em' }}>
                            Only download files from sources you trust
                        </li>
                        <li style={{ marginBottom: '0.5em' }}>
                            Scan the file with your antivirus software before opening
                        </li>
                        <li style={{ marginBottom: '0.5em' }}>
                            Be cautious of executable files (.exe, .bat, .cmd, .sh, etc.)
                        </li>
                    </ul>
                    <p style={{ marginBottom: '1.5em', color: '#d9534f' }}>
                        <strong>Download at your own risk.</strong>
                    </p>
                    <div className="buttons">
                        <a 
                            href={downloadUrl}
                            className="button"
                            onClick={() => session.hideModal()}
                            download
                        >
                            <Icon name="download" />
                            Download Anyway
                        </a>
                        <button 
                            className="cancel"
                            onClick={() => session.hideModal()}
                        >
                            Cancel
                        </button>
                    </div>
                </div>
            </Modal>
        );
    };
    
    return (
        <div className="file-download-module">
            {isEditable && !module.filename && (
                <div className="tool-bar">
                    <div className="left-side">
                        <div className="file-upload-button-container">
                            <button disabled={isUploading}>
                                <Icon name="upload" />
                                {isUploading ? 'Uploading...' : 'Upload File'}
                            </button>
                            <input 
                                type="file" 
                                ref={fileInputRef}
                                onChange={handleFileChange}
                                disabled={isUploading}
                            />
                        </div>
                    </div>
                </div>
            )}
            
            {uploadError && (
                <div className="error-message" style={{ color: 'red', marginTop: '0.5em' }}>
                    {uploadError}
                </div>
            )}
            
            {module.filename && (
                <div className="file-download-preview">
                    <a 
                        className={`${module.style || 'button outline'} download-button`}
                        href={`${apiBasePath()}/file/${journalId}/${entryId}/${module.filename}`}
                        onClick={handleDownloadClick}
                    >
                        <Icon name="download" />
                        <span style={{ flex: 1, textAlign: 'left' }}>
                            Download {module.label || module.originalFilename || module.filename}
                            {(module.showFileSize !== false) && ` (${formatFileSize(module.fileSize)})`}
                        </span>
                    </a>
                </div>
            )}
        </div>
    );
}
