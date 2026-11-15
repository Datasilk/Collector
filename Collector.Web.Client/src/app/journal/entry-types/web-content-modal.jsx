import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import * as signalR from '@microsoft/signalr';
import Modal from '@/components/ui/modal';
import Input from '@/components/forms/input';
import Icon from '@/components/ui/icon';
import SelectJournal from '@/components/forms/select-journal';
import NewEntryTag from '../components/new-entry-tag';
import TagsList from '../components/tags-list';
import { apiBasePath } from '@/helpers/endpoints.js';
import { useSession } from '@/context/session';
import { useWorkerHub } from '@/context/workerhub';
import '../entry.css';

/**
 * <summary>Web Content Modal</summary>
 * <description>Modal for creating a new entry from web content by providing a URL to scrape.</description>
 */
export default function WebContentModal({ onClose, journalId = null, entryId = null, defaultTagIds = null }) {
    const navigate = useNavigate();
    const session = useSession();
    const { call: callWorker } = useWorkerHub();

    const [url, setUrl] = useState('');
    const [selectedJournalId, setSelectedJournalId] = useState('');
    const [selectedTags, setSelectedTags] = useState([]);
    const [selectionLoading, setSelectionLoading] = useState(false);
    const [isScraping, setIsScraping] = useState(false);
    const [scrapeStatusMessage, setScrapeStatusMessage] = useState('');
    const [error, setError] = useState(null);

    const isYouTubeChannelUrl = (value) => {
        return /^https?:\/\/(www\.|m\.)?youtube\.com\/(@|channel\/|c\/|user\/)[\w.-]+/i.test((value || '').trim());
    };

    const getSelectedTagIds = () => {
        const tagIds = [...(defaultTagIds || []), ...selectedTags.map(t => t.tagId)];
        return [...new Set(tagIds)];
    };

    const handleAddTag = (tag) => {
        if (!tag || tag.id == null) return;
        setSelectedTags(prev => {
            if (prev.some(t => t.tagId === tag.id)) return prev;
            return [...prev, { tagId: tag.id, name: tag.tag }];
        });
    };

    const handleRemoveTag = (tag) => {
        setSelectedTags(prev => prev.filter(t => t.tagId !== tag.tagId));
    };

    const handleScrape = async () => {
        if (!url.trim() || !selectedJournalId || isScraping || selectionLoading) return;

        const urlToScrape = url.trim();
        const journalId = parseInt(selectedJournalId);

        // YouTube channel URLs are handled by the YoutubeChannelWorker, which
        // imports every video in the channel in the background. Progress is
        // shown in the notifications dropdown instead of redirecting.
        if (isYouTubeChannelUrl(urlToScrape)) {
            try {
                await callWorker('youtube-channel-worker', 'ImportChannel', {
                    channelUrl: urlToScrape,
                    journalId,
                    parentEntryId: entryId || null,
                    tagIds: getSelectedTagIds()
                }, null, `channel:${urlToScrape}`, urlToScrape);
                onClose();
            } catch (err) {
                console.error('Error starting YouTube channel worker:', err);
                setError('Failed to start the YouTube channel import. Please try again.');
            }
            return;
        }

        const webContentHub = new signalR.HubConnectionBuilder()
            .withUrl(apiBasePath() + '/web-content', {
                withCredentials: true,
                skipNegotiation: true,
                transport: signalR.HttpTransportType.WebSockets
            })
            .withAutomaticReconnect([0, 1000, 5000, 10000])
            .configureLogging(signalR.LogLevel.Information)
            .build();

        try {
            setIsScraping(true);
            setScrapeStatusMessage(`Scraping URL ${urlToScrape}`);

            webContentHub.on('ScrapeStatus', (status) => {
                setScrapeStatusMessage(status);
            });

            await webContentHub.start();
            const appUserId = session?.user?.appUserId;
            const result = await webContentHub.invoke('ScrapeUrl', urlToScrape, journalId, entryId, appUserId, getSelectedTagIds());

            if (result && result.success && result.entryId) {
                const navOptions = { replace: true };
                const state = {};

                if (entryId) {
                    state.parentEntryId = entryId;
                }

                if (Object.keys(state).length > 0) {
                    navOptions.state = state;
                }

                navigate(`/journal/${journalId}/entry/${result.entryId}?edit`, navOptions);
                onClose();
            }
        } catch (err) {
            console.error('Error connecting to WebContentHub or scraping URL:', err);
        } finally {
            try {
                await webContentHub.stop();
            } catch (err) {
                console.error('Error stopping WebContentHub connection:', err);
            }
            setIsScraping(false);
        }
    };

    const handleKeyDown = (e) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            handleScrape();
        }
    };

    if (error) {
        return (
            <Modal title="Error" onClose={onClose} width="500px" className="web-content-modal error-modal">
                <div className="modal-content">
                    <p>{error}</p>
                    {error.includes("cookie") && (
                        <p>Please ensure the Chrome extension for Collector is installed and try again.</p>
                    )}
                    <div className="buttons">
                        <button className="btn cancel" onClick={onClose}>Close</button>
                    </div>
                </div>
            </Modal>
        );
    }

    return (
        <Modal title="Scrape Web Content" onClose={onClose}>
            <div className="modal-form">
                {!isScraping && (
                    <>
                        <div className="row">
                            <SelectJournal
                                journalId={journalId}
                                onChange={({ journalId: selectedId, loading }) => {
                                    setSelectedJournalId(selectedId || '');
                                    setSelectionLoading(loading);
                                }}
                            />
                        </div>
                        <div className="row">
                            <Input
                                label="URL"
                                name="url"
                                value={url}
                                onInput={(e) => setUrl(e.target.value)}
                                onKeyDown={handleKeyDown}
                                placeholder="https://..."
                                autoFocus
                            />
                        </div>
                        {selectedTags.length > 0 && (
                            <div className="row web-content-tags-row">
                                <TagsList
                                    tags={selectedTags}
                                    onRemoveTag={handleRemoveTag}
                                />
                            </div>
                        )}
                        <div className="row">
                            <div className="buttons web-content-buttons">
                                {selectedJournalId && (
                                    <NewEntryTag
                                        journalId={selectedJournalId}
                                        onAddTag={handleAddTag}
                                    />
                                )}
                                <button className="cancel" onClick={onClose}>Cancel</button>
                                <button
                                    onClick={handleScrape}
                                    disabled={!url.trim() || !selectedJournalId || selectionLoading || isScraping}
                                >
                                    Scrape
                                </button>
                            </div>
                        </div>
                    </>
                )}

                {isScraping && (
                    <div className="scrape-status-panel">
                        <div className="loading-spinner">
                            <Icon name="progress_activity" spin={true} />
                        </div>
                        <p>{scrapeStatusMessage || 'Starting web content scrape...'}</p>
                    </div>
                )}
            </div>
            <style>{`
                .web-content-tags-row {
                    display: flex;
                    align-items: flex-start;
                    flex-wrap: wrap;
                    gap: 0.5em;
                }

                .web-content-buttons {
                    display: flex;
                    align-items: center;
                }

                .web-content-buttons .entry-tags-region {
                    margin-right: auto;
                    margin-left: 0;
                }
            `}</style>
        </Modal>
    );
}
