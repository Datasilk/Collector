import React, { useState, useEffect } from 'react';
import Modal from '@/components/ui/modal';
import Icon from '@/components/ui/icon';
import Input from '@/components/forms/input';
import Select from '@/components/forms/select';
import Checkbox from '@/components/forms/checkbox';
import TextArea from '@/components/forms/textarea';
import Tabs from '@/components/ui/tabs';
import Message from '@/components/ui/message';
import { useSession } from '@/context/session';
import { useWorkerHub } from '@/context/workerhub';
import { Domains } from '@/api/user/domains';
import { printDate, printDateTime } from '@/helpers/datetime';
import './domain-modal.css';

const tabs = [
    { id: 'info', label: 'Info' },
    { id: 'links', label: 'Links' },
    { id: 'rules', label: 'Analyzer Rules' },
    { id: 'download', label: 'Download Rules' },
    { id: 'advanced', label: 'Advanced' }
];
const tabLabels = tabs.map(t => t.label);
const tabIds = tabs.map(t => t.id);

const normalizeDomain = (d) => d ? {
    ...d,
    type: d.type ?? 0,
    type2: d.type2 ?? 0,
    lang: d.lang ?? '',
    https: d.https ?? false,
    www: d.www ?? false,
    requireSubscription: d.paywall ?? false,
    hasFreeContent: d.free ?? false,
    isEmpty: d.empty ?? false
} : null;

