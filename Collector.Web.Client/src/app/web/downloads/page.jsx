import React, { useEffect, useMemo, useState } from 'react';
//styles
import './page.css';
//components
import Container from '@/components/admin/container';
import Icon from '@/components/ui/icon';
import Cli from '@/components/ui/cli';
import Modal from '@/components/ui/modal';
import Input from '@/components/forms/input';
import Select from '@/components/forms/select';
//components
import AddDownload from './components/add';
//context
import { useWorkerHub } from '@/context/workerhub';
import { useSession } from '@/context/session';
//api
import { Feeds } from '@/api/user/feeds';

const SORT_OPTIONS = [
    { value: 0, label: 'Newest First' },
    { value: 1, label: 'Oldest First' },
    { value: 2, label: 'Home Pages Only' },
    { value: 3, label: 'Random' }
];

function getFilterKey(filters) {
    return `${filters.sort}-${filters.domain || 'all'}-${filters.feedId || 0}`;
}

function getFilterTitle(filters, feeds) {
    const sortLabel = SORT_OPTIONS.find(s => s.value === filters.sort)?.label || `Sort ${filters.sort}`;
    const feed = feeds.find(f => f.feedId === filters.feedId);
    let title = sortLabel;
    if (filters.domain) title += ` - Domain: ${filters.domain}`;
    if (filters.feedId && feed) title += ` - Feed: ${feed.title || feed.feedId}`;
    return title;
}

/**
 * <summary>Admin Downloads Management Page</summary>
 * <description>Console-style interface for running and monitoring multiple filtered download workers.</description>
 */
