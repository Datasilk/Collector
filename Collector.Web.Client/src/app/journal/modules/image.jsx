import { useState, useRef, useEffect, useCallback } from 'react';
//components
import Icon from '@/components/ui/icon';
import Modal from '@/components/ui/modal';
import Select from '@/components/forms/select';
import Input from '@/components/forms/input';
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

    const handleShowSettings = useCallback(() => {
        session.showModal((
            <ImageSettingsModal
                module={module}
                onSave={(settings) => {
                    const updatedModule = { ...module, ...settings };
                    onUpdate(updatedModule);
                    session.hideModal();
                }}
                onClose={() => session.hideModal()}
            />
        ));
    }, [session, module, onUpdate]);

    useEffect(() => {
        if (isEditable == true) {
            if (typeof tabButtons !== 'function') return;
            const buttons = [];
            if (module.image) {
                buttons.push({
                    icon: 'add_photo_alternate',
                    title: 'Replace Image',
                    callback: handleReplaceImage
                });
                buttons.push({
                    icon: 'settings',
                    title: 'Image Settings',
                    callback: handleShowSettings
                });
            }
            tabButtons(buttons);
        }
    }, [module.image, isEditable, handleReplaceImage, handleShowSettings, tabButtons]);

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
                    {(!module.settings?.aspectRatio || module.settings?.aspectRatio === 'none') ? (
                        <img loading="lazy"
                            src={apiBasePath() + `/image/journal-entries/${module.entryId || entryId}/${module.image}`}
                            alt="Uploaded content"
                        />
                    ) : (
                        <div
                            style={{
                                width: '100%',
                                aspectRatio: module.settings.aspectRatio,
                                backgroundImage: `url(${apiBasePath()}/image/journal-entries/${module.entryId || entryId}/${module.image})`,
                                backgroundSize: module.settings.displayMode === 'custom'
                                    ? `${module.settings.widthValue || 'auto'} ${module.settings.heightValue || 'auto'}`
                                    : (module.settings.displayMode || 'cover'),
                                backgroundPosition: module.settings.position === 'custom'
                                    ? `${module.settings.positionX || '10px'} ${module.settings.positionY || '25%'}`
                                    : (module.settings.position || 'center'),
                                backgroundRepeat: 'no-repeat'
                            }}
                        />
                    )}
                </div>
            )}
        </div>
    );
}

