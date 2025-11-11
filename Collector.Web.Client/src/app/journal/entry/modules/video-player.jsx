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
    const [isPlaying, setIsPlaying] = useState(false);
    const [currentTime, setCurrentTime] = useState(0);
    const [duration, setDuration] = useState(0);
    const [volume, setVolume] = useState(1);
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

    //context
    const session = useSession();
    const { uploadVideo, deleteVideo } = Videos(session);

    useEffect(() => {
        moduleRef.current = module;
        if (!setDeleteListener) return;
        setDeleteListener(module, removeModule);
    }, [module.id]);

    // Pause other videos when this one starts playing
    useEffect(() => {
        if (isPlaying && videoRef.current) {
            // Dispatch custom event to pause other videos
            const event = new CustomEvent('videoPlaying', { 
                detail: { videoElement: videoRef.current } 
            });
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
                            let zIndex = 9999;
                            
                            while (currentElement) {
                                currentElement.style.zIndex = zIndex.toString();
                                currentElement.setAttribute('data-pip-zindex', 'true');
                                zIndex--;
                                
                                // Find next parent module
                                currentElement = currentElement.parentElement?.closest('.module');
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
            switch(e.key.toLowerCase()) {
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
                        seekRelative(-1/30); // Assuming 30fps
                    }
                    break;
                case '.':
                    // Period: Next frame (when paused)
                    if (!isPlaying) {
                        seekRelative(1/30); // Assuming 30fps
                    }
                    break;
            }
        };

        if(isFocused) document.addEventListener('keydown', handleKeyDown);
        return () => document.removeEventListener('keydown', handleKeyDown);
    }, [isFocused, isPlaying, volume, duration]);      

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
                if(progress == 100) {
                    setIsDownloading(false);
                    setDownloadProgress(0);
                    setDownloadStatus('');
                    setVideoUrl('');
                    // Close connection after download completes
                    conn.stop();
                }
            });

            // Listen for download completion
            conn.on('DownloadComplete', (data) => {
                moduleRef.current = {
                    ...moduleRef.current,
                    thumbnailPath: data.thumbnailPath,
                    downloaded: true
                };
                onUpdate(moduleRef.current);
            });

            // Listen for download errors
            conn.on('DownloadError', (error) => {
                console.error('Error downloading video:', error);
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
        setCurrentTime(videoRef.current.currentTime);
        updateBufferedProgress();
    };

    const handleLoadedMetadata = () => {
        if (!videoRef.current) return;
        const videoDuration = videoRef.current.duration;
        setDuration(videoDuration);
        
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
        const videoFileName = pathParts[pathParts.length - 1];
        
        return `${apiBasePath()}/video/preview/${entryIdFromPath}/${videoFileName}/${time}`;
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

            {!isDownloading && module.videoPath && module.downloaded !== false && hasLoadedOnce && (
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
                        className="custom-video-player"
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