export default function AdminDownloads() {
    const { call, stop, getWorkers, subscribe, requestProgress } = useWorkerHub();
    const session = useSession();
    const { getList: getFeeds } = Feeds(session);

    const [showAdd, setShowAdd] = useState(false);
    const [feeds, setFeeds] = useState([]);
    const [filters, setFilters] = useState({ sort: 0, domain: '', feedId: 0 });
    const [workers, setWorkers] = useState([]);
    const [showDuplicateModal, setShowDuplicateModal] = useState(false);

    useEffect(() => {
        let cancelled = false;
        getFeeds().then(response => {
            const data = response?.data?.data;
            const success = response?.data?.success;
            if (!cancelled && success && Array.isArray(data)) {
                setFeeds(data);
            }
        }).catch(err => console.error('Failed to load feeds:', err));
        return () => { cancelled = true; };
    }, []);

    useEffect(() => {
        let cancelled = false;
        const restoreWorkers = async () => {
            try {
                const hubWorkers = await getWorkers();
                const downloadWorkers = hubWorkers.filter(w => w.route === 'download-worker');
                for (const hubWorker of downloadWorkers) {
                    let restoredFilters = null;
                    try {
                        restoredFilters = JSON.parse(hubWorker.customId || '{}');
                    } catch {
                        continue;
                    }
                    const restoredKey = getFilterKey(restoredFilters);
                    const restoredWorker = {
                        workerId: hubWorker.workerId,
                        key: restoredKey,
                        filters: restoredFilters,
                        title: getFilterTitle(restoredFilters, feeds),
                        running: true,
                        status: 'Running',
                        cliLines: [{ type: 'info', text: 'Restored worker connection' }],
                        cliExpanded: false,
                        processed: 0,
                        saved: 0,
                        links: 0,
                        failed: 0
                    };
                    setWorkers(prev => {
                        if (prev.some(w => w.key === restoredKey)) return prev;
                        return [...prev, restoredWorker];
                    });
                    await subscribe(hubWorker.workerId, makeWorkerHandler(restoredKey));
                    await requestProgress(hubWorker.workerId);
                }
            } catch (err) {
                console.error('Failed to restore download workers:', err);
            }
        };
        if (feeds.length > 0) {
            restoreWorkers();
        }
        return () => { cancelled = true; };
    }, [feeds]);

    const feedOptions = useMemo(() => [
        { value: 0, label: 'All Feeds' },
        ...feeds.map(f => ({ value: f.feedId, label: f.title || `Feed ${f.feedId}` }))
    ], [feeds]);

    const updateWorkerByKey = (key, updates) => {
        setWorkers(prev => prev.map(w => w.key === key ? { ...w, ...updates } : w));
    };

    const addWorkerLineByKey = (key, line) => {
        setWorkers(prev => prev.map(w => {
            if (w.key !== key) return w;
            const next = [...w.cliLines, line];
            if (next.length > 5000) next.shift();
            return { ...w, cliLines: next };
        }));
    };

    const makeWorkerHandler = (key) => (msg) => {
        const { eventName, payload } = msg;
        let line = null;
        setWorkers(prev => {
            const worker = prev.find(w => w.key === key);
            if (!worker) return prev;

            let updates = {};
            if (eventName === 'DownloadProgress') {
                updates = {
                    processed: payload?.processed || 0,
                    saved: payload?.saved || 0,
                    links: payload?.links || 0
                };
            } else if (eventName === 'DownloadStarted') {
                updates = { status: 'Running', running: true };
                line = { type: 'success', text: `Worker started (feedId=${payload?.feedId}, domain=${payload?.domain || 'all'}, sort=${payload?.sort})` };
            } else if (eventName === 'DownloadComplete') {
                updates = { status: 'Completed', running: false };
                line = { type: 'success', text: `Worker completed. Processed ${payload?.processed}, saved=${payload?.saved}, found ${payload?.links} links.` };
            } else if (eventName === 'DownloadError') {
                updates = { status: 'Error', running: false, failed: (worker.failed || 0) + 1 };
                line = { type: 'error', text: `ERROR: ${payload?.message}` };
            } else if (eventName === 'DownloadUpdate' && payload?.message) {
                line = payload.type ? { type: payload.type, text: payload.message } : payload.message;
                if (payload.html) {
                    console.log(`[HTML feed: ${payload.message}]\n`, payload.html);
                }
            }

            if (line) {
                const nextLines = [...worker.cliLines, line];
                if (nextLines.length > 5000) nextLines.shift();
                updates.cliLines = nextLines;
            }

            if (Object.keys(updates).length === 0) return prev;
            return prev.map(w => w.key === key ? { ...w, ...updates } : w);
        });
    };

    const handleStartDownloads = async () => {
        const key = getFilterKey(filters);
        if (workers.some(w => w.running && w.key === key)) {
            setShowDuplicateModal(true);
            return;
        }

        const worker = {
            workerId: null,
            key,
            filters: { ...filters },
            title: getFilterTitle(filters, feeds),
            running: true,
            status: 'Running',
            cliLines: [{ type: 'success', text: 'Download worker started' }],
            cliExpanded: false,
            processed: 0,
            saved: 0,
            links: 0,
            failed: 0
        };
        setWorkers(prev => [...prev, worker]);

        try {
            const id = await call('download-worker', 'Start', {
                feedId: filters.feedId,
                domain: filters.domain,
                sort: filters.sort
            }, makeWorkerHandler(key), JSON.stringify(filters));
            updateWorkerByKey(key, { workerId: id });
        } catch (err) {
            console.error('Failed to start download worker:', err);
            updateWorkerByKey(key, { running: false, status: 'Failed to start' });
        }
    };

    const handleStopWorker = async (workerId, key) => {
        try {
            if (workerId) {
                await stop(workerId);
            }
            updateWorkerByKey(key, { running: false, status: 'Stopped' });
            addWorkerLineByKey(key, { type: 'warning', text: 'Download worker stopped' });
        } catch (err) {
            console.error('Failed to stop download worker:', err);
            addWorkerLineByKey(key, { type: 'error', text: 'Failed to stop download worker' });
        }
    };

    const handleClearWorkerCli = (key) => {
        updateWorkerByKey(key, { cliLines: [] });
    };

    const handleRemoveWorker = (key) => {
        setWorkers(prev => prev.filter(w => w.key !== key));
    };

    const handleToggleCli = (key, expanded) => {
        updateWorkerByKey(key, { cliExpanded: expanded });
    };

    const handleFilterChange = (e) => {
        const { name, value } = e.target;
        setFilters(prev => ({ ...prev, [name]: name === 'feedId' || name === 'sort' ? parseInt(value, 10) || 0 : value }));
    };

    const handleClosedAddDownload = (download) => {
        if (download && workers.length > 0) {
            workers.forEach(w => addWorkerLineByKey(w.key, { type: 'success', text: `Added to queue: ${download.url}` }));
        }
        setShowAdd(false);
    };

    const runningCount = workers.filter(w => w.running).length;

    const tools = (<>
        <button onClick={() => setShowAdd(true)}><Icon name="add" />New Download</button>
    </>);

    return (
        <div className="admin-downloads">
            {showAdd && <AddDownload onClose={handleClosedAddDownload} />}
            {showDuplicateModal && (
                <Modal title="Filter Already Running" onClose={() => setShowDuplicateModal(false)}>
                    <p>A download worker with the same filter is already running.</p>
                    <div className="buttons">
                        <button onClick={() => setShowDuplicateModal(false)}>OK</button>
                    </div>
                </Modal>
            )}
            <Container title="Download Management" tools={tools}>
                <div className="filters tool-bar">
                    <div className="form-group">
                        <Select
                            label="Sort"
                            name="sort"
                            value={filters.sort}
                            options={SORT_OPTIONS}
                            onChange={handleFilterChange}
                        />
                    </div>
                    <div className="form-group">
                        <Input
                            label="Domain"
                            name="domain"
                            value={filters.domain}
                            onChange={handleFilterChange}
                            placeholder="example.com"
                        />
                    </div>
                    <div className="form-group">
                        <Select
                            label="Feed"
                            name="feedId"
                            value={filters.feedId}
                            options={feedOptions}
                            onChange={handleFilterChange}
                        />
                    </div>
                    <div className="right-side">
                        <button onClick={handleStartDownloads} disabled={runningCount > 0 && workers.some(w => w.running && w.key === getFilterKey(filters))}>
                            <Icon name="play_arrow" />Start Downloads
                        </button>
                    </div>
                </div>
                {runningCount > 0 && (
                    <div className="worker-status">
                        <Icon name="progress_activity" spin />
                        {runningCount} worker{runningCount !== 1 ? 's' : ''} running
                    </div>
                )}
                <div className="worker-clis">
                    {workers.map(worker => (
                        <div key={worker.key} className={`worker-cli ${worker.running ? 'running' : 'stopped'}`}>
                            <div className="worker-cli-header">
                                <span className={`worker-cli-status ${worker.status.toLowerCase().replace(/\s+/g, '-')}`}>{worker.status}</span>
                                <span className="worker-cli-counts">
                                    {worker.processed > 0 && `Processed: ${worker.processed}`}
                                    {worker.processed > 0 && worker.saved > 0 && ', '}
                                    {worker.saved > 0 && `Saved: ${worker.saved}`}
                                    {((worker.processed > 0 || worker.saved > 0) && worker.links > 0) && ', '}
                                    {worker.links > 0 && `Links: ${worker.links}`}
                                </span>
                                <div className="worker-cli-actions">
                                    {worker.running ? (
                                        <button onClick={() => handleStopWorker(worker.workerId, worker.key)} className="cancel icon" title="Stop">
                                            <Icon name="stop" />
                                        </button>
                                    ) : (
                                        <button onClick={() => handleRemoveWorker(worker.key)} className="icon" title="Remove">
                                            <Icon name="close" />
                                        </button>
                                    )}
                                    <button onClick={() => handleClearWorkerCli(worker.key)} className="icon" title="Clear CLI">
                                        <Icon name="clear_all" />
                                    </button>
                                </div>
                            </div>
                            <Cli
                                lines={worker.cliLines}
                                title={worker.title}
                                expanded={worker.cliExpanded}
                                onToggle={(expanded) => handleToggleCli(worker.key, expanded)}
                                processed={worker.processed}
                                failed={worker.failed}
                            />
                        </div>
                    ))}
                </div>
            </Container>
        </div>
    );
}
