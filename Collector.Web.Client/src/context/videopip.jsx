import React, { createContext, useContext, useState, useRef, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { apiBasePath } from '@/helpers/endpoints.js';
import Icon from '@/components/ui/icon';

const VideoPiPContext = createContext(null);

export function VideoPiPProvider({ children }) {
    const navigate = useNavigate();
    
    // State for the PiP video
    const [pipVideo, setPipVideo] = useState(null);
    const [isPlaying, setIsPlaying] = useState(false);
    const [currentTime, setCurrentTime] = useState(0);
    const [duration, setDuration] = useState(0);
    const [volume, setVolume] = useState(1);
    const [isMuted, setIsMuted] = useState(false);
    const [showControls, setShowControls] = useState(true);
    const [bufferedRanges, setBufferedRanges] = useState([]);
    const [isDragging, setIsDragging] = useState(false);

    // Refs
    const videoRef = useRef(null);
    const seekBarRef = useRef(null);
    const controlsTimeoutRef = useRef(null);
    const initialTimeRef = useRef(0); // Store initial time to restore on video load

    // Register a video for PiP mode when navigating away
    const registerPipVideo = useCallback((videoData) => {
        window.__currentPipModuleId = videoData.moduleId;
        initialTimeRef.current = videoData.currentTime || 0; // Store for restoration
        setPipVideo(videoData);
        setCurrentTime(videoData.currentTime || 0);
        setDuration(videoData.duration || 0);
        setVolume(videoData.volume || 1);
        setIsMuted(videoData.isMuted || false);
        setIsPlaying(true);
    }, []);

    // Clear the PiP video
    const clearPipVideo = useCallback(() => {
        window.__currentPipModuleId = null;
        setPipVideo(null);
        setIsPlaying(false);
        setCurrentTime(0);
        setDuration(0);
    }, [pipVideo]);

    // Pause the PiP video without clearing it
    const pausePipVideo = useCallback(() => {
        if (videoRef.current && !videoRef.current.paused) {
            videoRef.current.pause();
            setIsPlaying(false);
        }
    }, []);

    // Get current PiP video state (for restoring when navigating back)
    const getPipVideoState = useCallback(() => {
        if (!pipVideo || !videoRef.current) return null;
        return {
            moduleId: pipVideo.moduleId,
            entryId: pipVideo.entryId,
            journalId: pipVideo.journalId,
            currentTime: videoRef.current.currentTime,
            duration: videoRef.current.duration,
            volume: videoRef.current.volume,
            isMuted: videoRef.current.muted,
            isPlaying: !videoRef.current.paused
        };
    }, [pipVideo]);

    // Video control handlers
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
        setDuration(videoRef.current.duration);
        // Restore playback position from ref (set by registerPipVideo)
        if (initialTimeRef.current > 0) {
            videoRef.current.currentTime = initialTimeRef.current;
            initialTimeRef.current = 0; // Reset after use
        }
        // Auto-play
        videoRef.current.play().catch(() => {});
    };

    const updateBufferedProgress = () => {
        if (!videoRef.current || !duration) return;
        const buffered = videoRef.current.buffered;
        const currentTimePercent = (videoRef.current.currentTime / duration) * 100;
        const ranges = [];
        for (let i = 0; i < buffered.length; i++) {
            const startPercent = (buffered.start(i) / duration) * 100;
            const endPercent = (buffered.end(i) / duration) * 100;
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
        const container = videoRef.current?.parentElement;
        if (!container) return;
        if (document.fullscreenElement) {
            document.exitFullscreen();
        } else {
            container.requestFullscreen();
        }
    };

    const handleVideoMouseEnter = () => {
        setShowControls(true);
        resetControlsTimeout();
    };

    const handleVideoMouseMove = () => {
        setShowControls(true);
        resetControlsTimeout();
    };

    const handleVideoMouseLeave = () => {
        setShowControls(false);
        if (controlsTimeoutRef.current) {
            clearTimeout(controlsTimeoutRef.current);
            controlsTimeoutRef.current = null;
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

    const handleNavigateToVideo = () => {
        if (!pipVideo) return;
        // Store current state before navigation for restoration
        const state = getPipVideoState();
        if (state) {
            window.__pipVideoState = {
                ...state,
                scrollToVideo: true // Flag to scroll to video and restore
            };
        }
        // Navigate to the entry using React Router
        navigate(`/journal/${pipVideo.journalId}/entry/${pipVideo.entryId}`);
    };

    const value = {
        pipVideo,
        registerPipVideo,
        clearPipVideo,
        pausePipVideo,
        getPipVideoState,
        videoRef
    };

    return (
        <VideoPiPContext.Provider value={value}>
            {children}
            {pipVideo && (
                <div 
                    className="global-pip-container"
                    onMouseEnter={handleVideoMouseEnter}
                    onMouseMove={handleVideoMouseMove}
                    onMouseLeave={handleVideoMouseLeave}
                >
                    <div className="pip-header">
                        <button className="pip-navigate" onClick={handleNavigateToVideo} title="Go to video">
                            <Icon name="open_in_new" />
                        </button>
                        <button className="pip-close" onClick={clearPipVideo} title="Close">
                            <Icon name="close" />
                        </button>
                    </div>
                    <video
                        key={pipVideo.moduleId + '-' + pipVideo.entryId}
                        ref={videoRef}
                        controls={false}
                        poster={pipVideo.thumbnailPath ? apiBasePath() + `/video/thumb/${pipVideo.thumbnailPath}` : undefined}
                        onTimeUpdate={handleTimeUpdate}
                        onLoadedMetadata={handleLoadedMetadata}
                        onPlay={() => setIsPlaying(true)}
                        onPause={() => setIsPlaying(false)}
                        onProgress={updateBufferedProgress}
                        onEnded={clearPipVideo}
                        onClick={togglePlay}
                    >
                        <source
                            src={pipVideo.videoId ? apiBasePath() + `/video/${pipVideo.videoId}` : apiBasePath() + `/video/${pipVideo.videoPath}`}
                            type="video/mp4"
                        />
                    </video>
                    <div className={`pip-controls ${!showControls ? 'hidden' : ''}`}>
                        <div 
                            className="pip-seek-bar"
                            ref={seekBarRef}
                            onClick={handleSeek}
                        >
                            <div 
                                className="pip-seek-progress" 
                                style={{ width: `${(currentTime / duration) * 100}%` }}
                            />
                            {bufferedRanges.map((range, index) => (
                                <div 
                                    key={index}
                                    className="pip-seek-buffered" 
                                    style={{ 
                                        left: `${range.start}%`,
                                        width: `${range.end - range.start}%` 
                                    }}
                                />
                            ))}
                        </div>
                        <div className="pip-controls-row">
                            <button className="pip-play-button" onClick={togglePlay}>
                                <Icon name={isPlaying ? 'pause' : 'play_arrow'} />
                            </button>
                            <div className="pip-time">
                                {formatTime(currentTime)} / {formatTime(duration)}
                            </div>
                            <div className="pip-volume">
                                <button onClick={toggleMute}>
                                    <Icon name={isMuted || volume === 0 ? 'volume_off' : 'volume_up'} />
                                </button>
                                <input
                                    type="range"
                                    min="0"
                                    max="1"
                                    step="0.01"
                                    value={isMuted ? 0 : volume}
                                    onChange={handleVolumeChange}
                                />
                            </div>
                            <button className="fullscreen-button" onClick={toggleFullscreen}>
                                <Icon name="fullscreen" />
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </VideoPiPContext.Provider>
    );
}

export function useVideoPiP() {
    const context = useContext(VideoPiPContext);
    if (!context) {
        throw new Error('useVideoPiP must be used within a VideoPiPProvider');
    }
    return context;
}
