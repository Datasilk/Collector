import React, { useState, useEffect, useRef, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
//styles
import '@/styles/admin/filter.css';
import './page.css';
//components
import Container from '@/components/admin/container';
import Icon from '@/components/ui/icon';
import Input from '@/components/forms/input';
import Select from '@/components/forms/select';
import Tabs from '@/components/ui/tabs';
import Cli from '@/components/ui/cli';
import ServicesModal from './components/services-modal';
//components
import AddDomain from './components/add';
import DomainModal from './components/domain-modal';
//context
import { useSession } from '@/context/session';
import { useWorkerHub } from '@/context/workerhub';
//api
import { Domains } from '@/api/user/domains';
import { Languages } from '@/api/user/languages';

/**
 * <summary>Admin Domains List Page</summary>
 * <description>Displays and manages the list of domains in the admin panel.</description>
 */
export default function AdminDomains() {
    const navigate = useNavigate();
    const session = useSession();
    const { getDomains, getDomainTypes } = Domains(session);
    const { getAll } = Languages(session);
    const { call, stop, getWorkers, subscribe, requestProgress } = useWorkerHub();

    const [domains, setDomains] = useState([]);
    const [showAdd, setShowAdd] = useState(false);
    const [showServicesModal, setShowServicesModal] = useState(false);
    const [selectedDomainId, setSelectedDomainId] = useState(null);
    const [debounceTimer, setDebounceTimer] = useState(null);
    const [domainTypes, setDomainTypes] = useState([]);
    const [languages, setLanguages] = useState([]);
    const [selectedServices, setSelectedServices] = useState([]);
    const [loading, setLoading] = useState(false);
    const loadingMoreRef = useRef(false);
    const [activeTab, setActiveTab] = useState(0);
    const [analyzerWorker, setAnalyzerWorker] = useState(null);

    // Create a filter object with all filter parameters including pagination
    const [filter, setFilter] = useState({
        search: '',
        sort: 0, // 0 = domain ASC
        subjectIds: [],
        type: 0, // DomainFilterType.All
        domainType: -1,
        domainType2: -1,
        lang: '',
        start: 0,
        length: 250,
        parentId: -1,
        serviceIds: []
    });

    const [totalItems, setTotalItems] = useState(0);
    const [hasMore, setHasMore] = useState(true);
    const [loadingMore, setLoadingMore] = useState(false);
    const [error, setError] = useState(null);
    const sentinelRef = useRef(null);

    const sortOptions = [
        { value: 0, label: 'Domain A-Z' },
        { value: 1, label: 'Domain Z-A' },
        { value: 6, label: 'Title A-Z' },
        { value: 7, label: 'Title Z-A' },
        { value: 2, label: 'Most Articles' },
        { value: 8, label: 'Least Articles' },
        { value: 3, label: 'Newest' },
        { value: 4, label: 'Oldest' },
        { value: 5, label: 'Last Updated' },
        { value: 9, label: 'Least Updated' }
    ];

    const fetchDomains = useCallback((requestFilter, append = false) => {
        return getDomains({
            Search: requestFilter.search || '',
            Sort: requestFilter.sort,
            SubjectIds: requestFilter.subjectIds,
            Type: requestFilter.type,
            DomainType: requestFilter.domainType,
            DomainType2: requestFilter.domainType2,
            Lang: requestFilter.lang,
            Start: requestFilter.start,
            Length: requestFilter.length,
            ParentId: requestFilter.parentId,
            ServiceIds: requestFilter.serviceIds
        }).then(response => {
            if (response.data.success) {
                const newDomains = response.data.data.domains || [];
                const count = response.data.data.totalCount || 0;
                setDomains(prev => append ? [...prev, ...newDomains] : newDomains);
                setTotalItems(count);
                setHasMore(requestFilter.start + newDomains.length < count);
                setError(null);
            } else {
                setError(response.data.message || 'Failed to load domains');
                if (!append) {
                    setDomains([]);
                }
            }
        }).catch(error => {
            console.error('Error fetching domains:', error);
            setError(error.message || 'An unexpected error occurred while loading domains');
            if (!append) {
                setDomains([]);
            }
        }).finally(() => {
            setLoading(false);
            setLoadingMore(false);
        });
    }, [getDomains]);

    const filterDomains = useCallback((customFilter) => {
        const requestFilter = customFilter || filter;
        setLoading(true);
        setError(null);
        fetchDomains(requestFilter, false);
    }, [filter, fetchDomains]);

    const loadMore = useCallback(() => {
        if (loadingMoreRef.current || !hasMore) return;
        loadingMoreRef.current = true;
        setLoadingMore(true);
        const newFilter = { ...filter, start: filter.start + filter.length };
        setFilter(newFilter);
        fetchDomains(newFilter, true).finally(() => {
            loadingMoreRef.current = false;
        });
    }, [filter, hasMore, fetchDomains]);

    useEffect(() => {
        if (!sentinelRef.current || error) return;

        const observer = new IntersectionObserver((entries) => {
            if (entries[0].isIntersecting && hasMore && !loadingMoreRef.current) {
                loadMore();
            }
        }, { rootMargin: '200px' });

        observer.observe(sentinelRef.current);
        return () => observer.disconnect();
    }, [sentinelRef.current, hasMore, loadMore, error]);

    useEffect(() => {
        if (domains.length === 0) {
            filterDomains();
        }
        getDomainTypes().then(response => {
            if (response.data.success) {
                setDomainTypes(response.data.data);
            }
        });
        getAll().then(response => {
            if (response.data.success) {
                // Convert dictionary to array of objects
                const languageData = response.data.data;
                const languageOptions = Object.keys(languageData).map(code => ({
                    value: code,
                    label: languageData[code]
                }));
                setLanguages(languageOptions);
            }
        }).catch(error => {
            console.error('Error fetching languages:', error);
        });
    }, []);

    useEffect(() => {
        let cancelled = false;
        const restoreWorkers = async () => {
            try {
                const hubWorkers = await getWorkers();
                const domainWorker = hubWorkers.find(w => w.route === 'domain-worker');
                if (domainWorker && !cancelled) {
                    setAnalyzerWorker({
                        workerId: domainWorker.workerId,
                        running: true,
                        status: 'Running',
                        cliLines: [{ type: 'info', text: 'Restored analyzer connection' }],
                        cliExpanded: true,
                        processed: 0
                    });
                    await subscribe(domainWorker.workerId, makeAnalyzerHandler());
                    await requestProgress(domainWorker.workerId);
                }
            } catch (err) {
                console.error('Failed to restore domain analyzer worker:', err);
            }
        };
        restoreWorkers();
        return () => { cancelled = true; };
    }, []);

    const handleSortChange = (value) => {
        const newFilter = { ...filter, sort: parseInt(value), start: 0 };
        setFilter(newFilter);
        filterDomains(newFilter);
    };

    const handleDomainNameInput = (e) => {
        const inputValue = e.target.value;

        setFilter(prev => ({
            ...prev,
            search: inputValue
        }));

        // Clear previous timer if exists
        if (debounceTimer) {
            clearTimeout(debounceTimer);
        }

        // Create a new filter object with the updated search value
        const newFilter = {
            ...filter,
            search: inputValue,
            start: 0
        };

        // Set new timer and pass the new filter object to the function
        const timer = setTimeout(() => {
            filterDomains(newFilter);
        }, 500);

        setDebounceTimer(timer);
    };

    const handleFilterChange = (field, value) => {
        setFilter(prev => ({
            ...prev,
            [field]: value,
            start: 0
        }));

        // Create a new filter object with the updated field value
        const newFilter = {
            ...filter,
            [field]: value,
            start: 0
        };

        // Only apply debouncing to text fields (search and lang)
        if (field === 'search' || field === 'lang') {
            // Clear any existing timer
            if (debounceTimer) {
                clearTimeout(debounceTimer);
            }

            // Set a new timer and pass the new filter object to the function
            const timer = setTimeout(() => {
                filterDomains(newFilter);
            }, 500);

            setDebounceTimer(timer);
        } else {
            // For non-text fields, fetch immediately
            filterDomains(newFilter);
        }
    };

    const handleClosedAddDomain = (domain) => {
        if (domain) {
            filterDomains();
        }
        setShowAdd(false);
    };

    const handleOpenDomainModal = (domainId) => {
        setSelectedDomainId(domainId);
    };

    const handleCloseDomainModal = () => {
        setSelectedDomainId(null);
    };

    const handleDomainUpdated = () => {
        filterDomains();
    };

    const makeAnalyzerHandler = () => {
        let lastAnalyzingDomain = '';
        return (msg) => {
            const { eventName, payload } = msg;
            setAnalyzerWorker(prev => {
                if (!prev) return prev;
                let updates = {};
                let line = null;

                if (eventName === 'DomainAnalysisStarted') {
                    updates = { running: true, status: 'Running' };
                    line = { type: 'success', text: 'Analyzer started' };
                } else if (eventName === 'DomainAnalysisProgress') {
                    updates = { processed: payload?.processed || 0 };
                    // intentionally no CLI line here; use the per-domain "Analyzing..." line instead
                } else if (eventName === 'DomainAnalysisComplete') {
                    if (payload?.mode === 'queue') {
                        updates = { status: 'Completed', running: false };
                        line = { type: 'success', text: `Analyzer completed. ${payload?.processed || 0} domains analyzed.` };
                    } else {
                        line = { type: 'success', text: `Single domain analysis complete: type=${payload?.type}, type2=${payload?.type2}, lang=${payload?.language}` };
                    }
                } else if (eventName === 'DomainAnalysisError') {
                    updates = { status: 'Error', running: false };
                    line = { type: 'error', text: `ERROR: ${payload?.message}` };
                } else if (eventName === 'DomainAnalysisUpdate' && payload?.message) {
                    const match = payload.message.match(/^Analyzing\s+(.+?)\.\.\.$/);
                    if (match) {
                        const domain = match[1];
                        if (domain !== lastAnalyzingDomain) {
                            lastAnalyzingDomain = domain;
                            line = { type: 'info', text: 'Analyzing...' };
                        }
                    } else {
                        line = payload.type ? { type: payload.type, text: payload.message } : { type: 'info', text: payload.message };
                    }
                }

                const nextLines = line ? [...prev.cliLines, line] : prev.cliLines;
                if (nextLines.length > 5000) nextLines.shift();
                updates.cliLines = nextLines;

                return { ...prev, ...updates };
            });

            if (eventName === 'DomainAnalysisComplete' && payload?.mode === 'queue') {
                filterDomains();
            }
        };
    };

    const handleStartAnalyzer = async () => {
        if (analyzerWorker?.running) return;

        const worker = {
            workerId: null,
            running: true,
            status: 'Running',
            cliLines: [{ type: 'success', text: 'Domain analyzer started' }],
            cliExpanded: true,
            processed: 0
        };
        setAnalyzerWorker(worker);

        try {
            const id = await call('domain-worker', 'Start', {}, makeAnalyzerHandler(), 'domain-analyzer');
            setAnalyzerWorker(prev => prev ? { ...prev, workerId: id } : null);
        } catch (err) {
            console.error('Failed to start domain analyzer:', err);
            setAnalyzerWorker(prev => prev ? { ...prev, running: false, status: 'Failed to start' } : null);
        }
    };

    const handleStopAnalyzer = async () => {
        if (!analyzerWorker?.workerId) return;
        try {
            await stop(analyzerWorker.workerId);
            setAnalyzerWorker(prev => prev ? { ...prev, running: false, status: 'Stopped' } : null);
        } catch (err) {
            console.error('Failed to stop domain analyzer:', err);
        }
    };

    const handleClearAnalyzerCli = () => {
        setAnalyzerWorker(prev => prev ? { ...prev, cliLines: [] } : null);
    };

    const handleRemoveAnalyzer = () => {
        setAnalyzerWorker(null);
    };

    const handleToggleAnalyzerCli = (expanded) => {
        setAnalyzerWorker(prev => prev ? { ...prev, cliExpanded: expanded } : null);
    };

    const handleShowServicesModal = () => {
        setShowServicesModal(true);
    };

    const handleServicesModalClose = () => {
        // Update the filter with the selected services and trigger a refresh
        const serviceIds = selectedServices.map(s => s.id);
        const newFilter = { ...filter, serviceIds, start: 0 };
        setFilter(newFilter);
        filterDomains(newFilter);
        setShowServicesModal(false);
    };

    const handleServicesModalSave = (services) => {
        setSelectedServices(services);
        const serviceIds = services.map(s => s.id);
        const newFilter = { ...filter, serviceIds, start: 0 };
        setFilter(newFilter);
        setDomains([]);
        filterDomains(newFilter);
        setShowServicesModal(false);
    };

    const handleRemoveService = (serviceId) => {
        const updatedServices = selectedServices.filter(s => s.id !== serviceId);
        setSelectedServices(updatedServices);
        const serviceIds = updatedServices.map(s => s.id);
        const newFilter = { ...filter, serviceIds, start: 0 };
        setFilter(newFilter);
        setDomains([]);
        setLoading(true);
        filterDomains(newFilter);
    };

    const getDomainUrl = (domain) => {
        return `http${domain.https ? 's' : ''}://${domain.www ? 'www.' : ''}${domain.domain}`;
    };

    const getDomainTypeLabel = (value) => {
        if (value <= 0) return '';
        const type = domainTypes.find(t => t.value === value);
        return type ? type.label : '';
    };

    const getDomainBadges = (domain) => {
        const badges = [];
        if (domain.whitelisted) badges.push({ label: 'Whitelisted', className: 'whitelisted' });
        if (domain.blacklisted) badges.push({ label: 'Blacklisted', className: 'blacklisted' });
        if (domain.paywall) badges.push({ label: 'Paywall', className: 'paywall' });
        if (domain.free) badges.push({ label: 'Free', className: 'free' });
        if (domain.empty) badges.push({ label: 'Empty', className: 'empty' });
        if (domain.lang) badges.push({ label: domain.lang.toUpperCase(), className: 'lang' });
        return badges;
    };

    const handleToolbarAnalyzeDomains = () => {
        setActiveTab(1);
        if (!analyzerWorker?.running) {
            handleStartAnalyzer();
        }
    };

    const tools = (<>
        <button onClick={() => setShowAdd(true)}><Icon name="add"></Icon>New Domain</button>
        <button onClick={handleToolbarAnalyzeDomains}><Icon name="batch_prediction"></Icon>Analyze Domains</button>
    </>);

    return (
        <div className="admin-domains">
            {showAdd && <AddDomain onClose={handleClosedAddDomain}></AddDomain>}
            {selectedDomainId && (
                <DomainModal
                    domainId={selectedDomainId}
                    onClose={handleCloseDomainModal}
                    onUpdated={handleDomainUpdated}
                />
            )}
            {showServicesModal && (
                <ServicesModal 
                    onClose={handleServicesModalClose} 
                    onSave={handleServicesModalSave} 
                    session={session} 
                    selectedServices={selectedServices}
                />
            )}
            <Container
                title="Domain Management"
                tools={tools}
            >
                <Tabs tabs={['Domains', 'Analyzer']} selectedIndex={activeTab} onChange={setActiveTab}>
                    <div className="domains-tab">
                        <div className="filters tool-bar">
                            <Input
                                name="domainsearch"
                                type="text"
                                placeholder="Search by Domain Name"
                                value={filter.search}
                                onInput={handleDomainNameInput}
                                className="domainNameInput"
                            />
                            <Select
                                name="domainType"
                                value={filter.domainType}
                                onChange={(e) => handleFilterChange('domainType', parseInt(e.target.value))}
                                options={domainTypes
                                    .sort((a, b) => {
                                        if (a.value === -1) return -1;
                                        if (b.value === -1) return 1;
                                        return a.label.localeCompare(b.label);
                                    })}
                            />
                            <Select
                                name="domainType2"
                                value={filter.domainType2}
                                onChange={(e) => handleFilterChange('domainType2', parseInt(e.target.value))}
                                options={domainTypes
                                    .sort((a, b) => {
                                        if (a.value === -1) return -1;
                                        if (b.value === -1) return 1;
                                        return a.label.localeCompare(b.label);
                                    })}
                            />
                            <Select
                                name="lang"
                                value={filter.lang}
                                onChange={(e) => handleFilterChange('lang', e.target.value)}
                                options={[
                                    { value: '', label: 'All Languages' },
                                    ...languages
                                ]}
                            />
                            <Select
                                name="type"
                                value={filter.type}
                                onChange={(e) => handleFilterChange('type', parseInt(e.target.value))}
                                options={[
                                    { value: 0, label: 'All' },
                                    { value: 1, label: 'Active' },
                                    { value: 2, label: 'Inactive' }
                                ]}
                            />
                            <Select
                                name="sort"
                                value={filter.sort}
                                onChange={(e) => handleSortChange(e.target.value)}
                                options={sortOptions}
                            />
                            <button onClick={() => handleShowServicesModal()}>Services</button>
                        </div>
                        
                        {selectedServices.length > 0 && (
                            <div className="filters tool-bar service-filter-tags">
                                {selectedServices.map(service => (
                                    <div key={service.id} className="service-tag">
                                        {service.name}
                                        <span className="remove" onClick={() => handleRemoveService(service.id)}>
                                            <Icon name="close" />
                                        </span>
                                    </div>
                                ))}
                            </div>
                        )}
                        {loading && domains.length === 0 && !error && (
                            <div className="empty loading">
                                <Icon name="progress_activity" spin={true} /> Loading...
                            </div>
                        )}
                        {error && (
                            <div className="error-message">
                                <Icon name="error" />
                                <p>{error}</p>
                            </div>
                        )}
                        {!error && (
                            <>
                                <div className="domains-grid">
                                    {domains.map(domain => {
                                        const url = getDomainUrl(domain);
                                        const badges = getDomainBadges(domain);
                                        const type1 = getDomainTypeLabel(domain.type);
                                        const type2 = getDomainTypeLabel(domain.type2);
                                        return (
                                            <div
                                                key={domain.domainId}
                                                className="domain-card"
                                                onClick={() => handleOpenDomainModal(domain.domainId)}
                                            >
                                                <div className="domain-card-header">
                                                    <h4 className="domain-card-title" title={domain.title || domain.domain}>
                                                        {domain.title || domain.domain}
                                                    </h4>
                                                    <a
                                                        href={url}
                                                        target="_blank"
                                                        rel="noopener noreferrer"
                                                        title="Open domain"
                                                        onClick={(e) => e.stopPropagation()}
                                                        className="domain-card-link"
                                                    >
                                                        <Icon name="open_in_new" />
                                                    </a>
                                                </div>
                                                <div className="domain-card-domain">{domain.domain}</div>
                                                <div className="domain-card-badges">
                                                    {badges.map((badge, index) => (
                                                        <span key={index} className={`badge ${badge.className}`}>{badge.label}</span>
                                                    ))}
                                                </div>
                                                <div className="domain-card-types">
                                                    {type1 && <span className="domain-type">{type1}</span>}
                                                    {type2 && <span className="domain-type">{type2}</span>}
                                                </div>
                                                <div className="domain-card-stats">
                                                    {domain.articles > 0 && (
                                                        <span className="stat">
                                                            <span className="num">{domain.articles}</span> articles
                                                        </span>
                                                    )}
                                                    {domain.inqueue > 0 && (
                                                        <span className="stat">
                                                            <span className="num">{domain.inqueue}</span> in queue
                                                        </span>
                                                    )}
                                                </div>
                                            </div>
                                        );
                                    })}
                                </div>
                                {loadingMore && (
                                    <div className="loading-more">
                                        <Icon name="progress_activity" spin={true} /> Loading more...
                                    </div>
                                )}
                                {!hasMore && domains.length > 0 && (
                                    <div className="end-of-results">{totalItems.toLocaleString()} domains loaded</div>
                                )}
                                <div ref={sentinelRef} className="scroll-sentinel" />
                            </>
                        )}
                    </div>
                    <div className="analyzer-tab">
                        <div className="filters tool-bar">
                            <div className="right-side">
                                <button onClick={handleStartAnalyzer} disabled={analyzerWorker?.running}>
                                    <Icon name="play_arrow" />Run Analyzer
                                </button>
                            </div>
                        </div>
                        {analyzerWorker?.running && (
                            <div className="worker-status">
                                <Icon name="progress_activity" spin />
                                Analyzer running
                            </div>
                        )}
                        <div className="worker-clis">
                            {analyzerWorker && (
                                <div className={`worker-cli ${analyzerWorker.running ? 'running' : 'stopped'}`}>
                                    <div className="worker-cli-header">
                                        <span className={`worker-cli-status ${analyzerWorker.status.toLowerCase().replace(/\s+/g, '-')}`}>{analyzerWorker.status}</span>
                                        <span className="worker-cli-counts">
                                            {analyzerWorker.processed > 0 && `Analyzed: ${analyzerWorker.processed}`}
                                        </span>
                                        <div className="worker-cli-actions">
                                            {analyzerWorker.running ? (
                                                <button onClick={handleStopAnalyzer} className="cancel icon" title="Stop">
                                                    <Icon name="stop" />
                                                </button>
                                            ) : (
                                                <button onClick={handleRemoveAnalyzer} className="icon" title="Remove">
                                                    <Icon name="close" />
                                                </button>
                                            )}
                                            <button onClick={handleClearAnalyzerCli} className="icon" title="Clear CLI">
                                                <Icon name="clear_all" />
                                            </button>
                                        </div>
                                    </div>
                                    <Cli
                                        lines={analyzerWorker.cliLines}
                                        title="Domain Analyzer"
                                        expanded={analyzerWorker.cliExpanded}
                                        onToggle={handleToggleAnalyzerCli}
                                        processed={analyzerWorker.processed}
                                    />
                                </div>
                            )}
                        </div>
                    </div>
                </Tabs>
            </Container>
        </div>
    );
}
