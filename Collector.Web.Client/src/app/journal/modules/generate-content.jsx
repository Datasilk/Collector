import React, { useState, useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import Icon from '@/components/ui/icon';
import { apiBasePath } from '@/helpers/endpoints.js';
import { useSession } from '@/context/session';
import GenerateContentModal from './generate-content/generate-content-modal';
import './generate-content.css';

export default function GenerateContentModule({ 
    module, 
    entryId, 
    journalId, 
    onUpdate, 
    isEditable = true, 
    manuallyAdded = false,
    tabButtons,
    onAddModuleAbove
}) {
    const session = useSession();
    const moduleRef = useRef(module);
    
    const [isGenerating, setIsGenerating] = useState(false);
    const [isComplete, setIsComplete] = useState(false);
    const [progress, setProgress] = useState(0);
    const [statusMessage, setStatusMessage] = useState('');
    const [url, setUrl] = useState(module.url || '');
    const [includeVideo, setIncludeVideo] = useState(module.includeVideo ?? true);
    const [includeTitle, setIncludeTitle] = useState(module.includeTitle ?? true);
    const [includeDescription, setIncludeDescription] = useState(module.includeDescription ?? true);
    const [includeTranscriptResearch, setIncludeTranscriptResearch] = useState(module.includeTranscriptResearch ?? false);
    const [includeCommentsResearch, setIncludeCommentsResearch] = useState(module.includeCommentsResearch ?? false);
    const [generateChapters, setGenerateChapters] = useState(module.generateChapters ?? true);
    const [chapterCount, setChapterCount] = useState(module.chapterCount || 'any');
    const [userInstructions, setUserInstructions] = useState(module.userInstructions || '');
    const [cache, setCache] = useState(module.cache || null);
    const [commentsRetrievalFailed, setCommentsRetrievalFailed] = useState(false);
    const hubConnectionRef = useRef(null);

    useEffect(() => {
        moduleRef.current = module;
    }, [module]);

    useEffect(() => {
        if (manuallyAdded && !module.url) {
            handleShowSettings();
        }
    }, [manuallyAdded, module.url]);

    const isYouTubeUrl = (urlString) => {
        if (!urlString) return false;
        const youtubePattern = /^(https?:\/\/)?(www\.|m\.)?(youtube\.com\/(watch\?v=|embed\/|v\/)|youtu\.be\/)[\w-]+/i;
        return youtubePattern.test(urlString);
    };

    const handleGenerate = async (settings) => {
        const generateUrl = settings?.url || url;
        const generateIncludeVideo = settings?.includeVideo ?? includeVideo;
        const generateIncludeTitle = settings?.includeTitle ?? includeTitle;
        const generateIncludeDescription = settings?.includeDescription ?? includeDescription;
        const generateIncludeTranscriptResearch = settings?.includeTranscriptResearch ?? includeTranscriptResearch;
        const generateIncludeCommentsResearch = settings?.includeCommentsResearch ?? includeCommentsResearch;
        const generateGenerateChapters = settings?.generateChapters ?? generateChapters;
        const generateChapterCount = settings?.chapterCount ?? chapterCount;
        const generateUserInstructions = settings?.userInstructions ?? userInstructions;

        if (!generateUrl.trim() || !isYouTubeUrl(generateUrl)) {
            alert('Please enter a valid YouTube URL');
            return;
        }

        setIsGenerating(true);
        setIsComplete(false);
        setProgress(0);
        setStatusMessage('Starting...');
        setCommentsRetrievalFailed(false);

        const updatedModule = {
            ...moduleRef.current,
            url: generateUrl,
            includeVideo: generateIncludeVideo,
            includeTitle: generateIncludeTitle,
            includeDescription: generateIncludeDescription,
            includeTranscriptResearch: generateIncludeTranscriptResearch,
            includeCommentsResearch: generateIncludeCommentsResearch,
            generateChapters: generateGenerateChapters,
            chapterCount: generateChapterCount,
            userInstructions: generateUserInstructions
        };
        moduleRef.current = updatedModule;
        onUpdate(updatedModule);

        const webContentHub = new signalR.HubConnectionBuilder()
            .withUrl(apiBasePath() + '/web-content', {
                withCredentials: true,
                skipNegotiation: true,
                transport: signalR.HttpTransportType.WebSockets
            })
            .withAutomaticReconnect([0, 1000, 5000, 10000])
            .configureLogging(signalR.LogLevel.Information)
            .build();

        hubConnectionRef.current = webContentHub;

        try {
            webContentHub.on('ScrapeStatus', (status) => {
                setStatusMessage(status);
            });

            webContentHub.on('GenerateStatus', (status) => {
                setStatusMessage(status);
                if (status === 'Content generation complete!') {
                    setIsGenerating(false);
                    setIsComplete(true);
                }
            });

            webContentHub.on('GenerateProgress', (progressValue) => {
                setProgress(progressValue);
            });

            webContentHub.on('ModuleGenerated', (moduleData) => {
                if (moduleData && onAddModuleAbove) {
                    onAddModuleAbove(moduleData.type, moduleData);
                }
            });

            webContentHub.on('CacheUpdate', (cacheData) => {
                if (cacheData === "null" || cacheData === null) {
                    setCache(null);
                    const updatedModule = {
                        ...moduleRef.current,
                        cache: null
                    };
                    moduleRef.current = updatedModule;
                    onUpdate(updatedModule);
                } else {
                    const parsedCache = JSON.parse(cacheData);
                    setCache(parsedCache);
                    
                    // Restore settings from cache if available
                    if (parsedCache.GenerateChapters !== undefined) {
                        setGenerateChapters(parsedCache.GenerateChapters);
                    }
                    if (parsedCache.ChapterCount !== undefined) {
                        setChapterCount(parsedCache.ChapterCount);
                    }
                    if (parsedCache.UserInstructions !== undefined) {
                        setUserInstructions(parsedCache.UserInstructions);
                    }
                    
                    // Update module with cache and restored settings
                    const updatedModule = {
                        ...moduleRef.current,
                        cache: parsedCache,
                        generateChapters: parsedCache.GenerateChapters ?? moduleRef.current.generateChapters,
                        chapterCount: parsedCache.ChapterCount ?? moduleRef.current.chapterCount,
                        userInstructions: parsedCache.UserInstructions ?? moduleRef.current.userInstructions
                    };
                    moduleRef.current = updatedModule;
                    onUpdate(updatedModule);
                }
            });

            webContentHub.on('CommentsRetrievalFailed', (error) => {
                console.error('Comments retrieval failed:', error);
                setStatusMessage(error);
                setIsGenerating(false);
                setCommentsRetrievalFailed(true);
            });

            webContentHub.on('OnError', (error) => {
                console.error('Content generation error:', error);
                setStatusMessage(`Error: ${error}`);
                setIsGenerating(false);
            });

            webContentHub.on('GenerateError', (error) => {
                console.error('Content generation error:', error);
                setStatusMessage(`Error: ${error}`);
                setIsGenerating(false);
            });

            await webContentHub.start();
            
            const appUserId = session?.user?.appUserId;
            webContentHub.invoke('GenerateContent', 
                generateUrl,
                generateIncludeVideo,
                generateIncludeTitle,
                generateIncludeDescription,
                generateIncludeTranscriptResearch,
                generateIncludeCommentsResearch,
                appUserId,
                cache ? JSON.stringify(cache) : null,
                generateUserInstructions,
                generateGenerateChapters,
                generateChapterCount
            ).catch(err => {
                console.error('Error invoking GenerateContent:', err);
                setStatusMessage('Error starting content generation.');
                setIsGenerating(false);
            });

        } catch (err) {
            console.error('Error generating content:', err);
            setStatusMessage('Error generating content. Please try again.');
            setIsGenerating(false);
        }
    };

    const handleShowSettings = () => {
        const handleModalGenerate = (settings) => {
            setUrl(settings.url);
            setIncludeVideo(settings.includeVideo);
            setIncludeTitle(settings.includeTitle);
            setIncludeDescription(settings.includeDescription);
            setIncludeTranscriptResearch(settings.includeTranscriptResearch);
            setIncludeCommentsResearch(settings.includeCommentsResearch);
            setGenerateChapters(settings.generateChapters);
            setChapterCount(settings.chapterCount);
            setUserInstructions(settings.userInstructions);
            
            handleGenerate(settings);
        };

        session.showModal(() => (
            <GenerateContentModal
                initialUrl={url}
                initialIncludeVideo={includeVideo}
                initialIncludeTitle={includeTitle}
                initialIncludeDescription={includeDescription}
                initialIncludeTranscriptResearch={includeTranscriptResearch}
                initialIncludeCommentsResearch={includeCommentsResearch}
                initialGenerateChapters={generateChapters}
                initialChapterCount={chapterCount}
                initialUserInstructions={userInstructions}
                onGenerate={handleModalGenerate}
                onClose={() => session.hideModal()}
            />
        ));
    };

    useEffect(() => {
        if (!isEditable || !tabButtons || typeof tabButtons !== 'function') return;
        
        const buttons = [];
        if (module.url) {
            buttons.push({
                icon: 'settings',
                title: 'Content Generator Settings',
                callback: handleShowSettings
            });
        }
        tabButtons(buttons);
    }, [module.url, isEditable, handleShowSettings, tabButtons]);

    if (!isEditable) {
        return null;
    }

    return (
        <div className="generate-content-module">

            {isGenerating && (
                <div className="generate-content-status">
                    <div className="status-header">
                        <Icon name="progress_activity" spin={true} />
                        <span>{statusMessage || 'Generating content...'}</span>
                    </div>
                    <div className="progress-bar-container">
                        <div className="progress-bar">
                            <div 
                                className="progress-bar-fill" 
                                style={{ width: `${progress}%` }}
                            ></div>
                        </div>
                        <span className="progress-text">{Math.round(progress)}%</span>
                    </div>
                </div>
            )}

            {!isGenerating && !isComplete && cache && !commentsRetrievalFailed && (
                <div className="generate-content-resume tool-bar">
                    <button onClick={() => handleGenerate({ url, includeVideo, includeTitle, includeDescription, includeTranscriptResearch, includeCommentsResearch })}>
                        Continue Generating Content
                    </button>
                </div>
            )}

            {commentsRetrievalFailed && (
                <div className="generate-content-error">
                    <p>{statusMessage}</p>
                    <button onClick={() => handleGenerate({ url, includeVideo, includeTitle, includeDescription, includeTranscriptResearch, includeCommentsResearch })}>
                        Retry
                    </button>
                </div>
            )}

            {isComplete && url && (
                <div className="generate-content-complete">
                    <p>
                        The content above was generated via <a href={url} target="_blank" rel="noopener noreferrer">{url}</a>
                    </p>
                </div>
            )}
        </div>
    );
}
