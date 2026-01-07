import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
// components
import Icon from '@/components/ui/icon';
import Modal from '@/components/ui/modal';
// context
import { useSession } from '@/context/session';
// api
import { Images } from '@/api/user/images';
import { JournalImages } from '@/api/user/journalImages';
// helpers
import { apiBasePath } from '@/helpers/endpoints.js';
// local
import ImageGallerySettingsModal from './image-gallery-settings-modal';

export default function ImageGalleryModule({
    module,
    entryId,
    journalId,
    onUpdate,
    isEditable = true,
    manuallyAdded = false,
    setDeleteListener,
    tabButtons
}) {
    const session = useSession();
    const { uploadBatch } = Images(session);
    const journalImagesApi = JournalImages(session);

    const moduleRef = useRef(module);
    const fileInputRef = useRef(null);
    const thumbnailsViewportRef = useRef(null);

    const [selectedIndex, setSelectedIndex] = useState(0);
    const [thumbnailsPerRow, setThumbnailsPerRow] = useState(module.thumbnailsPerRow || 6);
    const [galleryTypeState, setGalleryTypeState] = useState(Number(module.galleryType ?? 0));
    const [thumbnailSize, setThumbnailSize] = useState(module.thumbnailSize || '1:1');
    const [isUploading, setIsUploading] = useState(false);
    const [uploadError, setUploadError] = useState(null);
    const [page, setPage] = useState(0);

    const images = module.images || [];

    useEffect(() => {
        moduleRef.current = {
            ...module,
            images: module.images || [],
            galleryType: module.galleryType ?? moduleRef.current.galleryType ?? 0,
            thumbnailSize: module.thumbnailSize || moduleRef.current.thumbnailSize || '1:1',
            thumbnailsPerRow: module.thumbnailsPerRow || moduleRef.current.thumbnailsPerRow || 6
        };
        setThumbnailsPerRow(moduleRef.current.thumbnailsPerRow);
        setGalleryTypeState(Number(moduleRef.current.galleryType ?? 0));
        setThumbnailSize(moduleRef.current.thumbnailSize || '1:1');
    }, [module]);

    useEffect(() => {
        if (!setDeleteListener) return;
        setDeleteListener(module, handleDeleteModule);
    }, [module.id]);

    useEffect(() => {
        if (!manuallyAdded || !isEditable) return;
        if (!fileInputRef.current) return;
        const timer = setTimeout(() => {
            fileInputRef.current.click();
        }, 150);
        return () => clearTimeout(timer);
    }, [manuallyAdded, isEditable]);

    useEffect(() => {
        if (!images.length) {
            setSelectedIndex(0);
            setPage(0);
            return;
        }

        const safeIndex = Math.min(selectedIndex, images.length - 1);
        if (safeIndex !== selectedIndex) {
            setSelectedIndex(safeIndex);
        }
    }, [images.length]);

    useEffect(() => {
        const maxPage = Math.max(0, Math.ceil(images.length / Math.max(thumbnailsPerRow, 1)) - 1);
        const desiredPage = Math.min(maxPage, Math.floor(selectedIndex / Math.max(thumbnailsPerRow, 1)));
        if (desiredPage !== page) {
            setPage(desiredPage);
        }
    }, [images.length, thumbnailsPerRow, selectedIndex]);

    useEffect(() => {
        scrollToPage(page);
    }, [page, thumbnailsPerRow, images.length]);

    const totalPages = useMemo(() => {
        if (!thumbnailsPerRow) return 1;
        return Math.max(1, Math.ceil(images.length / thumbnailsPerRow));
    }, [images.length, thumbnailsPerRow]);

    const getFullImageSrc = useCallback((fileName) => {
        if (!fileName) return '';
        return `${apiBasePath()}/image/journal-entries/${entryId}/${fileName}`;
    }, [entryId]);

    const mainImageSrc = useMemo(() => {
        if (!images.length) return null;
        const fileName = images[selectedIndex] || images[0];
        if (!fileName) return null;
        return getFullImageSrc(fileName);
    }, [getFullImageSrc, images, selectedIndex]);

    const getThumbnailSrc = useCallback((fileName) => {
        if (!fileName) return '';
        const thumbName = `thumb_${fileName}`;
        return `${apiBasePath()}/image/journal-entries/${entryId}/${thumbName}`;
    }, [entryId]);

    const scrollToPage = useCallback((targetPage) => {
        const viewport = thumbnailsViewportRef.current;
        if (!viewport) return;
        const width = viewport.clientWidth;
        viewport.scrollTo({
            left: targetPage * width,
            behavior: 'smooth'
        });
    }, []);

    const handleDeleteModule = useCallback((moduleItem) => {
        if (!journalImagesApi?.deleteByModuleId || !entryId || !moduleItem?.id) return;
        journalImagesApi.deleteByModuleId(entryId, moduleItem.id);
    }, [entryId, journalImagesApi]);

    const handleUploadButtonClick = useCallback(() => {
        if (!isEditable || !fileInputRef.current) return;
        fileInputRef.current.click();
    }, [isEditable]);

    const handleFileChange = useCallback(async (event) => {
        const selectedFiles = Array.from(event.target.files || []);
        event.target.value = '';
        if (!selectedFiles.length) return;
        await uploadImages(selectedFiles);
    }, [entryId, module.id, thumbnailsPerRow]);

    const uploadImages = useCallback(async (selectedFiles) => {
        if (!entryId || !moduleRef.current?.id) {
            setUploadError('Missing entry or module information.');
            return;
        }

        const imageFiles = selectedFiles.filter(file => (file.type || '').startsWith('image/'));
        if (!imageFiles.length) {
            setUploadError('Please select image files.');
            return;
        }

        setIsUploading(true);
        setUploadError(null);

        try {
            const folder = `journal-entries/${entryId}`;
            const response = await uploadBatch(folder, imageFiles, { moduleId: moduleRef.current.id });
            if (!response.data?.success) {
                setUploadError(response.data?.message || 'Failed to upload images.');
                return;
            }

            const saved = response.data?.data?.saved || [];
            const failed = response.data?.data?.failed || [];

            if (!saved.length) {
                setUploadError(response.data?.message || 'Failed to upload images.');
                return;
            }

            await Promise.all(saved.map((fileName, index) => saveImageMetadata(fileName, imageFiles[index])));

            const baseImages = moduleRef.current.images || [];
            const updatedImages = [...baseImages, ...saved];

            const updatedModule = {
                ...moduleRef.current,
                images: updatedImages,
                galleryType: moduleRef.current.galleryType ?? 0,
                thumbnailsPerRow
            };

            moduleRef.current = updatedModule;
            onUpdate(updatedModule);

            const startIndex = updatedImages.length - saved.length;
            setSelectedIndex(startIndex >= 0 ? startIndex : 0);

            if (failed.length) {
                setUploadError(`Uploaded with warnings. Failed files: ${failed.join(', ')}`);
            }
        } catch (error) {
            console.error('Error uploading images', error);
            setUploadError('An error occurred while uploading images.');
        } finally {
            setIsUploading(false);
        }
    }, [entryId, onUpdate, thumbnailsPerRow, uploadBatch]);

    const saveImageMetadata = useCallback(async (fileName, file) => {
        if (!journalImagesApi?.add || !fileName) return;
        try {
            const dimensions = file ? await getImageDimensions(file) : { width: 0, height: 0 };
            await journalImagesApi.add({
                JournalId: journalId || null,
                JournalEntryId: entryId,
                ModuleId: moduleRef.current.id,
                FileName: fileName,
                Width: dimensions.width,
                Height: dimensions.height
            });
        } catch (error) {
            console.error('Error saving image metadata', error);
        }
    }, [entryId, journalId, journalImagesApi]);

    const getImageDimensions = (file) => {
        return new Promise((resolve) => {
            if (!file) {
                resolve({ width: 0, height: 0 });
                return;
            }

            const img = new Image();
            const objectUrl = URL.createObjectURL(file);
            img.onload = () => {
                resolve({ width: img.width || 0, height: img.height || 0 });
                URL.revokeObjectURL(objectUrl);
            };
            img.onerror = () => {
                resolve({ width: 0, height: 0 });
                URL.revokeObjectURL(objectUrl);
            };
            img.src = objectUrl;
        });
    };

    const handlePrevPage = useCallback(() => {
        if (page === 0) return;
        const newPage = page - 1;
        setPage(newPage);
        const newIndex = Math.min(images.length - 1, newPage * thumbnailsPerRow);
        setSelectedIndex(newIndex);
    }, [page, images.length, thumbnailsPerRow]);

    const handleNextPage = useCallback(() => {
        if (page >= totalPages - 1) return;
        const newPage = page + 1;
        setPage(newPage);
        const newIndex = Math.min(images.length - 1, newPage * thumbnailsPerRow);
        setSelectedIndex(newIndex);
    }, [page, thumbnailsPerRow, totalPages, images.length]);

    const handleThumbnailButtonClick = useCallback((event) => {
        const value = parseInt(event.currentTarget.getAttribute('data-index'), 10);
        if (isNaN(value)) return;
        setSelectedIndex(value);
    }, []);

    const handleGridThumbnailClick = useCallback((event) => {
        const value = parseInt(event.currentTarget.getAttribute('data-index'), 10);
        if (isNaN(value)) return;
        const fileName = images[value];
        if (!fileName) return;
        const fullSrc = getFullImageSrc(fileName);
        session.showModal(() => (
            <Modal
                onClose={() => session.hideModal()}
                className="image-gallery-preview-modal"
            >
                <img
                    className="image-gallery-preview-image"
                    src={fullSrc}
                />
            </Modal>
        ));
    }, [getFullImageSrc, images, session]);

    const handleRemoveImage = useCallback((event) => {
        event.stopPropagation();
        const button = event.currentTarget;
        const value = parseInt(button.getAttribute('data-index'), 10);
        if (isNaN(value)) return;

        const baseImages = moduleRef.current.images || [];
        const updatedImages = baseImages.filter((_, index) => index !== value);
        const updatedModule = {
            ...moduleRef.current,
            images: updatedImages
        };

        moduleRef.current = updatedModule;
        onUpdate(updatedModule);

        setSelectedIndex((prev) => {
            if (!updatedImages.length) {
                return 0;
            }
            if (prev >= updatedImages.length) {
                return updatedImages.length - 1;
            }
            return prev;
        });
    }, [onUpdate]);

    const handleSaveSettings = useCallback((settings) => {
        const {
            thumbnailsPerRow: newCount = thumbnailsPerRow,
            galleryType: newGalleryType = galleryTypeState,
            thumbnailSize: newThumbnailSize = thumbnailSize
        } = settings || {};

        const normalizedCount = newCount || thumbnailsPerRow || 6;

        setThumbnailsPerRow(normalizedCount);
        setGalleryTypeState(Number(newGalleryType ?? 0));
        setThumbnailSize(newThumbnailSize || '1:1');

        const updatedModule = {
            ...moduleRef.current,
            thumbnailsPerRow: normalizedCount,
            galleryType: Number(newGalleryType ?? 0),
            thumbnailSize: newThumbnailSize || '1:1'
        };
        moduleRef.current = updatedModule;
        onUpdate(updatedModule);
    }, [galleryTypeState, onUpdate, thumbnailSize, thumbnailsPerRow]);

    const handleShowSettings = useCallback(() => {
        if (!isEditable) return;
        session.showModal(() => (
            <ImageGallerySettingsModal
                thumbnailsPerRow={thumbnailsPerRow}
                galleryType={galleryTypeState}
                thumbnailSize={thumbnailSize}
                onSave={handleSaveSettings}
                onClose={() => session.hideModal()}
            />
        ));
    }, [galleryTypeState, handleSaveSettings, isEditable, session, thumbnailSize, thumbnailsPerRow]);

    useEffect(() => {
        if (typeof tabButtons !== 'function') return;
        const buttons = [];
        if (isEditable) {
            buttons.push({
                icon: 'add_photo_alternate',
                title: 'Add Images',
                callback: handleUploadButtonClick
            });
        }
        buttons.push({
            icon: 'settings',
            title: 'Image Gallery Settings',
            callback: handleShowSettings
        });
        tabButtons(buttons);
    }, [handleShowSettings, handleUploadButtonClick, isEditable, module.id, tabButtons, thumbnailsPerRow]);

    const thumbnailAspectRatio = useMemo(() => {
        const [w, h] = (thumbnailSize || '').split(':').map(Number);
        if (!w || !h) return '1 / 1';
        return `${w} / ${h}`;
    }, [thumbnailSize]);

    const galleryStyle = useMemo(() => ({
        '--image-gallery-columns': Math.max(1, thumbnailsPerRow || 6),
        '--image-gallery-aspect': thumbnailAspectRatio
    }), [thumbnailAspectRatio, thumbnailsPerRow]);

    const uploadButtonText = isUploading ? 'Uploading...' : 'Upload Images';
    const mainContainerClass = mainImageSrc ? 'image-gallery-main' : 'image-gallery-main empty';
    const isGridView = galleryTypeState === 1;
    const moduleClassNames = `image-gallery-module${isGridView ? ' grid-view' : ' carousel-view'}`;

    return (
        <div className={moduleClassNames} style={galleryStyle}>
            <input
                type="file"
                ref={fileInputRef}
                multiple={true}
                accept="image/*"
                onChange={handleFileChange}
                style={{ display: 'none' }}
            />

            {uploadError && (
                <div className="error-message">
                    {uploadError}
                </div>
            )}

            {!isGridView && (
                <>
                    <div className={mainContainerClass}>
                        {mainImageSrc ? (
                            <img src={mainImageSrc} alt="Selected gallery item" loading="lazy" />
                        ) : (
                            <div className="image-gallery-empty">
                                <Icon name="collections" />
                                <span>No images yet.</span>
                                {isEditable && (
                                    <button onClick={handleUploadButtonClick} disabled={isUploading}>
                                        <Icon name="upload" />
                                        {uploadButtonText}
                                    </button>
                                )}
                            </div>
                        )}
                    </div>

                    {images.length > 0 && (
                        <div className="image-gallery-thumbnails">
                            {page > 0 && (
                                <button
                                    className="icon outline"
                                    onClick={handlePrevPage}
                                    aria-label="Previous images"
                                >
                                    <Icon name="chevron_left" />
                                </button>
                            )}
                            <div className="image-gallery-thumbnails-viewport" ref={thumbnailsViewportRef}>
                                <div className="image-gallery-thumbnails-track">
                                    {images.map((fileName, index) => {
                                        const src = getThumbnailSrc(fileName);
                                        const isSelected = index === selectedIndex;
                                        const thumbnailClass = `image-gallery-thumbnail${isSelected ? ' selected' : ''}`;
                                        return (
                                            <div
                                                key={fileName + index}
                                                className={thumbnailClass}
                                                data-index={index}
                                            >
                                                <div
                                                    className="image-gallery-thumbnail-select"
                                                    data-index={index}
                                                    onClick={handleThumbnailButtonClick}
                                                >
                                                    <img src={src} alt={`Gallery thumbnail ${index + 1}`} loading="lazy" />
                                                </div>
                                                {isEditable && (
                                                    <button
                                                        type="button"
                                                        className="icon image-gallery-thumbnail-delete"
                                                        data-index={index}
                                                        aria-label="Remove image from gallery"
                                                        onClick={handleRemoveImage}
                                                    >
                                                        <Icon name="close" />
                                                    </button>
                                                )}
                                            </div>
                                        );
                                    })}
                                </div>
                            </div>
                            {page < totalPages - 1 && (
                                <button
                                    className="icon outline"
                                    onClick={handleNextPage}
                                    aria-label="Next images"
                                >
                                    <Icon name="chevron_right" />
                                </button>
                            )}
                        </div>
                    )}
                </>
            )}

            {isGridView && (
                <>
                    {images.length > 0 ? (
                        <div className="image-gallery-grid">
                            {images.map((fileName, index) => {
                                const src = getThumbnailSrc(fileName);
                                return (
                                    <div
                                        key={fileName + index}
                                        className="image-gallery-grid-item"
                                        data-index={index}
                                    >
                                        <div
                                            className="image-gallery-grid-thumb"
                                            data-index={index}
                                            onClick={handleGridThumbnailClick}
                                            style={{ backgroundImage: `url(${src})` }}
                                        >
                                        </div>
                                        {isEditable && (
                                            <button
                                                type="button"
                                                className="icon image-gallery-thumbnail-delete"
                                                data-index={index}
                                                aria-label="Remove image from gallery"
                                                onClick={handleRemoveImage}
                                            >
                                                <Icon name="close" />
                                            </button>
                                        )}
                                    </div>
                                );
                            })}
                        </div>
                    ) : (
                        <div className="image-gallery-empty">
                            <Icon name="collections" />
                            <span>No images yet.</span>
                            {isEditable && (
                                <button onClick={handleUploadButtonClick} disabled={isUploading}>
                                    <Icon name="upload" />
                                    {uploadButtonText}
                                </button>
                            )}
                        </div>
                    )}
                </>
            )}

            {isUploading && (
                <div className="image-gallery-uploading">
                    <Icon name="progress_activity" spin={true} />
                    <span>Uploading images...</span>
                </div>
            )}

        </div>
    );
}
