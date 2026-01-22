import { useState, useRef, useEffect, useCallback } from 'react';
//components
import Icon from '@/components/ui/icon';
import Modal from '@/components/ui/modal';
//context
import { useSession } from '@/context/session';
//api
import { Images } from '@/api/user/images';
import { JournalImages } from '@/api/user/journalImages';
//helpers
import { apiBasePath } from '@/helpers/endpoints.js';

export default function ImageModule({ module, entryId, journalId, onUpdate, isEditable = true, manuallyAdded = false, setDeleteListener, tabButtons }) {
    //state
    const [isUploading, setIsUploading] = useState(false);
    const [isLoadingImage, setIsLoadingImage] = useState(false);
    const [uploadError, setUploadError] = useState(null);
    
    //refs
    const fileInputRef = useRef(null);
    const replaceInputRef = useRef(null);
    const clipboardUploadRef = useRef(false);
    
    //context
    const session = useSession();
    const { upload } = Images(session);
    const { add: addImageMetadata, delete: deleteImageMetadata } = JournalImages(session);
    
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
    
    const processImageUpload = async (file, { fromClipboard = false } = {}) => {
        if (!isEditable || !file) return;

        const fileType = file.type || '';
        if (!fileType.startsWith('image/')) {
            setUploadError('Please select an image file');
            return;
        }

        setIsUploading(true);
        setUploadError(null);

        try {
            const timestamp = new Date().getTime();
            const extensionFromName = file.name?.includes('.') ? file.name.split('.').pop() : null;
            const fallbackExtension = fileType.split('/').pop() || 'png';
            const fileExtension = extensionFromName || fallbackExtension;
            const fileName = `${timestamp}-${module.id}.${fileExtension}`;
            const sourceEntryId = module.entryId || entryId;
            const path = `journal-entries/${sourceEntryId}/${fileName}`;

            const response = await upload(path, file);

            if (response.data.success) {
                setIsLoadingImage(true);
                const img = new Image();
                img.onload = async () => {
                    try {
                        const imageMetadata = {
                            JournalId: journalId,
                            JournalEntryId: entryId,
                            ModuleId: module.id,
                            FileName: fileName,
                            Width: img.width,
                            Height: img.height
                        };

                        await addImageMetadata(imageMetadata);

                        onUpdate({ ...module, image: fileName, entryId: sourceEntryId, uploadFromClipboard: null });
                        setIsLoadingImage(false);
                    } catch (error) {
                        console.error('Error saving image metadata:', error);
                        onUpdate({ ...module, image: fileName, uploadFromClipboard: null });
                        setIsLoadingImage(false);
                    }
                };
                img.onerror = () => {
                    onUpdate({ ...module, image: fileName, entryId: sourceEntryId, uploadFromClipboard: null });
                    setIsLoadingImage(false);
                };
                img.src = URL.createObjectURL(file);
            } else {
                setUploadError(response.data.message || 'Failed to upload image');
                if (fromClipboard && module.uploadFromClipboard) {
                    onUpdate({ ...module, uploadFromClipboard: false });
                }
            }
        } catch (error) {
            console.error('Error uploading image:', error);
            setUploadError('An error occurred while uploading the image');
            if (fromClipboard && module.uploadFromClipboard) {
                onUpdate({ ...module, uploadFromClipboard: null });
            }
        } finally {
            setIsUploading(false);
        }
    };

    const handleReplaceImage = useCallback(() => {
        const modalFileInputRef = { current: null };
        let isDragging = false;

        const handleModalDrop = async (e) => {
            e.preventDefault();
            e.stopPropagation();
            isDragging = false;
            
            const files = e.dataTransfer?.files;
            if (files && files.length > 0) {
                await processImageUpload(files[0]);
                session.hideModal();
            }
        };

        const handleModalDragOver = (e) => {
            e.preventDefault();
            e.stopPropagation();
        };

        const handleModalDragEnter = (e) => {
            e.preventDefault();
            e.stopPropagation();
            isDragging = true;
        };

        const handleModalDragLeave = (e) => {
            e.preventDefault();
            e.stopPropagation();
            isDragging = false;
        };

        const handleModalFileChange = async (e) => {
            const file = e.target.files[0];
            if (file) {
                await processImageUpload(file);
                session.hideModal();
            }
        };

        session.showModal(() => (
            <Modal
                title="Replace Image"
                onClose={() => session.hideModal()}
            >
                <div
                    style={{
                        padding: '2em',
                        textAlign: 'center',
                        minHeight: '200px',
                        display: 'flex',
                        flexDirection: 'column',
                        alignItems: 'center',
                        justifyContent: 'center',
                        gap: '1em'
                    }}
                    onDrop={handleModalDrop}
                    onDragOver={handleModalDragOver}
                    onDragEnter={handleModalDragEnter}
                    onDragLeave={handleModalDragLeave}
                >
                    <div
                        style={{
                            border: '2px dashed #ccc',
                            borderRadius: '8px',
                            padding: '2em',
                            width: '100%',
                            backgroundColor: isDragging ? '#f0f0f0' : 'transparent'
                        }}
                    >
                        <Icon name="cloud_upload" style={{ fontSize: '3em', opacity: 0.5 }} />
                        <p>Drag and drop an image here</p>
                    </div>
                    <div className="tool-bar">
                        <div className="image-upload-button-container">
                            <button disabled={isUploading}>
                                <Icon name="upload" />
                                {isUploading ? 'Uploading...' : 'Upload Image'}
                            </button>
                            <input
                                type="file"
                                ref={(el) => { modalFileInputRef.current = el; }}
                                onChange={handleModalFileChange}
                                accept="image/*"
                                disabled={isUploading}
                            />
                        </div>
                    </div>
                </div>
            </Modal>
        ));
    }, [session, isUploading, processImageUpload]);

    useEffect(() => {
        if (typeof tabButtons !== 'function') return;
        const buttons = [];
        if (module.image) {
            buttons.push({
                icon: 'add_photo_alternate',
                title: 'Replace Image',
                callback: handleReplaceImage
            });
        }
        tabButtons(buttons);
    }, [module.image, handleReplaceImage]);

    const handleFileChange = async (e) => {
        if (!isEditable) return;
        const file = e.target.files[0];
        if (!file) return;
        await processImageUpload(file);
    };

    useEffect(() => {
        if (!isEditable || !module.uploadFromClipboard || clipboardUploadRef.current) return;
        if (typeof window === 'undefined') return;

        const clipboardImages = window.clipboardImages || null;
        let file = clipboardImages?.[module.id];

        if (!file && window.clipboardImage) {
            file = window.clipboardImage.file;
        }

        clipboardUploadRef.current = true;

        if (!file) {
            onUpdate({ ...module, uploadFromClipboard: null });
            clipboardUploadRef.current = false;
            return;
        }

        processImageUpload(file, { fromClipboard: true }).finally(() => {
            if (typeof window !== 'undefined') {
                if (window.clipboardImages && window.clipboardImages[module.id]) {
                    delete window.clipboardImages[module.id];
                }
                if (window.clipboardImage) {
                    delete window.clipboardImage;
                }
            }
            clipboardUploadRef.current = false;
        });
    }, [isEditable, module.uploadFromClipboard, module.id]);

    const removeModule = (moduleItem) => {
        if (deleteImageMetadata) {
            const sourceEntryId = moduleItem.entryId || entryId;
            deleteImageMetadata(sourceEntryId, moduleItem.id);
        }
    };
    
    return (
        <div className="image-module">
            {isEditable && !module.image && !isLoadingImage && (
                <div className="tool-bar">
                    <div className="left-side">
                        <div className="image-upload-button-container">
                            <button disabled={isUploading}>
                                <Icon name="upload" />
                                {isUploading ? 'Uploading...' : 'Upload Image'}
                            </button>
                            <input 
                                type="file" 
                                ref={fileInputRef}
                                onChange={handleFileChange}
                                accept="image/*"
                                disabled={isUploading}
                            />
                        </div>
                    </div>
                </div>
            )}
            
            {isLoadingImage && (
                <div className="loading-message" style={{ padding: '1em', textAlign: 'center', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5em' }}>
                    <Icon name="progress_activity" spin={true} />
                    Loading image...
                </div>
            )}
            
            {uploadError && (
                <div className="error-message" style={{ color: 'red', marginTop: '0.5em' }}>
                    {uploadError}
                </div>
            )}
            
            {module.image && !isLoadingImage && (
                <div className="image-preview">
                    <img loading="lazy"
                        src={apiBasePath() + `/image/journal-entries/${module.entryId || entryId}/${module.image}`} 
                        alt="Uploaded content" 
                    />
                </div>
            )}
        </div>
    );
}