function ImageSettingsModal({ module, onSave, onClose }) {
    const settings = module.settings || {};
    const [aspectRatio, setAspectRatio] = useState(settings.aspectRatio || 'none');
    const [displayMode, setDisplayMode] = useState(settings.displayMode || 'cover');
    const [widthMode, setWidthMode] = useState(settings.widthMode || 'auto');
    const [heightMode, setHeightMode] = useState(settings.heightMode || 'auto');
    const [widthValue, setWidthValue] = useState(settings.widthValue || '100%');
    const [heightValue, setHeightValue] = useState(settings.heightValue || '100%');
    const [position, setPosition] = useState(settings.position || 'center');
    const [positionX, setPositionX] = useState(settings.positionX || '10px');
    const [positionY, setPositionY] = useState(settings.positionY || '25%');

    const aspectRatioOptions = [
        { label: 'None', value: 'none' },
        { label: '1:1 (Square)', value: '1 / 1' },
        { label: '3:2', value: '3 / 2' },
        { label: '2:3', value: '2 / 3' },
        { label: '4:3', value: '4 / 3' },
        { label: '3:4', value: '3 / 4' },
        { label: '16:9 (Widescreen)', value: '16 / 9' },
        { label: '9:16 (Portrait)', value: '9 / 16' },
        { label: '21:9 (Ultrawide)', value: '21 / 9' },
        { label: '5:4', value: '5 / 4' },
        { label: '4:5', value: '4 / 5' }
    ];

    const displayModeOptions = [
        { label: 'Contain', value: 'contain' },
        { label: 'Cover', value: 'cover' },
        { label: 'Auto', value: 'auto' },
        { label: 'Custom', value: 'custom' }
    ];

    const sizeModeOptions = [
        { label: 'Auto', value: 'auto' },
        { label: 'Value', value: 'value' }
    ];

    const positionOptions = [
        { label: 'Top', value: 'top' },
        { label: 'Bottom', value: 'bottom' },
        { label: 'Left', value: 'left' },
        { label: 'Right', value: 'right' },
        { label: 'Center', value: 'center' },
        { label: 'Custom', value: 'custom' }
    ];

    const handleSave = () => {
        const newSettings = {
            aspectRatio,
            displayMode: aspectRatio === 'none' ? undefined : displayMode,
            widthMode: displayMode === 'custom' ? widthMode : undefined,
            heightMode: displayMode === 'custom' ? heightMode : undefined,
            widthValue: displayMode === 'custom' && widthMode === 'value' ? widthValue : undefined,
            heightValue: displayMode === 'custom' && heightMode === 'value' ? heightValue : undefined,
            position: aspectRatio === 'none' ? undefined : position,
            positionX: position === 'custom' ? positionX : undefined,
            positionY: position === 'custom' ? positionY : undefined
        };
        onSave({ settings: newSettings });
    };

    return (
        <Modal
            title="Image Display Settings"
            onClose={onClose}
            className="image-settings-modal"
        >
            <div className="form">
                <div className="form-row">
                    <Select
                        label="Aspect Ratio"
                        name="aspect-ratio"
                        value={aspectRatio}
                        onChange={(e) => setAspectRatio(e.target.value)}
                        options={aspectRatioOptions}
                    />
                </div>
                <div className="form-row">
                    {aspectRatio !== 'none' && (<>
                        <Select
                            label="Display Mode"
                            name="display-mode"
                            value={displayMode}
                            onChange={(e) => setDisplayMode(e.target.value)}
                            options={displayModeOptions}
                        />
                        {displayMode === 'custom' && (
                            <>
                                <div>
                                    <Select
                                        label="Width"
                                        name="width-mode"
                                        value={widthMode}
                                        onChange={(e) => setWidthMode(e.target.value)}
                                        options={sizeModeOptions}
                                    />
                                    {widthMode === 'value' && (
                                        <Input
                                            label="Width Value"
                                            name="width-value"
                                            value={widthValue}
                                            onChange={(e) => setWidthValue(e.target.value)}
                                            placeholder="100%"
                                            style={{ width: '5em' }}
                                        />
                                    )}
                                </div>
                                <div>
                                    <Select
                                        label="Height"
                                        name="height-mode"
                                        value={heightMode}
                                        onChange={(e) => setHeightMode(e.target.value)}
                                        options={sizeModeOptions}
                                    />
                                    {heightMode === 'value' && (
                                        <Input
                                            label="Height Value"
                                            name="height-value"
                                            value={heightValue}
                                            onChange={(e) => setHeightValue(e.target.value)}
                                            placeholder="100%"
                                            style={{ width: '5em' }}
                                        />
                                    )}
                                </div>
                            </>
                        )}
                    </>)}
                </div>

                {aspectRatio !== 'none' && (
                    <div className="form-row">
                        <div>
                            <Select
                                label="Position"
                                name="position"
                                value={position}
                                onChange={(e) => setPosition(e.target.value)}
                                options={positionOptions}
                            />
                        </div>

                        {position === 'custom' && (<>
                            <div>
                                <Input
                                    label="Position X"
                                    name="position-x"
                                    value={positionX}
                                    onChange={(e) => setPositionX(e.target.value)}
                                    placeholder="10px"
                                    style={{ width: '5em' }}
                                />
                            </div>
                            <div>
                                <Input
                                    label="Position Y"
                                    name="position-y"
                                    value={positionY}
                                    onChange={(e) => setPositionY(e.target.value)}
                                    placeholder="25%"
                                    style={{ width: '5em' }}
                                />
                            </div>
                        </>
                        )}
                    </div>
                )}
            </div>
            <div className="buttons">
                <button className="cancel" onClick={onClose}>Cancel</button>
                <button onClick={handleSave}>Save Changes</button>
            </div>
        </Modal >
    );
}
