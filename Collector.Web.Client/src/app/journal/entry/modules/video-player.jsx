import { useState, useRef, useEffect } from 'react';
import * as signalR from '@microsoft/signalr';
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
    const [videoUrl, setVideoUrl] = useState('');
    const [isDownloading, setIsDownloading] = useState(false);
    const [downloadProgress, setDownloadProgress] = useState(0);
    const [downloadTotal, setDownloadTotal] = useState(0);
    const [downloadLoaded, setDownloadLoaded] = useState(0);
    const [downloadStatus, setDownloadStatus] = useState('');
    const [downloadError, setDownloadError] = useState(null);
    const [showDeleteModal, setShowDeleteModal] = useState(false);
    const [pendingDeleteModuleId, setPendingDeleteModuleId] = useState(null);

    //refs
    const fileInputRef = useRef(null);
    const moduleRef = useRef(module);

    //context
    const session = useSession();
    const { uploadVideo, deleteVideo } = Videos(session);

    useEffect(() => {
        moduleRef.current = module;
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
            const response = await uploadVideo(journalId, entryId, module.id, file, (progress, e) => {
                setUploadProgress(progress);
                setDownloadLoaded(e.loaded);
                if(downloadTotal == 0) setDownloadTotal(e.total);
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

    const handleDownloadVideo = () => {
        if (!videoUrl.trim()) return;

        setIsDownloading(true);
        setDownloadError(null);
        setDownloadProgress(0);
        setDownloadStatus('Connecting...');

        // Create and setup SignalR connection
        const conn = new signalR.HubConnectionBuilder()
            .withUrl(apiBasePath() + '/video-download', {
                withCredentials: true,
                skipNegotiation: true,
                transport: signalR.HttpTransportType.WebSockets
            })
            .withAutomaticReconnect([0, 1000, 5000, 10000])
            .configureLogging(signalR.LogLevel.Information)
            .build();

        conn.start().then(() => {
            // Listen for video record creation (before download starts)
            conn.on('VideoRecordCreated', (data) => {
                // Update module with video ID and path immediately so entry can be saved
                moduleRef.current = {
                    ...moduleRef.current,
                    videoId: data.id,
                    videoPath: data.videoPath,
                    url: videoUrl.trim(),
                    title: data.title,
                    downloaded: false
                };
                onUpdate(moduleRef.current);
            });

            // Listen for download progress
            conn.on('DownloadProgress', (progress, status) => {
                setDownloadProgress(progress);
                setDownloadStatus(status);
            });

            // Listen for download completion
            conn.on('DownloadComplete', (data) => {
                moduleRef.current = {
                    ...moduleRef.current,
                    thumbnailPath: data.thumbnailPath,
                    downloaded: true
                };
                onUpdate(moduleRef.current);
                setIsDownloading(false);
                setDownloadProgress(0);
                setDownloadStatus('');
                setVideoUrl('');

                // Close connection after download completes
                conn.stop();
            });

            // Listen for download errors
            conn.on('DownloadError', (error) => {
                setDownloadError(error);
                setIsDownloading(false);
                setDownloadProgress(0);
                setDownloadStatus('');

                // Close connection on error
                conn.stop();
            });

            // Invoke download

            conn.invoke('DownloadVideo', videoUrl.trim(), parseInt(journalId), entryId, module.id)
                .then(() => { })
                .catch(err => {
                    // Only show error if it's not a connection closed error after successful completion
                    if (err.message && !err.message.includes('connection being closed')) {
                        console.error('Error invoking DownloadVideo:', err);
                        setDownloadError('Error sending request to download service. Please try again.');
                        setIsDownloading(false);
                    }
                    // Don't try to stop connection here as it may already be stopped
                });
        }).catch(err => {
            console.error('Error starting SignalR connection:', err);
            setDownloadError('Error connecting to download service. Please try again.');
            setIsDownloading(false);
        });
    };

    const handleUrlKeyDown = (e) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            handleDownloadVideo();
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

    const hasVideo = module.videoPath;

    return (
        <div className="video-player-module">
            {isEditable && !hasVideo && !isUploading && !isDownloading && (
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
                                accept=".mp4,.mkv,.webm,.ogg,.mov,.avi,.wmv,.flv,.m4v,video/*"
                                style={{ display: 'none' }}
                            />
                        </div>
                    </div>
                    <div className="left-side or">or</div>
                    <div className="left-side tool-bar flex" style={{ width: 'calc(100% - 20em)' }}>
                        <Input
                            name="video-url"
                            value={videoUrl}
                            onChange={(e) => setVideoUrl(e.target.value)}
                            onKeyDown={handleUrlKeyDown}
                            placeholder="Paste video URL (YouTube, etc.)"
                            style={{ width: '100%' }}
                            formGroupClassName="width-100"
                        />
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
                            {uploadProgress === 100 ? 'Processing Video...' : uploadProgress + '% (' + (downloadLoaded / 1048576).toFixed(2) + 'MB / ' + (downloadTotal / 1048576).toFixed(2) + 'MB)'}
                        </div>
                    </div>
                </div>
            )}

            {isDownloading && (
                <div className="upload-progress-container">
                    {downloadStatus && (
                        <div className="processing-text">
                            {downloadStatus}
                        </div>
                    )}
                    <div className="progress-bar">
                        <div
                            className="progress-fill"
                            style={{ width: `${downloadProgress}%` }}
                        />
                        <div className="progress-text">
                            {downloadProgress}%
                        </div>
                    </div>
                </div>
            )}

            {uploadError && (
                <div className="error-message">
                    {uploadError}
                </div>
            )}

            {downloadError && (
                <div className="error-message">
                    {downloadError}
                </div>
            )}

            {isEditable && !isDownloading && module.videoId && module.downloaded === false && module.url && (
                <div className="video-not-downloaded">
                    <p>The video has not been downloaded yet.</p>
                    <button onClick={() => {
                        setVideoUrl(module.url);
                        handleDownloadVideo();
                    }}>
                        <Icon name="download" />
                        Try Again
                    </button>
                </div>
            )}

            {!isDownloading && module.videoPath && module.downloaded !== false && (
                <div className="video-preview">
                    {module.url && (
                        <div className="video-url-icon">
                            <a href={module.url} target="_blank" rel="noopener noreferrer" title="Open original video URL">
                                <Icon name="arrow_outward" />
                            </a>
                        </div>
                    )}
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
