import { useState, useRef, useEffect } from 'react';
//components
import Icon from '@/components/ui/icon';
import Input from '@/components/forms/input';
import Modal from '@/components/ui/modal';
//context
import { useSession } from '@/context/session';
//api
import { Videos } from '@/api/user/videos';
//helpers
import { apiBasePath } from '@/helpers/endpoints.js';

export default function VideoPlayerModule({ module, entryId, journalId, onUpdate, isEditable = true, manuallyAdded = false, setDeleteListener }) {
    //state
    const [isUploading, setIsUploading] = useState(false);
    const [uploadProgress, setUploadProgress] = useState(0);
    const [uploadError, setUploadError] = useState(null);
    const [showUrlInput, setShowUrlInput] = useState(false);
    const [videoUrl, setVideoUrl] = useState(module.videoUrl || '');
    const [showDeleteModal, setShowDeleteModal] = useState(false);
    const [pendingDeleteModuleId, setPendingDeleteModuleId] = useState(null);

    //refs
    const fileInputRef = useRef(null);

    //context
    const session = useSession();
    const { uploadVideo, deleteVideo } = Videos(session);

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

    const handleFileChange = async (e) => {
        if (!isEditable) return;

        const file = e.target.files[0];
        if (!file) return;

        // Check if file is a video
        if (!file.type.startsWith('video/')) {
            setUploadError('Please select a video file');
            return;
        }

        setIsUploading(true);
        setUploadError(null);
        setUploadProgress(0);

        try {
            const response = await uploadVideo(journalId, entryId, module.id, file, (progress) => {
                setUploadProgress(progress);
            });

            if (response.data.success) {
                const { id, videoPath, thumbnailPath } = response.data.data;

                // Update the module with the video ID, path and thumbnail
                onUpdate({
                    ...module,
                    videoId: id,
                    videoPath: videoPath,
                    thumbnailPath: thumbnailPath,
                    videoUrl: '' // Clear URL if video is uploaded
                });
            } else {
                setUploadError(response.data.message || 'Failed to upload video');
            }
        } catch (error) {
            console.error('Error uploading video:', error);
            setUploadError('An error occurred while uploading the video');
        } finally {
            setIsUploading(false);
            setUploadProgress(0);
        }
    };

    const handleUrlSubmit = () => {
        if (!videoUrl.trim()) return;

        // Update the module with the video URL
        onUpdate({
            ...module,
            videoUrl: videoUrl.trim(),
            videoPath: '', // Clear uploaded video if URL is set
            thumbnailPath: ''
        });
        setShowUrlInput(false);
    };

    const handleUrlKeyDown = (e) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            handleUrlSubmit();
        } else if (e.key === 'Escape') {
            e.preventDefault();
            setShowUrlInput(false);
            setVideoUrl(module.videoUrl || '');
        }
    };

    const removeModule = async (moduleItem, callback) => {
        // Show confirmation modal if video was uploaded
        if (moduleItem.videoId) {
            setPendingDeleteModuleId(moduleItem.id);
            setShowDeleteModal(true);
            window.__videoDeleteResolve = callback;
        }
    };

    const handleDeleteConfirm = async (deleteFiles) => {
        if (!pendingDeleteModuleId) return;

        try {
            await deleteVideo(entryId, pendingDeleteModuleId, deleteFiles);
        } catch (error) {
            console.error('Error deleting video:', error);
        } finally {
            setShowDeleteModal(false);
            setPendingDeleteModuleId(null);

            // Resolve the promise
            if (window.__videoDeleteResolve) {
                window.__videoDeleteResolve();
                delete window.__videoDeleteResolve;
            }
        }
    };

    const handleDeleteCancel = () => {
        setShowDeleteModal(false);
        setPendingDeleteModuleId(null);

        // Resolve the promise
        if (window.__videoDeleteResolve) {
            window.__videoDeleteResolve();
            delete window.__videoDeleteResolve;
        }
    };

    const hasVideo = module.videoPath || module.videoUrl;

    return (
        <div className="video-player-module">
            {isEditable && !hasVideo && !isUploading && (
                <div className="tool-bar">
                    <div className="left-side">
                        <div className="video-upload-button-container">
                            <button onClick={() => fileInputRef.current?.click()}>
                                <Icon name="upload" />
                                Upload Video
                            </button>
                            <input
                                type="file"
                                ref={fileInputRef}
                                onChange={handleFileChange}
                                accept="video/*"
                                style={{ display: 'none' }}
                            />
                        </div>
                    </div>
                    <div className="left-side">
                        <button onClick={() => setShowUrlInput(!showUrlInput)}>
                            <Icon name="link" />
                            Paste URL
                        </button>
                    </div>
                </div>
            )}

            {isUploading && (
                <div className="upload-progress-container">
                    <div className="progress-bar">
                        <div
                            className="progress-fill"
                            style={{ width: `${uploadProgress}%` }}
                        />
                        <div className="progress-text">
                            {uploadProgress === 100 ? 'Processing Video...' : uploadProgress + '%'}
                        </div>
                    </div>
                </div>
            )}

            {showUrlInput && !hasVideo && (
                <div className="url-input-container">
                    <Input
                        name="video-url"
                        value={videoUrl}
                        onChange={(e) => setVideoUrl(e.target.value)}
                        onKeyDown={handleUrlKeyDown}
                        placeholder="Paste video URL (YouTube, Vimeo, etc.)"
                        autoFocus
                    />
                    <div className="buttons">
                        <button onClick={handleUrlSubmit}>Add URL</button>
                        <button className="cancel" onClick={() => {
                            setShowUrlInput(false);
                            setVideoUrl(module.videoUrl || '');
                        }}>Cancel</button>
                    </div>
                </div>
            )}

            {uploadError && (
                <div className="error-message">
                    {uploadError}
                </div>
            )}

            {module.videoPath && (
                <div className="video-preview">
                    <video
                        controls
                        poster={module.thumbnailPath ? apiBasePath() + `/video/thumb/${module.thumbnailPath}` : undefined}
                    >
                        <source
                            src={module.videoId ? apiBasePath() + `/video/${module.videoId}` : apiBasePath() + `/video/${module.videoPath}`}
                            type="video/mp4"
                        />
                        Your browser does not support the video tag.
                    </video>
                </div>
            )}

            {module.videoUrl && !module.videoPath && (
                <div className="video-embed">
                    <iframe
                        src={module.videoUrl}
                        allowFullScreen
                        title="Video player"
                    />
                </div>
            )}

            {showDeleteModal && (
                <Modal title="Delete Video" onClose={handleDeleteCancel}>
                    <p>Would you like to delete the video file & metadata from the server as well? If you choose yes, the video will be permanently deleted from the server.</p>
                    <div className="buttons">
                        <button className="delete" onClick={() => handleDeleteConfirm(true)}>Delete Permanently</button>
                        <button className="cancel" onClick={() => handleDeleteConfirm(false)}>Keep Video</button>
                    </div>
                </Modal>
            )}
        </div>
    );
}