export default function DomainModal({ domainId, onClose, onUpdated }) {
    const session = useSession();
    const { call } = useWorkerHub();
    const api = Domains(session);
    const [activeTab, setActiveTab] = useState('info');
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [message, setMessage] = useState('');
    const [analyzing, setAnalyzing] = useState(false);
    const [analyzeMessage, setAnalyzeMessage] = useState('');
    const [domain, setDomain] = useState(null);
    const [originalDomain, setOriginalDomain] = useState(null);
    const [domainTypes, setDomainTypes] = useState([]);
    const [links, setLinks] = useState([]);
    const [analyzerRules, setAnalyzerRules] = useState([]);
    const [downloadRules, setDownloadRules] = useState([]);
    const [newRule, setNewRule] = useState({ selector: '', rule: false });
    const [newDownloadRule, setNewDownloadRule] = useState({ rule: true, url: '', title: '', summary: '' });

    useEffect(() => {
        loadDomain();
        api.getDomainTypes().then(res => {
            if (res.data?.success) {
                setDomainTypes(res.data.data);
            }
        });
    }, [domainId]);

    const loadDomain = async () => {
        setLoading(true);
        try {
            const res = await api.getDomain(domainId);
            if (res.data?.success) {
                const normalized = normalizeDomain(res.data.data);
                setDomain(normalized);
                setOriginalDomain(normalized);
            } else {
                setMessage(res.data?.message || 'Failed to load domain');
            }
        } catch (err) {
            setMessage('Error loading domain');
        } finally {
            setLoading(false);
        }
    };

    const loadLinks = async () => {
        try {
            const res = await api.getLinks(domainId);
            if (res.data?.success) {
                setLinks(res.data.data || []);
            }
        } catch (err) {
            console.error('Error loading links', err);
        }
    };

    const loadAnalyzerRules = async () => {
        try {
            const res = await api.getAnalyzerRules(domainId);
            if (res.data?.success) {
                setAnalyzerRules(res.data.data || []);
            }
        } catch (err) {
            console.error('Error loading analyzer rules', err);
        }
    };

    const loadDownloadRules = async () => {
        try {
            const res = await api.getDownloadRules(domainId);
            if (res.data?.success) {
                setDownloadRules(res.data.data || []);
            }
        } catch (err) {
            console.error('Error loading download rules', err);
        }
    };

    const handleTabChange = (index) => {
        const tab = tabIds[index];
        setActiveTab(tab);
        setMessage('');
        if (tab === 'links' && links.length === 0) loadLinks();
        if (tab === 'rules' && analyzerRules.length === 0) loadAnalyzerRules();
        if (tab === 'download' && downloadRules.length === 0) loadDownloadRules();
    };

    const handleFieldChange = (field, value) => {
        setDomain(prev => ({ ...prev, [field]: value }));
    };

    const handleClearMessage = () => setMessage('');

    const handleSaveInfo = async () => {
        if (!domain || !originalDomain) return;
        setSaving(true);
        setMessage('');
        try {
            const promises = [];
            if (domain.title !== originalDomain.title || domain.description !== originalDomain.description || domain.lang !== originalDomain.lang) {
                promises.push(api.updateDomainInfo({
                    DomainId: domainId,
                    Title: domain.title || '',
                    Description: domain.description || '',
                    Lang: domain.lang || ''
                }));
            }
            if (domain.type !== originalDomain.type) {
                promises.push(api.updateDomainType({ DomainId: domainId, Type: domain.type || 0 }));
            }
            if (domain.type2 !== originalDomain.type2) {
                promises.push(api.updateDomainType2({ DomainId: domainId, Type: domain.type2 || 0 }));
            }
            if (domain.lang !== originalDomain.lang && domain.title === originalDomain.title && domain.description === originalDomain.description) {
                promises.push(api.updateLanguage({ DomainId: domainId, Lang: domain.lang || '' }));
            }
            if (domain.https !== originalDomain.https || domain.www !== originalDomain.www) {
                promises.push(api.updateHttpsWww({
                    DomainId: domainId,
                    Https: domain.https || false,
                    Www: domain.www || false
                }));
            }
            if (domain.requireSubscription !== originalDomain.requireSubscription) {
                promises.push(api.requireSubscription({ DomainId: domainId, Status: domain.requireSubscription || false }));
            }
            if (domain.hasFreeContent !== originalDomain.hasFreeContent) {
                promises.push(api.hasFreeContent({ DomainId: domainId, Status: domain.hasFreeContent || false }));
            }
            if (domain.isEmpty !== originalDomain.isEmpty) {
                promises.push(api.isEmpty({ DomainId: domainId, Status: domain.isEmpty || false }));
            }
            await Promise.all(promises);
            setOriginalDomain(domain);
            setMessage('Domain updated successfully');
            if (onUpdated) onUpdated();
        } catch (err) {
            setMessage('Failed to update domain');
        } finally {
            setSaving(false);
        }
    };

    const handleAnalyzeDomain = async () => {
        setAnalyzing(true);
        setAnalyzeMessage('Domain analysis started');
        try {
            await call('domain-worker', 'AnalyzeDomain', { domainId }, (msg) => {
                if (msg.eventName === 'DomainAnalysisUpdate' && msg.payload?.message) {
                    setAnalyzeMessage(msg.payload.message);
                } else if (msg.eventName === 'DomainAnalysisComplete') {
                    setAnalyzing(false);
                    setAnalyzeMessage('');
                    loadDomain();
                    if (onUpdated) onUpdated();
                } else if (msg.eventName === 'DomainAnalysisError') {
                    setAnalyzing(false);
                    setAnalyzeMessage(msg.payload?.message || 'Domain analysis failed');
                }
            });
        } catch (err) {
            console.error('Failed to start domain analysis:', err);
            setAnalyzing(false);
            setAnalyzeMessage('Failed to start domain analysis');
        }
    };

    const handleAddAnalyzerRule = async () => {
        if (!newRule.selector) return;
        try {
            await api.addAnalyzerRule({
                DomainId: domainId,
                Selector: newRule.selector,
                Rule: newRule.rule
            });
            setNewRule({ selector: '', rule: false });
            loadAnalyzerRules();
        } catch (err) {
            setMessage('Failed to add analyzer rule');
        }
    };

    const handleRemoveAnalyzerRule = async (ruleId) => {
        try {
            await api.removeAnalyzerRule(ruleId);
            loadAnalyzerRules();
        } catch (err) {
            setMessage('Failed to remove analyzer rule');
        }
    };

    const handleAddDownloadRule = async () => {
        if (!newDownloadRule.url) return;
        try {
            await api.addDownloadRule({
                DomainId: domainId,
                Rule: newDownloadRule.rule,
                Url: newDownloadRule.url,
                Title: newDownloadRule.title,
                Summary: newDownloadRule.summary
            });
            setNewDownloadRule({ rule: true, url: '', title: '', summary: '' });
            loadDownloadRules();
        } catch (err) {
            setMessage('Failed to add download rule');
        }
    };

    const handleRemoveDownloadRule = async (ruleId) => {
        try {
            await api.removeDownloadRule(ruleId);
            loadDownloadRules();
        } catch (err) {
            setMessage('Failed to remove download rule');
        }
    };

    const handleFindTitle = async () => {
        setSaving(true);
        try {
            const res = await api.findDomainTitle(domainId);
            if (res.data?.success) {
                setDomain(prev => ({ ...prev, title: res.data.data }));
                setMessage('Title found');
                if (onUpdated) onUpdated();
            }
        } catch (err) {
            setMessage('Failed to find title');
        } finally {
            setSaving(false);
        }
    };

    const handleFindDescription = async () => {
        setSaving(true);
        try {
            const res = await api.findDescription(domainId);
            if (res.data?.success) {
                setDomain(prev => ({ ...prev, description: res.data.data }));
                setMessage('Description found');
                if (onUpdated) onUpdated();
            }
        } catch (err) {
            setMessage('Failed to find description');
        } finally {
            setSaving(false);
        }
    };

    const handleDeleteAllArticles = async () => {
        if (!confirm('Do you really want to delete all articles for this domain? This cannot be undone!')) return;
        setSaving(true);
        try {
            await api.deleteAllArticles(domainId);
            setMessage('All articles deleted');
            if (onUpdated) onUpdated();
        } catch (err) {
            setMessage('Failed to delete articles');
        } finally {
            setSaving(false);
        }
    };

    const handleCleanDownloads = async () => {
        if (!confirm('Do you really want to run this cleanup? This will permanently delete all affected articles, downloads, and associated files on disk.')) return;
        setSaving(true);
        try {
            await api.cleanDownloads(domainId);
            setMessage('Downloads cleaned up');
            if (onUpdated) onUpdated();
        } catch (err) {
            setMessage('Failed to clean downloads');
        } finally {
            setSaving(false);
        }
    };

    const handleDeleteDomain = async () => {
        if (!confirm('Do you really want to delete this domain? This cannot be undone!')) return;
        setSaving(true);
        try {
            await api.deleteDomain(domainId);
            setMessage('Domain deleted');
            onClose();
            if (onUpdated) onUpdated();
        } catch (err) {
            setMessage('Failed to delete domain');
            setSaving(false);
        }
    };

    const renderInfoTab = () => (
        <div className="domain-info-tab">
            <div className="form-row two-col">
                <Input
                    label="Domain"
                    value={domain.domain || ''}
                    onInput={(e) => handleFieldChange('domain', e.target.value)}
                    disabled={true}
                />
                <Input
                    label="Title"
                    value={domain.title || ''}
                    onInput={(e) => handleFieldChange('title', e.target.value)}
                    className="wide"
                />
            </div>
            <div className="form-row-block">
                <TextArea
                    label="Description"
                    name="domain-description"
                    defaultValue={domain.description || ''}
                    rows={3}
                    onInput={(e) => handleFieldChange('description', e.target.value)}
                />
            </div>
            <div className="form-row three-col">
                <Select
                    label="Domain Type"
                    value={domain.type || 0}
                    onChange={(e) => handleFieldChange('type', parseInt(e.target.value))}
                    options={domainTypes}
                />
                <Select
                    label="Domain Type 2"
                    value={domain.type2 || 0}
                    onChange={(e) => handleFieldChange('type2', parseInt(e.target.value))}
                    options={domainTypes}
                />
                <Input
                    label="Language"
                    value={domain.lang || ''}
                    onInput={(e) => handleFieldChange('lang', e.target.value)}
                    maxLength={6}
                />
            </div>
            <div className="form-row flags">
                <Checkbox
                    label="Requires Subscription"
                    checked={domain.requireSubscription || false}
                    onChange={(checked) => handleFieldChange('requireSubscription', checked)}
                    name="requireSubscription"
                />
                <Checkbox
                    label="Has Free Content"
                    checked={domain.hasFreeContent || false}
                    onChange={(checked) => handleFieldChange('hasFreeContent', checked)}
                    name="hasFreeContent"
                />
                <Checkbox
                    label="Is Empty"
                    checked={domain.isEmpty || false}
                    onChange={(checked) => handleFieldChange('isEmpty', checked)}
                    name="isEmpty"
                />
                <Checkbox
                    label="HTTPS"
                    checked={domain.https || false}
                    onChange={(checked) => handleFieldChange('https', checked)}
                    name="https"
                />
                <Checkbox
                    label="WWW"
                    checked={domain.www || false}
                    onChange={(checked) => handleFieldChange('www', checked)}
                    name="www"
                />
            </div>
            <div className="form-row meta">
                <span>Articles: {domain.articles || 0}</span>
                <span>Created: {domain.datecreated ? printDate(new Date(domain.datecreated)) : 'Unknown'}</span>
                <span>Updated: {domain.dateupdated ? printDateTime(new Date(domain.dateupdated)) : 'Never'}</span>
            </div>
            <div className="buttons">
                <button onClick={handleAnalyzeDomain} disabled={analyzing}>
                    {analyzing && <Icon name="progress_activity" spin />}
                    {analyzing ? 'Analyzing...' : 'Analyze Domain'}
                </button>
                <button onClick={handleSaveInfo} disabled={saving}>{saving ? 'Saving...' : 'Save Info'}</button>
            </div>
        </div>
    );

    const renderLinksTab = () => (
        <div className="domain-links-tab">
            {links.length === 0 ? (
                <div className="empty">No links found for this domain.</div>
            ) : (
                <table className="spreadsheet">
                    <thead>
                        <tr>
                            <th>Domain</th>
                            <th>Level</th>
                        </tr>
                    </thead>
                    <tbody>
                        {links.map((link, idx) => (
                            <tr key={idx}>
                                <td>{link.domain || link.linkDomain || link}</td>
                                <td>{link.level || 1}</td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            )}
        </div>
    );

    const renderRulesTab = () => (
        <div className="domain-rules-tab">
            <div className="new-rule">
                <Input
                    placeholder="Selector"
                    value={newRule.selector}
                    onInput={(e) => setNewRule({ ...newRule, selector: e.target.value })}
                />
                <Checkbox
                    label="Protect"
                    checked={newRule.rule}
                    onChange={(checked) => setNewRule({ ...newRule, rule: checked })}
                    name="rule"
                />
                <button onClick={handleAddAnalyzerRule}>Add Rule</button>
            </div>
            {analyzerRules.length === 0 ? (
                <div className="empty">No analyzer rules found.</div>
            ) : (
                <table className="spreadsheet">
                    <thead>
                        <tr>
                            <th>Selector</th>
                            <th>Type</th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                        {analyzerRules.map(rule => (
                            <tr key={rule.ruleId || rule.id}>
                                <td>{rule.selector}</td>
                                <td>{rule.rule ? 'Protect' : 'Remove'}</td>
                                <td className="buttons">
                                    <button className="icon" onClick={() => handleRemoveAnalyzerRule(rule.ruleId || rule.id)}>
                                        <Icon name="delete" />
                                    </button>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            )}
        </div>
    );

    const renderDownloadRulesTab = () => (
        <div className="domain-download-rules-tab">
            <div className="new-rule">
                <Input
                    placeholder="URL pattern"
                    value={newDownloadRule.url}
                    onInput={(e) => setNewDownloadRule({ ...newDownloadRule, url: e.target.value })}
                />
                <Input
                    placeholder="Title"
                    value={newDownloadRule.title}
                    onInput={(e) => setNewDownloadRule({ ...newDownloadRule, title: e.target.value })}
                />
                <Input
                    placeholder="Summary"
                    value={newDownloadRule.summary}
                    onInput={(e) => setNewDownloadRule({ ...newDownloadRule, summary: e.target.value })}
                />
                <Checkbox
                    label="Download Only"
                    checked={newDownloadRule.rule}
                    onChange={(checked) => setNewDownloadRule({ ...newDownloadRule, rule: checked })}
                    name="downloadRule"
                />
                <button onClick={handleAddDownloadRule}>Add Rule</button>
            </div>
            {downloadRules.length === 0 ? (
                <div className="empty">No download rules found.</div>
            ) : (
                <table className="spreadsheet">
                    <thead>
                        <tr>
                            <th>URL</th>
                            <th>Title</th>
                            <th>Summary</th>
                            <th>Type</th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                        {downloadRules.map(rule => (
                            <tr key={rule.ruleId || rule.id}>
                                <td>{rule.url}</td>
                                <td>{rule.title}</td>
                                <td>{rule.summary}</td>
                                <td>{rule.rule ? 'Download Only' : 'Skip'}</td>
                                <td className="buttons">
                                    <button className="icon" onClick={() => handleRemoveDownloadRule(rule.ruleId || rule.id)}>
                                        <Icon name="delete" />
                                    </button>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            )}
        </div>
    );

    const renderAdvancedTab = () => (
        <div className="domain-advanced-tab">
            <div className="advanced-section">
                <h4>Domain Content</h4>
                <div className="buttons">
                    <button onClick={handleFindTitle} disabled={saving}>Find Title</button>
                    <button onClick={handleFindDescription} disabled={saving}>Find Description</button>
                </div>
            </div>
            <div className="advanced-section">
                <h4>Cleanup</h4>
                <div className="buttons">
                    <button onClick={handleCleanDownloads} disabled={saving}>Clean Up Downloads</button>
                    <button onClick={handleDeleteAllArticles} disabled={saving}>Delete All Articles</button>
                </div>
            </div>
            <div className="advanced-section danger">
                <h4>Danger Zone</h4>
                <div className="buttons">
                    <button onClick={handleDeleteDomain} disabled={saving}>Delete Domain</button>
                </div>
            </div>
        </div>
    );

    return (
        <Modal
            title={domain ? `Edit ${domain.domain}` : 'Edit Domain'}
            onClose={onClose}
            wide={true}
            className="domain-modal"
        >
            {loading ? (
                <div className="empty loading">
                    <Icon name="progress_activity" spin={true} /> Loading...
                </div>
            ) : !domain ? (
                <div className="empty">Failed to load domain.</div>
            ) : (
                <>
                    {analyzeMessage && (
                        <Message type="info" onClose={() => setAnalyzeMessage('')}>{analyzeMessage}</Message>
                    )}
                    {message && <Message type={/failed|error/i.test(message) ? 'error' : 'info'} onClose={handleClearMessage}>{message}</Message>}
                    <Tabs tabs={tabLabels} selectedIndex={tabIds.indexOf(activeTab)} onChange={handleTabChange}>
                        {renderInfoTab()}
                        {renderLinksTab()}
                        {renderRulesTab()}
                        {renderDownloadRulesTab()}
                        {renderAdvancedTab()}
                    </Tabs>
                </>
            )}
        </Modal>
    );
}
