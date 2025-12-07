import { useState, useRef, useEffect, useImperativeHandle, forwardRef } from 'react';
//components
import Icon from '@/components/ui/icon';
import Input from '@/components/forms/input';
import Modal from '@/components/ui/modal';
import CookiesUploadModal from '@/app/journal/components/cookies-upload-modal';
//context
import { useSession } from '@/context/session';
import { useWorkerHub } from '@/context/workerhub';
import { useVideoPiP } from '@/context/videopip';
//api
import { Videos } from '@/api/user/videos';
import { Cookies } from '@/api/user/cookies';
//helpers
import { apiBasePath } from '@/helpers/endpoints.js';

export default function VideoPlayerModule({ module, entryId, journalId, onUpdate, isEditable = true, manuallyAdded = false, setDeleteListener, tabButtons }) {
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
    const [isPlaying, setIsPlaying] = useState(false);
    const [currentTime, setCurrentTime] = useState(0);
    const [duration, setDuration] = useState(0);
    const [volume, setVolume] = useState(() => {
        const saved = localStorage.getItem('collector:video-player:volume');
        return saved !== null ? parseFloat(saved) : 1;
    });
    const [isMuted, setIsMuted] = useState(false);
    const [showPreview, setShowPreview] = useState(false);
    const [previewTime, setPreviewTime] = useState(0);
    const [previewPosition, setPreviewPosition] = useState(0);
    const [previewBlobUrl, setPreviewBlobUrl] = useState(null);
    const [bufferedRanges, setBufferedRanges] = useState([]);
    const [isDragging, setIsDragging] = useState(false);
    const [showControls, setShowControls] = useState(true);
    const [isFocused, setIsFocused] = useState(false);
    const [hasLoadedOnce, setHasLoadedOnce] = useState(false);
    const [isPiPMode, setIsPiPMode] = useState(false);
    const [isFullscreen, setIsFullscreen] = useState(false);
    const [pendingDownloadUrl, setPendingDownloadUrl] = useState(null);

    //refs
    const fileInputRef = useRef(null);
    const moduleRef = useRef(module);
    const videoRef = useRef(null);
    const videoPlayerRef = useRef(null);
    const seekBarRef = useRef(null);
    const containerRef = useRef(null);
    const thumbnailCacheRef = useRef({}); // Cache: { url: base64DataUrl }
    const downloadingRef = useRef(new Set()); // Track in-progress downloads
    const pendingLoadRef = useRef(null); // Track pending thumbnail load promise
    const controlsTimeoutRef = useRef(null); // Track controls hide timeout
    const pipDataRef = useRef(null); // Track data for PiP registration on unmount
    const isPlayingRef = useRef(false); // Track playing state for unmount cleanup
    const registerPipVideoRef = useRef(null); // Track registerPipVideo for unmount
    const clearPipVideoRef = useRef(null); // Track clearPipVideo for restoration
    const currentTimeRef = useRef(0); // Track current playback time for PiP

    //context
    const session = useSession();
    const { uploadVideo, deleteVideo, generateThumbnail } = Videos(session);
    const { checkYouTubeCookies } = Cookies(session);
    const { call: callWorker, getWorkers, subscribe, requestProgress } = useWorkerHub();
    const { pipVideo, registerPipVideo, clearPipVideo, pausePipVideo, getPipVideoState } = useVideoPiP();

    // Keep refs updated
    registerPipVideoRef.current = registerPipVideo;
    clearPipVideoRef.current = clearPipVideo;

    // Check if this video should restore from PiP state on mount
    const pendingPipRestoreRef = useRef(null);

    useEffect(() => {
        // Check for navigation from PiP navigate button
        const pipState = window.__pipVideoState;
        if (pipState && pipState.moduleId === module.id && pipState.entryId == entryId) {
            window.__pipVideoState = null;

            // If video is already loaded, restore immediately
            if (videoRef.current && videoRef.current.readyState >= 1) {
                // Scroll to the module container (multiple attempts for lazy loading)
                if (pipState.scrollToVideo) {
                    const scrollToModule = () => {
                        const moduleElement = containerRef.current?.closest('.module');
                        if (moduleElement) {
                            moduleElement.scrollIntoView({ behavior: 'auto', block: 'center' });
                        }
                    };
                    scrollToModule();
                    setTimeout(scrollToModule, 300);
                    setTimeout(scrollToModule, 700);
                    setTimeout(scrollToModule, 1500);
                }

                // Restore playback
                videoRef.current.currentTime = pipState.currentTime || 0;
                videoRef.current.volume = pipState.volume || 1;
                videoRef.current.muted = pipState.isMuted || false;
                setVolume(pipState.volume || 1);
                setIsMuted(pipState.isMuted || false);
                videoRef.current.play().catch(() => { });
                clearPipVideoRef.current?.();
            } else {
                // Store for restoration when video loads
                pendingPipRestoreRef.current = pipState;
                setHasLoadedOnce(true);
            }
            return;
        }

        // Also handle live PiP restoration (when navigating back without using the button)
        if (pipVideo && pipVideo.moduleId === module.id && pipVideo.entryId == entryId) {
            const livePipState = getPipVideoState();
            if (livePipState) {
                pendingPipRestoreRef.current = livePipState;
                setHasLoadedOnce(true);
            }
        }
    }, [pipVideo, module.id, entryId]);

    // Expose method to get current video state for PiP mode
    const getVideoState = () => {
        if (!videoRef.current || !module.videoPath) return null;
        return {
            moduleId: module.id,
            entryId: entryId,
            journalId: journalId,
            videoId: module.videoId,
            videoPath: module.videoPath,
            thumbnailPath: module.thumbnailPath,
            url: module.url,
            currentTime: videoRef.current.currentTime,
            duration: videoRef.current.duration,
            volume: videoRef.current.volume,
            isMuted: videoRef.current.muted,
            isPlaying: !videoRef.current.paused
        };
    };

    // Store getVideoState on window for access from entry.jsx
    useEffect(() => {
        if (!window.__videoPlayers) {
            window.__videoPlayers = {};
        }
        window.__videoPlayers[module.id] = {
            getVideoState,
            isPlaying: () => isPlaying
        };
        return () => {
            if (window.__videoPlayers) {
                delete window.__videoPlayers[module.id];
            }
        };
    }, [module.id, isPlaying]);

    // Keep pipDataRef up to date for PiP registration on unmount
    useEffect(() => {
        pipDataRef.current = {
            moduleId: module.id,
            entryId,
            journalId,
            videoId: module.videoId,
            videoPath: module.videoPath,
            thumbnailPath: module.thumbnailPath,
            url: module.url
        };
    }, [module.id, entryId, journalId, module.videoId, module.videoPath, module.thumbnailPath, module.url]);

    // Register with global PiP when unmounting while playing
    useEffect(() => {
        return () => {
            // Check if video was playing when unmounting
            // Also check that this video isn't already the one in global PiP (it was paused there)
            const isAlreadyInPiP = window.__currentPipModuleId === pipDataRef.current?.moduleId;
            if (isPlayingRef.current && pipDataRef.current?.videoPath && !isAlreadyInPiP) {
                const videoState = {
                    ...pipDataRef.current,
                    currentTime: currentTimeRef.current, // Use ref instead of videoRef
                    duration: videoRef.current?.duration || 0,
                    volume: videoRef.current?.volume || 1,
                    isMuted: videoRef.current?.muted || false,
                    isPlaying: true
                };
                window.__currentPipModuleId = videoState.moduleId;
                registerPipVideoRef.current(videoState);
            }
        };
    }, []);

    useEffect(() => {
        moduleRef.current = module;
        if (!setDeleteListener) return;
        setDeleteListener(module, removeModule);
    }, [module.id]);

    // Auto-download video if autoTryAgain is set
    useEffect(() => {
        if (module.autoTryAgain && module.url && !module.videoId && !isDownloading) {
            setVideoUrl(module.url);
            // Trigger download after a short delay to ensure state is updated
            setTimeout(() => {
                handleDownloadVideo(module.url);
            }, 2000);
        }
    }, [module.autoTryAgain, module.url, module.videoId]);

    // Pause other videos when this one starts playing
    useEffect(() => {
        // Keep isPlayingRef in sync for unmount cleanup
        isPlayingRef.current = isPlaying;

        if (isPlaying && videoRef.current) {
            // Pause the global PiP player if it's playing
            if (pipVideo) {
                pausePipVideo();
            }

            // Dispatch custom event to pause other videos
            const event = new CustomEvent('videoPlaying', {
                detail: { videoElement: videoRef.current }
            }, module.id);
            window.dispatchEvent(event);
        }
    }, [isPlaying]);

    // Listen for other videos playing and pause this one
    useEffect(() => {
        const handleOtherVideoPlaying = (event) => {
            if (event.detail.videoElement !== videoRef.current && videoRef.current && !videoRef.current.paused) {
                videoRef.current.pause();
                setIsPlaying(false);
            }
        };

        window.addEventListener('videoPlaying', handleOtherVideoPlaying);

        return () => {
            window.removeEventListener('videoPlaying', handleOtherVideoPlaying);
        };
    }, []);

    // IntersectionObserver to lazy load video when it comes into view
    useEffect(() => {
        if (!containerRef.current || hasLoadedOnce) return;

        const observer = new IntersectionObserver(
            (entries) => {
                entries.forEach((entry) => {
                    if (entry.isIntersecting) {
                        setHasLoadedOnce(true);
                    }
                });
            },
            {
                root: null,
                rootMargin: '100px',
                threshold: 0.1
            }
        );

        observer.observe(containerRef.current);

        return () => {
            if (containerRef.current) {
                observer.unobserve(containerRef.current);
            }
        };
    }, [hasLoadedOnce]);

    // IntersectionObserver to enable PiP mode when video is playing and out of view
    useEffect(() => {
        if (!videoPlayerRef.current || !hasLoadedOnce) return;

        const observer = new IntersectionObserver(
            (entries) => {
                entries.forEach((entry) => {
                    // Enable PiP only if video is playing and not in view
                    if (!entry.isIntersecting && isPlaying) {
                        // Clear global PiP player only if it's a DIFFERENT video
                        if (window.__currentPipModuleId && window.__currentPipModuleId !== module.id) {
                            clearPipVideo();
                        }

                        // Dispatch event to revert any existing PiP players
                        const event = new CustomEvent('requestPiPMode', {
                            detail: { videoElement: videoRef.current }
                        });
                        window.dispatchEvent(event);

                        // Save current height before going into PiP mode
                        if (containerRef.current && videoPlayerRef.current) {
                            const height = videoPlayerRef.current.offsetHeight;
                            containerRef.current.style.height = `${height}px`;

                            // Find all parent module elements and set z-index
                            let currentElement = containerRef.current.closest('.module');
                            let zIndex = 99;

                            while (currentElement) {
                                currentElement.style.zIndex = zIndex.toString();
                                currentElement.setAttribute('data-pip-zindex', 'true');
                                zIndex--;

                                // Find next parent module
                                currentElement = currentElement.parentElement?.closest('.module');
                            }

                            // Add placeholder div at bottom of entry content
                            const entryContent = document.querySelector('.entry-content');
                            if (entryContent) {
                                // Calculate PiP player height (400px width * 9/16 aspect ratio = 225px)
                                const pipWidth = 400;
                                const pipHeight = pipWidth * (9 / 16);

                                const placeholder = document.createElement('div');
                                placeholder.className = 'pip-placeholder';
                                placeholder.style.width = '100%';
                                placeholder.style.height = `${pipHeight}px`;
                                placeholder.setAttribute('data-pip-placeholder', module.id);
                                entryContent.appendChild(placeholder);
                            }
                        }
                        setIsPiPMode(true);
                    } else if (entry.isIntersecting) {
                        // Remove fixed height when back in view
                        if (containerRef.current) {
                            containerRef.current.style.height = '';

                            // Remove z-index from all parent module elements
                            let currentElement = containerRef.current.closest('.module');

                            while (currentElement) {
                                if (currentElement.hasAttribute('data-pip-zindex')) {
                                    currentElement.style.zIndex = '';
                                    currentElement.removeAttribute('data-pip-zindex');
                                }

                                // Find next parent module
                                currentElement = currentElement.parentElement?.closest('.module');
                            }

                            // Remove placeholder div from module-list
                            const placeholder = document.querySelector(`[data-pip-placeholder="${module.id}"]`);
                            if (placeholder) {
                                placeholder.remove();
                            }
                        }
                        setIsPiPMode(false);
                    }
                });
            },
            {
                root: null,
                rootMargin: '0px',
                threshold: 0.1
            }
        );

        observer.observe(containerRef.current);

        return () => {
            if (containerRef.current) {
                observer.unobserve(containerRef.current);
            }
        };
    }, [hasLoadedOnce, isPlaying]);

    // Listen for PiP mode requests from other videos
    useEffect(() => {
        const handlePiPRequest = (event) => {
            // If this video is in PiP mode but not playing, revert it
            if (event.detail.videoElement !== videoRef.current && isPiPMode && !isPlaying) {
                // Remove fixed height
                if (containerRef.current) {
                    containerRef.current.style.height = '';

                    // Remove z-index from all parent module elements
                    let currentElement = containerRef.current.closest('.module');

                    while (currentElement) {
                        if (currentElement.hasAttribute('data-pip-zindex')) {
                            currentElement.style.zIndex = '';
                            currentElement.removeAttribute('data-pip-zindex');
                        }

                        // Find next parent module
                        currentElement = currentElement.parentElement?.closest('.module');
                    }

                    // Remove placeholder div from module-list
                    const placeholder = document.querySelector(`[data-pip-placeholder="${module.id}"]`);
                    if (placeholder) {
                        placeholder.remove();
                    }
                }

                setIsPiPMode(false);
            }
        };

        window.addEventListener('requestPiPMode', handlePiPRequest);

        return () => {
            window.removeEventListener('requestPiPMode', handlePiPRequest);
        };
    }, [isPiPMode, isPlaying]);

    useEffect(() => {
        if (isDragging) {
            const handleMouseMove = (e) => handleDrag(e);
            const handleMouseUp = () => handleDragEnd();

            document.addEventListener('mousemove', handleMouseMove);
            document.addEventListener('mouseup', handleMouseUp);

            return () => {
                document.removeEventListener('mousemove', handleMouseMove);
                document.removeEventListener('mouseup', handleMouseUp);
            };
        }
    }, [isDragging, duration]);

    // Auto-upload a dropped video file when this module was created via drag & drop
    useEffect(() => {
        try {
            if (!module.autoUploadDroppedVideo || module.videoId) return;
            if (!window.__droppedVideoFiles) return;
            const file = window.__droppedVideoFiles[module.id];
            if (!file) return;

            // Clear the stored file so it is only used once
            delete window.__droppedVideoFiles[module.id];

            // Reuse the same upload logic used by the file input handler
            uploadVideoFile(file);
        } catch (err) {
            console.error('Error auto-uploading dropped video file:', err);
        }
    }, [module.id, module.autoUploadDroppedVideo, module.videoId]);

    // Auto-hide controls on initial load
    useEffect(() => {
        if (module.videoPath || module.videoId) {
            // Show controls initially, then hide after 3 seconds
            setShowControls(true);
            const timeout = setTimeout(() => {
                setShowControls(false);
            }, 3000);

            return () => clearTimeout(timeout);
        }
    }, [module.videoPath, module.videoId]);

    // Generate thumbnail if missing when video is ready to display
    useEffect(() => {
        const ensureThumbnail = async () => {
            if (!module.videoId || module.thumbnailPath || !module.downloaded) return;

            try {
                const response = await generateThumbnail(module.videoId);
                if (response.data?.success && response.data?.data?.thumbnailPath) {
                    moduleRef.current = {
                        ...moduleRef.current,
                        thumbnailPath: response.data.data.thumbnailPath
                    };
                    onUpdate(moduleRef.current);
                }
            } catch (err) {
                console.error('Error generating thumbnail:', err);
            }
        };

        ensureThumbnail();
    }, [module.videoId, module.thumbnailPath, module.downloaded]);

    const handleSaveThumbnailAtPosition = async () => {
        if (!module.videoId || !videoRef.current) return;

        const currentTime = videoRef.current.currentTime;

        // Show loading modal
        session.showModal(() => (
            <Modal title="Generating Thumbnail" hideButtons={true}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '12px', padding: '20px' }}>
                    <Icon name="progress_activity" className="spin" />
                    <span>Generating thumbnail...</span>
                </div>
            </Modal>
        ));

        try {
            const response = await generateThumbnail(module.videoId, currentTime);
            if (response.data?.success && response.data?.data?.thumbnailPath) {
                moduleRef.current = {
                    ...moduleRef.current,
                    thumbnailPath: response.data.data.thumbnailPath
                };
                onUpdate(moduleRef.current);
            }
        } catch (err) {
            console.error('Error generating thumbnail at position:', err);
        } finally {
            session.hideModal();
        }
    };

    // Update tab buttons when video player is displayed
    const isVideoPlayerVisible = module.videoPath && (module.downloaded !== false || downloadProgress >= 95);

    useEffect(() => {
        if (!tabButtons || !isVideoPlayerVisible) return;

        tabButtons([
            {
                icon: 'photo_camera',
                title: 'Save thumbnail at video position',
                callback: handleSaveThumbnailAtPosition
            }
        ]);
    }, [isVideoPlayerVisible, tabButtons]);

    // Keyboard shortcuts handler (YouTube-style)
    useEffect(() => {
        if (!isFocused) return;

        const handleKeyDown = (e) => {
            // Don't trigger shortcuts if user is typing in an input
            if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') {
                return;
            }

            if (!videoRef.current) return;
            e.preventDefault();
            switch (e.key.toLowerCase()) {
                case ' ':
                case 'k':
                    // Space or K: Play/Pause
                    togglePlay();
                    break;
                case 'arrowleft':
                    // Left arrow: Seek backward 5 seconds
                    seekRelative(-5);
                    break;
                case 'arrowright':
                    // Right arrow: Seek forward 5 seconds
                    seekRelative(5);
                    break;
                case 'j':
                    // J: Seek backward 10 seconds
                    seekRelative(-10);
                    break;
                case 'l':
                    // L: Seek forward 10 seconds
                    seekRelative(10);
                    break;
                case 'arrowup':
                    // Up arrow: Increase volume 5%
                    adjustVolume(0.05);
                    break;
                case 'arrowdown':
                    // Down arrow: Decrease volume 5%
                    adjustVolume(-0.05);
                    break;
                case 'm':
                    // M: Toggle mute
                    toggleMute();
                    break;
                case 'f':
                    // F: Toggle fullscreen
                    toggleFullscreen();
                    break;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    // 0-9: Seek to 0%-90% of video
                    const percent = parseInt(e.key) / 10;
                    seekToPercent(percent);
                    break;
                case 'home':
                    // Home: Seek to beginning
                    seekToPercent(0);
                    break;
                case 'end':
                    // End: Seek to end
                    seekToPercent(1);
                    break;
                case ',':
                    // Comma: Previous frame (when paused)
                    if (!isPlaying) {
                        seekRelative(-1 / 30); // Assuming 30fps
                    }
                    break;
                case '.':
                    // Period: Next frame (when paused)
                    if (!isPlaying) {
                        seekRelative(1 / 30); // Assuming 30fps
                    }
                    break;
            }
        };

        if (isFocused) document.addEventListener('keydown', handleKeyDown);
        return () => document.removeEventListener('keydown', handleKeyDown);
    }, [isFocused, isPlaying, volume, duration]);

    const uploadVideoFile = async (file) => {
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
                if (downloadTotal == 0) setDownloadTotal(e.total);
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

    // Consolidated handler for worker events
    const handleWorkerEvent = (eventName, payload, url = null) => {
        switch (eventName) {
            case 'VideoRecordCreated': {
                const data = payload;
                moduleRef.current = {
                    ...moduleRef.current,
                    videoId: data.id,
                    videoPath: data.videoPath,
                    url: url || module.url,
                    title: data.title,
                    downloaded: false
                };
                onUpdate(moduleRef.current);
                break;
            }
            case 'DownloadProgress': {
                const { progress, status } = payload || {};
                if (typeof progress === 'number') {
                    setDownloadProgress(progress);
                    // Show video player when generating seek preview thumbnails (95%+)
                    if (progress >= 95) {
                        setHasLoadedOnce(true);
                    }
                }
                if (status) setDownloadStatus(status);
                if (progress === 100) {
                    setIsDownloading(false);
                    setVideoUrl('');
                }
                break;
            }
            case 'DownloadComplete': {
                const data = payload || {};
                moduleRef.current = {
                    ...moduleRef.current,
                    videoPath: data.videoPath,
                    thumbnailPath: data.thumbnailPath,
                    downloaded: true,
                    entryId: data.entryId
                };
                onUpdate(moduleRef.current);
                setIsDownloading(false);
                setDownloadStatus('');
                setDownloadProgress(100);
                setVideoUrl('');
                setHasLoadedOnce(true);
                break;
            }
            case 'DownloadError': {
                const { message } = payload || {};
                setDownloadError(message || 'Error downloading video');
                setIsDownloading(false);
                setDownloadProgress(0);
                setDownloadStatus('');
                break;
            }
            default:
                break;
        }
    };

    // On mount, if this module has a pending download, attempt to reattach to an existing worker by moduleId
    useEffect(() => {
        const tryAttachToExistingWorker = async () => {
            try {
                if (!module.url || module.downloaded || isDownloading) return;

                const workers = await getWorkers();
                const worker = workers.find(w => w.customId === module.id);
                if (!worker) return;

                setIsDownloading(true);
                setDownloadError(null);

                await subscribe(worker.workerId, ({ eventName, payload }) => {
                    handleWorkerEvent(eventName, payload);
                });

                // Request current progress from the worker
                await requestProgress(worker.workerId);
            } catch (err) {
                console.error('Error attaching to existing video worker:', err);
            }
        };

        tryAttachToExistingWorker();
    }, [module.id]);

    const handleFileChange = async (e) => {
        if (!isEditable) return;

        const file = e.target.files[0];
        if (!file) return;

        uploadVideoFile(file);
    };

    const isYouTubeUrl = (url) => {
        return url.includes('youtube.com') || url.includes('youtu.be');
    };

    const handleDownloadVideo = async (url) => {
        if (!url.trim()) return;

        // Check if YouTube URL and if cookies exist
        if (isYouTubeUrl(url)) {
            let showmodal = false;
            try {
                const response = await checkYouTubeCookies();
                // Show modal if cookies don't exist
                const cookiesExist = response.data?.success && response.data?.data?.exists === true;
                if (!cookiesExist) {
                    // No cookies, show upload modal
                    setPendingDownloadUrl(url);
                    showmodal = true;
                }
            } catch (err) {
                console.warn('Failed to check cookies, showing upload modal:', err);
                // On error, show the modal to be safe
                setPendingDownloadUrl(url);
                showmodal = true;
            }
            if (showmodal) {
                session.showModal(() => (
                    <CookiesUploadModal
                        onClose={() => {
                            session.hideModal();
                            setPendingDownloadUrl(null);
                        }}
                        onSuccess={() => {
                            session.hideModal();
                            startDownload(url);
                            setPendingDownloadUrl(null);
                        }}
                    />
                ));
                return;
            }
        }

        startDownload(url);
    };

    const startDownload = async (url) => {
        const trimmedUrl = url.trim();
        setIsDownloading(true);
        setDownloadError(null);
        setDownloadProgress(0);
        setDownloadStatus('Starting download...');

        try {
            await callWorker('video-worker', 'DownloadVideo', {
                url: trimmedUrl,
                journalId: parseInt(journalId),
                entryId,
                moduleId: module.id
            }, ({ eventName, payload }) => {
                handleWorkerEvent(eventName, payload, trimmedUrl);
            }, module.id, window.location.href);
        } catch (err) {
            console.error('Error calling video worker:', err);
            setDownloadError('Error connecting to download service. Please try again.');
            setIsDownloading(false);
            setDownloadProgress(0);
            setDownloadStatus('');
        }
    };

    const handleUrlKeyDown = (e) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            handleDownloadVideo(videoUrl);
        }
    };

    const handleUrlPaste = (e) => {
        const pastedText = e.clipboardData.getData('text');
        if (pastedText && pastedText.trim()) {
            e.preventDefault();
            const url = pastedText.trim();
            setVideoUrl(url);
            handleDownloadVideo(url);
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

    // Video player controls
    const togglePlay = () => {
        if (!videoRef.current) return;
        if (isPlaying) {
            videoRef.current.pause();
        } else {
            videoRef.current.play();
        }
        setIsPlaying(!isPlaying);
    };

    const handleTimeUpdate = () => {
        if (!videoRef.current) return;
        const time = videoRef.current.currentTime;
        currentTimeRef.current = time; // Keep ref in sync for PiP
        setCurrentTime(time);
        updateBufferedProgress();
    };

    const handleLoadedMetadata = () => {
        if (!videoRef.current) return;
        const videoDuration = videoRef.current.duration;
        setDuration(videoDuration);

        // Apply saved volume to video element
        const savedVolume = localStorage.getItem('collector:video-player:volume');
        if (savedVolume !== null) {
            videoRef.current.volume = parseFloat(savedVolume);
        }

        // Check for pending PiP restoration
        if (pendingPipRestoreRef.current) {
            const pipState = pendingPipRestoreRef.current;
            pendingPipRestoreRef.current = null;

            // Scroll to the module container (multiple attempts for lazy loading)
            if (pipState.scrollToVideo) {
                const scrollToModule = () => {
                    const moduleElement = containerRef.current?.closest('.module');
                    if (moduleElement) {
                        moduleElement.scrollIntoView({ behavior: 'auto', block: 'center' });
                    }
                };
                scrollToModule();
                setTimeout(scrollToModule, 300);
            }

            // Restore playback position and state
            videoRef.current.currentTime = pipState.currentTime || 0;
            videoRef.current.muted = pipState.isMuted || false;
            setIsMuted(pipState.isMuted || false);

            // Play the video
            videoRef.current.play().catch((e) => console.error('[VideoPlayer] Play failed:', e));

            // Clear the global PiP video
            clearPipVideoRef.current?.();
        }

        // Start preloading all thumbnails in the background
        preloadAllThumbnails(videoDuration);
    };

    const updateBufferedProgress = () => {
        if (!videoRef.current || !duration) return;

        const buffered = videoRef.current.buffered;
        const currentTimePercent = (videoRef.current.currentTime / duration) * 100;
        const ranges = [];

        // Collect all buffered time ranges that contain or are near the current time
        for (let i = 0; i < buffered.length; i++) {
            const startPercent = (buffered.start(i) / duration) * 100;
            const endPercent = (buffered.end(i) / duration) * 100;

            // Only show buffered ranges that include the current position or are ahead of it
            if (endPercent >= currentTimePercent - 1) {
                ranges.push({ start: startPercent, end: endPercent });
            }
        }

        setBufferedRanges(ranges);
    };

    const handleSeek = (e) => {
        if (!videoRef.current || !seekBarRef.current || isDragging) return;
        const rect = seekBarRef.current.getBoundingClientRect();
        const pos = (e.clientX - rect.left) / rect.width;
        const time = pos * duration;
        videoRef.current.currentTime = time;
        setCurrentTime(time);
        updateBufferedProgress();
    };

    const handleDragStart = (e) => {
        e.stopPropagation();
        setIsDragging(true);
    };

    const handleDrag = (e) => {
        if (!isDragging || !videoRef.current || !seekBarRef.current) return;
        const rect = seekBarRef.current.getBoundingClientRect();
        const pos = Math.max(0, Math.min((e.clientX - rect.left) / rect.width, 1));
        const time = pos * duration;
        videoRef.current.currentTime = time;
        setCurrentTime(time);
    };

    const handleDragEnd = () => {
        setIsDragging(false);
        updateBufferedProgress();
    };

    const handleSeekHover = async (e) => {
        if (!seekBarRef.current || !videoRef.current) return;

        // Wait for any pending thumbnail load to complete
        if (pendingLoadRef.current) return;

        const rect = seekBarRef.current.getBoundingClientRect();
        const pos = (e.clientX - rect.left) / rect.width;
        const time = Math.max(0, Math.min(pos * duration, duration));
        setPreviewTime(time);

        // Calculate preview position with bounds checking
        // Preview is 10em wide (approximately 160px), centered with translateX(-50%)
        const previewWidth = 160; // 10em in pixels (approximate)
        const halfPreviewWidth = previewWidth / 2;
        let position = e.clientX - rect.left;

        // Constrain position to keep preview within bounds
        position = Math.max(halfPreviewWidth, Math.min(position, rect.width - halfPreviewWidth));

        setPreviewPosition(position);
        setShowPreview(true);

        // Round down to nearest 10 seconds (0, 10, 20, 30, etc.)
        const roundedTime = Math.floor(time / 10) * 10;

        // Load thumbnail blob and track the promise
        const loadPromise = loadPreviewThumbnail(roundedTime);
        pendingLoadRef.current = loadPromise;
        await loadPromise;
        pendingLoadRef.current = null;
    };

    const handleSeekLeave = () => {
        setShowPreview(false);
        setPreviewBlobUrl(null);
    };

    const buildPreviewThumbnailUrl = (time) => {
        if (!module.videoPath) return null;

        // Extract entry ID and video filename from videoPath
        const pathParts = module.videoPath.split('/');
        if (pathParts.length < 2) return null;

        const entryIdFromPath = pathParts[0];
        const effectiveEntryId = module.entryId || entryIdFromPath;
        const videoFileName = pathParts[pathParts.length - 1];

        return `${apiBasePath()}/video/preview/${effectiveEntryId}/${videoFileName}/${time}`;
    };

    const loadPreviewThumbnail = async (time) => {
        const url = buildPreviewThumbnailUrl(time);
        if (!url) return;

        // Check if already cached (including 404s cached as null)
        if (url in thumbnailCacheRef.current) {
            const cachedValue = thumbnailCacheRef.current[url];
            if (cachedValue) {
                setPreviewBlobUrl(cachedValue);
            }
            return;
        }

        // Check if already downloading
        if (downloadingRef.current.has(url)) {
            return;
        }

        try {
            // Mark as downloading
            downloadingRef.current.add(url);

            // Fetch the image
            const response = await fetch(url);
            if (!response.ok) {
                // Cache 404s as null to prevent repeated requests
                thumbnailCacheRef.current[url] = null;
                return;
            }

            // Convert to blob, then to base64 data URL
            const blob = await response.blob();
            const base64DataUrl = await new Promise((resolve, reject) => {
                const reader = new FileReader();
                reader.onloadend = () => resolve(reader.result);
                reader.onerror = reject;
                reader.readAsDataURL(blob);
            });

            // Cache the base64 data URL
            thumbnailCacheRef.current[url] = base64DataUrl;
            setPreviewBlobUrl(base64DataUrl);
        } catch (error) {
            console.error('Error loading preview thumbnail:', error);
            // Cache failed requests as null to prevent repeated attempts
            thumbnailCacheRef.current[url] = null;
        } finally {
            // Remove from downloading set
            downloadingRef.current.delete(url);
        }
    };

    const preloadAllThumbnails = async (videoDuration) => {
        if (!module.videoPath || !videoDuration) return;

        // Generate array of all 10-second intervals
        const times = [];
        for (let second = 0; second < videoDuration; second += 10) {
            times.push(second);
        }

        // Process in batches of 5
        const batchSize = 5;
        for (let i = 0; i < times.length; i += batchSize) {
            const batch = times.slice(i, i + batchSize);

            // Download batch in parallel
            await Promise.all(batch.map(async (time) => {
                const url = buildPreviewThumbnailUrl(time);
                if (!url) return;

                // Skip if already cached
                if (url in thumbnailCacheRef.current) {
                    return;
                }

                // Skip if already downloading
                if (downloadingRef.current.has(url)) {
                    return;
                }

                try {
                    downloadingRef.current.add(url);

                    const response = await fetch(url);
                    if (!response.ok) {
                        thumbnailCacheRef.current[url] = null;
                        return;
                    }

                    // Convert to blob, then to base64 data URL
                    const blob = await response.blob();
                    const base64DataUrl = await new Promise((resolve, reject) => {
                        const reader = new FileReader();
                        reader.onloadend = () => resolve(reader.result);
                        reader.onerror = reject;
                        reader.readAsDataURL(blob);
                    });

                    thumbnailCacheRef.current[url] = base64DataUrl;
                } catch (error) {
                    console.error(`Error preloading thumbnail at ${time}s:`, error);
                    thumbnailCacheRef.current[url] = null;
                } finally {
                    downloadingRef.current.delete(url);
                }
            }));
        }

        //console.warn(`Preloaded ${times.length} thumbnails for video`);
    };


    const handleVolumeChange = (e) => {
        const newVolume = parseFloat(e.target.value);
        setVolume(newVolume);
        localStorage.setItem('collector:video-player:volume', newVolume);
        if (videoRef.current) {
            videoRef.current.volume = newVolume;
        }
        setIsMuted(newVolume === 0);
    };

    const toggleMute = () => {
        if (!videoRef.current) return;
        const newMuted = !isMuted;
        setIsMuted(newMuted);
        videoRef.current.muted = newMuted;
    };

    const toggleFullscreen = () => {
        if (!videoPlayerRef.current) return;
        if (document.fullscreenElement) {
            document.exitFullscreen();
        } else {
            videoPlayerRef.current.requestFullscreen();
        }
    };

    // Listen for fullscreen changes
    useEffect(() => {
        const handleFullscreenChange = () => {
            setIsFullscreen(!!document.fullscreenElement);
        };
        document.addEventListener('fullscreenchange', handleFullscreenChange);
        return () => document.removeEventListener('fullscreenchange', handleFullscreenChange);
    }, []);

    const seekRelative = (seconds) => {
        if (!videoRef.current) return;
        const newTime = Math.max(0, Math.min(videoRef.current.currentTime + seconds, duration));
        videoRef.current.currentTime = newTime;
        setCurrentTime(newTime);
        updateBufferedProgress();
    };

    const seekToPercent = (percent) => {
        if (!videoRef.current || !duration) return;
        const newTime = duration * percent;
        videoRef.current.currentTime = newTime;
        setCurrentTime(newTime);
        updateBufferedProgress();
    };

    const adjustVolume = (delta) => {
        if (!videoRef.current) return;
        const newVolume = Math.max(0, Math.min(volume + delta, 1));
        setVolume(newVolume);
        videoRef.current.volume = newVolume;
        setIsMuted(newVolume === 0);
    };

    const handleVideoMouseEnter = () => {
        setShowControls(true);
        setIsFocused(true);
        resetControlsTimeout();
    };

    const handleVideoMouseMove = () => {
        setShowControls(true);
        setIsFocused(true);
        resetControlsTimeout();
    };

    const handleVideoMouseLeave = () => {
        setShowControls(false);
        setIsFocused(false);
        if (controlsTimeoutRef.current) {
            clearTimeout(controlsTimeoutRef.current);
            controlsTimeoutRef.current = null;
        }
    };

    const handleVideoClick = () => {
        setIsFocused(true);
        togglePlay();
    };

    const handleScrollToVideo = () => {
        if (containerRef.current) {
            containerRef.current.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    };

    const resetControlsTimeout = () => {
        if (controlsTimeoutRef.current) {
            clearTimeout(controlsTimeoutRef.current);
        }
        controlsTimeoutRef.current = setTimeout(() => {
            setShowControls(false);
        }, 3000);
    };

    const formatTime = (seconds, padMinutes = false) => {
        if (!seconds || isNaN(seconds)) return padMinutes ? '00:00' : '0:00';
        const mins = Math.floor(seconds / 60);
        const secs = Math.floor(seconds % 60);
        const minStr = padMinutes ? mins.toString().padStart(2, '0') : mins.toString();
        return `${minStr}:${secs.toString().padStart(2, '0')}`;
    };

    const hasVideo = module.videoPath;

    return (
        <div className="video-player-module" ref={containerRef}>
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
                            onPaste={handleUrlPaste}
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

            {isDownloading && downloadProgress < 95 && (
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
                        handleDownloadVideo(module.url);
                    }}>
                        <Icon name="download" />
                        Try Again
                    </button>
                </div>
            )}

            {(module.videoPath && (module.downloaded !== false || downloadProgress >= 95)) && (
                <div className={`video-preview ${isPiPMode ? 'pip-mode' : ''}`}>
                    {module.url && !isPiPMode && (
                        <div className="video-url-icon">
                            <a href={module.url} target="_blank" rel="noopener noreferrer" title="Open original video URL">
                                <Icon name="arrow_outward" />
                            </a>
                        </div>
                    )}
                    {isPiPMode && (
                        <div className="video-url-icon">
                            <button onClick={handleScrollToVideo} title="Scroll to video">
                                <Icon name="vertical_align_top" />
                            </button>
                        </div>
                    )}
                    <div
                        ref={videoPlayerRef}
                        className={`custom-video-player${isFullscreen ? ' fullscreen' : ''}`}
                        onMouseEnter={handleVideoMouseEnter}
                        onMouseMove={handleVideoMouseMove}
                        onMouseLeave={handleVideoMouseLeave}
                    >
                        <video
                            ref={videoRef}
                            controls={false}
                            poster={module.thumbnailPath ? apiBasePath() + `/video/thumb/${module.thumbnailPath}` : undefined}
                            onTimeUpdate={handleTimeUpdate}
                            onLoadedMetadata={handleLoadedMetadata}
                            onPlay={() => setIsPlaying(true)}
                            onPause={() => setIsPlaying(false)}
                            onProgress={updateBufferedProgress}
                            onClick={handleVideoClick}
                        >
                            <source
                                src={module.videoId ? apiBasePath() + `/video/${module.videoId}` : apiBasePath() + `/video/${module.videoPath}`}
                                type="video/mp4"
                            />
                            Your browser does not support the video tag.
                        </video>

                        <div className={`video-controls ${!showControls ? 'hidden' : ''}`}>
                            <div
                                className="seek-bar-container"
                                ref={seekBarRef}
                                onClick={handleSeek}
                                onMouseMove={handleSeekHover}
                                onMouseLeave={handleSeekLeave}
                            >
                                <div className="seek-bar">
                                    <div
                                        className="seek-bar-progress"
                                        style={{ width: `${(currentTime / duration) * 100}%` }}
                                    />
                                    {bufferedRanges.map((range, index) => (
                                        <div
                                            key={index}
                                            className="seek-bar-buffered"
                                            style={{
                                                left: `${range.start}%`,
                                                width: `${range.end - range.start}%`
                                            }}
                                        />
                                    ))}
                                    <div
                                        className="seek-bar-handle"
                                        style={{ left: `${(currentTime / duration) * 100}%` }}
                                        onMouseDown={handleDragStart}
                                    />
                                </div>

                                {showPreview && (
                                    <div
                                        className="seek-preview"
                                        style={{ left: `${previewPosition}px` }}
                                    >
                                        {previewBlobUrl ? (
                                            <img
                                                src={previewBlobUrl}
                                                alt="Preview"
                                                onError={(e) => e.target.style.display = 'none'}
                                            />
                                        ) : (
                                            <div className="preview-loading">Loading...</div>
                                        )}
                                        <div className="preview-time">{formatTime(previewTime)}</div>
                                    </div>
                                )}
                            </div>

                            <div className="video-controls-row">
                                <button className="play-button" onClick={togglePlay}>
                                    <Icon name={isPlaying ? 'pause' : 'play_arrow'} />
                                </button>

                                <div className="time-display">
                                    {formatTime(currentTime, duration >= 600)} / {formatTime(duration)}
                                </div>

                                <div className="volume-controls">
                                    <button className="volume-button" onClick={toggleMute}>
                                        <Icon name={isMuted || volume === 0 ? 'volume_off' : volume < 0.5 ? 'volume_down' : 'volume_up'} />
                                    </button>

                                    <input
                                        type="range"
                                        className="volume-slider"
                                        min="0"
                                        max="1"
                                        step="0.01"
                                        value={isMuted ? 0 : volume}
                                        onChange={handleVolumeChange}
                                        style={{
                                            background: `linear-gradient(to right, rgba(119, 99, 237, 0.7) 0%, rgba(119, 99, 237, 0.7) ${(isMuted ? 0 : volume) * 100}%, rgba(255, 255, 255, 0.3) ${(isMuted ? 0 : volume) * 100}%, rgba(255, 255, 255, 0.3) 100%)`
                                        }}
                                    />
                                </div>

                                <button className="fullscreen-button" onClick={toggleFullscreen}>
                                    <Icon name="fullscreen" />
                                </button>
                            </div>
                        </div>
                    </div>
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
