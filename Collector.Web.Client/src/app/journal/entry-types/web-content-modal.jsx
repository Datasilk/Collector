import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import * as signalR from '@microsoft/signalr';
import Modal from '@/components/ui/modal';
import Input from '@/components/forms/input';
import Icon from '@/components/ui/icon';
import SelectJournal from '@/components/forms/select-journal';
import { apiBasePath } from '@/helpers/endpoints.js';
import { useSession } from '@/context/session';
import { JournalTags } from '@/api/user/journal-tags';

/**
 * <summary>Web Content Modal</summary>
 * <description>Modal for creating a new entry from web content by providing a URL to scrape.</description>
 */
export default function WebContentModal({ onClose, journalId = null, entryId = null, defaultTagIds = null }) {
    const navigate = useNavigate();
    const session = useSession();
    const { addTagToEntry } = JournalTags(session);

    const [url, setUrl] = useState('');
    const [selectedJournalId, setSelectedJournalId] = useState('');
    const [selectionLoading, setSelectionLoading] = useState(false);
    const [isScraping, setIsScraping] = useState(false);
    const [scrapeStatusMessage, setScrapeStatusMessage] = useState('');

    const handleScrape = async () => {
        if (!url.trim() || !selectedJournalId || isScraping || selectionLoading) return;

        const urlToScrape = url.trim();
        const journalId = parseInt(selectedJournalId);

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
            const result = await webContentHub.invoke('ScrapeUrl', urlToScrape, journalId, entryId);

            if (result && result.success && result.entryId) {
                if (Array.isArray(defaultTagIds) && defaultTagIds.length > 0) {
                    try {
                        await Promise.all(
                            defaultTagIds.map(tagId => addTagToEntry(result.entryId, tagId))
                        );
                    } catch (err) {
                        console.error('Error applying default tags to scraped entry:', err);
                    }
                }

                navigate(`/journal/${journalId}/entry/${result.entryId}?edit`, { replace: true });
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

    return (
        <Modal title="Scrape Web Content" onClose={onClose}>
            <div className="modal-form">
                {!isScraping && (
                    <>
                        <SelectJournal
                            journalId={journalId}
                            onChange={({ categoryId, journalId: selectedId, loading }) => {
                                setSelectedJournalId(selectedId || '');
                                setSelectionLoading(loading);
                            }}
                        />
                        <Input
                            label="URL"
                            name="url"
                            value={url}
                            onInput={(e) => setUrl(e.target.value)}
                            onKeyDown={handleKeyDown}
                            placeholder="https://..."
                            autoFocus
                        />

                        <div className="buttons">
                            <button className="cancel" onClick={onClose}>Cancel</button>
                            <button
                                onClick={handleScrape}
                                disabled={!url.trim() || !selectedJournalId || selectionLoading || isScraping}
                            >
                                Scrape
                            </button>
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
        </Modal>
    );
}
