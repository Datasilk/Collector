import React, { useState, useEffect } from 'react';
import Modal from '@/components/ui/modal';
import Tabs from '@/components/ui/tabs';
import Icon from '@/components/ui/icon';
import Input from '@/components/forms/input';
import Select from '@/components/forms/select';
import ToggleSwitch from '@/components/ui/toggle-switch';
import JournalChapters from '@/components/journal/journal-chapters';
import { useSession } from '@/context/session';
import { Journals } from '@/api/user/journals';
import { apiBasePath } from '@/helpers/endpoints.js';
import Editor from '@monaco-editor/react';

export default function SettingsModal({
    entry,
    entryJson,
    journalId,
    chapters,
    onClose,
    onSaved,
    onUpdate,
    onChaptersChanged
}) {
    const session = useSession();
    const { setEntryEncrypted, setEntryPublished, updateEntryCreated, setEntryChapter, updateEntryThumbnail, archiveEntry } = Journals(session);

    const [selectedTab, setSelectedTab] = useState(0);
    const [settings, setSettings] = useState({
        encrypted: entry.encrypted,
        published: entry.status === 2,
        created: entry.created ? new Date(entry.created).toISOString().slice(0, 16) : '',
        chapterId: entry.chapterId || null,
        thumbnail: entry.thumbnail || null
    });
    const [settingsChanged, setSettingsChanged] = useState(false);
    const [showChapterManagement, setShowChapterManagement] = useState(false);
    const [saveStatus, setSaveStatus] = useState(null);

    const [journalTitle, setJournalTitle] = useState('');
    const [originalJournalTitle, setOriginalJournalTitle] = useState('');
    const [cssCode, setCssCode] = useState('');
    const [originalCss, setOriginalCss] = useState('');
    const [journalSaving, setJournalSaving] = useState(false);
    const [cssSaving, setCssSaving] = useState(false);
    const [theme] = useState('vs-dark');

    // Get all image modules and video modules with thumbnails from entryJson
    const imageModules = entryJson?.modules?.filter(module => module.type === 'image') || [];
    const videoModules = entryJson?.modules?.filter(module => module.type === 'video-player' && module.thumbnailPath) || [];
    const thumbnailModules = [...imageModules, ...videoModules];

    useEffect(() => {
        if (!journalId) return;

        const loadJournalDetails = async () => {
            try {
                const api = Journals(session);
                const response = await api.getJournal(journalId);
                if (response.data?.success && response.data.data) {
                    const journal = response.data.data;
                    const title = journal.title || '';
                    setJournalTitle(title);
                    setOriginalJournalTitle(title);
                } else {
                    setJournalTitle('');
                    setOriginalJournalTitle('');
                }
            } catch (err) {
                console.error('Error loading journal details:', err);
                setJournalTitle('');
                setOriginalJournalTitle('');
            }
        };

        loadJournalDetails();
    }, [journalId, session]);

    useEffect(() => {
        const css = entryJson && entryJson.css ? entryJson.css : '';
        setCssCode(css);
        setOriginalCss(css);
    }, [entryJson]);

    const handleSettingChange = (setting, value) => {
        const newSettings = { ...settings, [setting]: value };

        // if encrypted is turned on, turn published off
        if (setting === 'encrypted' && value && newSettings.published) {
            newSettings.published = false;
        }

        setSettings(newSettings);
        setSettingsChanged(true);
    };

    const handleDatePaste = (e) => {
        e.preventDefault();
        const pastedText = e.clipboardData.getData('text');

        try {
            const parsedDate = new Date(pastedText);

            if (!isNaN(parsedDate.getTime())) {
                const year = parsedDate.getFullYear();
                const month = String(parsedDate.getMonth() + 1).padStart(2, '0');
                const day = String(parsedDate.getDate()).padStart(2, '0');
                const hours = String(parsedDate.getHours()).padStart(2, '0');
                const minutes = String(parsedDate.getMinutes()).padStart(2, '0');

                const formattedDate = `${year}-${month}-${day}T${hours}:${minutes}`;
                handleSettingChange('created', formattedDate);
            } else {
                console.warn('Could not parse date:', pastedText);
            }
        } catch (err) {
            console.error('Error parsing pasted date:', err);
        }
    };

    const handleThumbnailChange = (e) => {
        const value = e.target.value;
        handleSettingChange('thumbnail', value === '' ? null : value);
    };

    const handleSaveSettings = async () => {
        if (!settingsChanged) return;

        setSaveStatus('saving');
        try {
            let promises = [];
            const originalPublished = entry.status === 2;
            const originalCreated = entry.created ? new Date(entry.created).toISOString().slice(0, 16) : '';
            const originalChapterId = entry.chapterId || null;
            const originalThumbnail = entry.thumbnail || null;

            if (settings.encrypted !== entry.encrypted) {
                promises.push(setEntryEncrypted(entry.id, settings.encrypted));
            }

            if (settings.published !== originalPublished) {
                promises.push(setEntryPublished(entry.id, settings.published));
            }

            if (settings.created !== originalCreated) {
                const localDate = new Date(settings.created);
                const utcDate = new Date(localDate.getTime() - (localDate.getTimezoneOffset() * 60000));
                promises.push(updateEntryCreated(entry.id, utcDate.toISOString()));
            }

            if (settings.chapterId !== originalChapterId) {
                promises.push(setEntryChapter(entry.id, settings.chapterId));
            }

            if (settings.thumbnail !== originalThumbnail) {
                const selectedModule = imageModules.find(m => m.id === settings.thumbnail);
                if (!selectedModule) {
                    promises.push(updateEntryThumbnail(entry.id, ''));
                } else {
                    promises.push(updateEntryThumbnail(entry.id, selectedModule.image));
                }
            }

            await Promise.all(promises);

            // Build updated entry object
            const updatedEntry = {
                ...entry,
                encrypted: settings.encrypted,
                status: settings.published ? 2 : 1,
                created: settings.created ? (() => {
                    const localDate = new Date(settings.created);
                    const utcDate = new Date(localDate.getTime() - (localDate.getTimezoneOffset() * 60000));
                    return utcDate.toISOString();
                })() : entry.created,
                chapterId: settings.chapterId,
                thumbnail: settings.thumbnail
            };

            setSaveStatus('saved');
            setTimeout(() => {
                onSaved(updatedEntry);
                onClose();
            }, 500);

        } catch (err) {
            console.error('Error saving settings:', err);
            setSaveStatus('error');
        }
    };

    const handleArchiveEntry = async () => {
        if (!window.confirm('Are you sure you want to archive this entry?')) return;

        setSaveStatus('saving');
        try {
            await archiveEntry(entry.id);

            const updatedEntry = { ...entry, status: 0 };

            setSaveStatus('saved');
            setTimeout(() => {
                onSaved(updatedEntry);
                onClose();
            }, 500);

        } catch (err) {
            console.error('Error archiving entry:', err);
            setSaveStatus('error');
        }
    };

    const handleShowChapterManagement = () => {
        setShowChapterManagement(true);
    };

    const handleBackFromChapterManagement = () => {
        setShowChapterManagement(false);
    };

    const handleChaptersChanged = async (updatedChapters) => {
        if (onChaptersChanged) {
            onChaptersChanged(updatedChapters);
        }
    };

    const handleJournalTitleChange = (e) => {
        setJournalTitle(e.target.value);
    };

    const handleJournalCancelDetails = () => {
        setJournalTitle(originalJournalTitle);
    };

    const handleJournalSaveDetails = async () => {
        if (!journalId) return;

        if (journalTitle === originalJournalTitle) {
            return;
        }

        setJournalSaving(true);
        try {
            const api = Journals(session);
            await api.renameJournal(journalId, journalTitle);
            setOriginalJournalTitle(journalTitle);
        } catch (err) {
            console.error('Error saving journal title:', err);
        } finally {
            setJournalSaving(false);
        }
    };

    const handleEditorChange = (value) => {
        setCssCode(value || '');
    };

    const handleCancelCSS = () => {
        setCssCode(originalCss);
    };

    const handleSaveCSS = async () => {
        if (cssCode === originalCss) {
            return;
        }

        setCssSaving(true);
        try {
            const newEntryJson = {
                ...(entryJson || {}),
                css: cssCode
            };

            if (onUpdate) {
                await onUpdate(newEntryJson);
            }

            setOriginalCss(cssCode);
        } catch (err) {
            console.error('Error saving entry CSS:', err);
        } finally {
            setCssSaving(false);
        }
    };

    const hasJournalDetailsChanges = journalTitle !== originalJournalTitle;
    const hasCSSChanges = cssCode !== originalCss;

    return (
        <Modal title="Entry Settings" onClose={onClose} width="600px" className={'entry-settings-modal selected-tab-' + selectedTab}>
            {!showChapterManagement ? (
                <>
                    <Tabs tabs={['Details', 'Journal', 'Images', 'CSS']} selectedIndex={selectedTab} onChange={setSelectedTab}>
                        {/* Details Tab */}
                        <div className="settings-modal-content">
                            <div className="form-row-block">
                                <div className="form-group chapter-selection">
                                    <label>Chapter</label>
                                    <div className="flex tool-bar">
                                        <Select
                                            name="chapter"
                                            value={settings.chapterId || ''}
                                            onChange={(e) => handleSettingChange('chapterId', e.target.value ? parseInt(e.target.value) : null)}
                                            options={[
                                                { value: '', label: 'No Chapter' },
                                                ...chapters.map(ch => ({ value: ch.chapterId, label: ch.sort + ': ' + ch.title }))
                                            ]}
                                        ></Select>
                                        <button onClick={handleShowChapterManagement}>
                                            <Icon name="add" /> Add Chapter
                                        </button>
                                    </div>
                                </div>
                            </div>
                            <div className="form-row-block">
                                <Input
                                    name="created"
                                    label="Creation Date"
                                    type="datetime-local"
                                    value={settings.created}
                                    onInput={(e) => handleSettingChange('created', e.target.value)}
                                    onPaste={handleDatePaste}
                                />
                            </div>
                            <div className="form-row-block">
                                <div className="col-2">
                                    <ToggleSwitch
                                        name="encrypted"
                                        label="Encrypted"
                                        checked={settings.encrypted}
                                        title="Encrypted entries are only visible to you and cannot be shared with others."
                                        onChange={(isChecked) => handleSettingChange('encrypted', isChecked)}
                                    />
                                </div>
                                <div className="col-2">
                                    <ToggleSwitch
                                        name="published"
                                        label="Published"
                                        checked={settings.published}
                                        title="Published entries are visible to anyone who has access to the journal."
                                        onChange={(isChecked) => handleSettingChange('published', isChecked)}
                                        disabled={settings.encrypted}
                                    />
                                </div>
                            </div>

                            <div className="buttons">
                                {entry.status !== 0 && (
                                    <button className="btn" style={{ backgroundColor: '#c62828', color: 'white' }} onClick={handleArchiveEntry}>Archive Entry</button>
                                )}
                                {settingsChanged && (
                                    <button className="btn primary" onClick={handleSaveSettings}>Save Changes</button>
                                )}
                                <button className="btn cancel" onClick={onClose}>Cancel</button>
                            </div>
                        </div>

                        {/* Journal Tab */}
                        <div className="settings-modal-content">
                            <div className="form-row-block">
                                <div className="form-group">
                                    <label htmlFor="journal-title">Journal Title</label>
                                    <Input
                                        id="journal-title"
                                        type="text"
                                        value={journalTitle}
                                        onChange={handleJournalTitleChange}
                                        placeholder="Enter journal title"
                                    />
                                </div>
                            </div>
                            {hasJournalDetailsChanges && (
                                <div className="buttons">
                                    <button onClick={handleJournalCancelDetails} className="cancel" disabled={journalSaving}>
                                        Cancel
                                    </button>
                                    <button onClick={handleJournalSaveDetails} disabled={journalSaving}>
                                        {journalSaving ? (
                                            <>
                                                <Icon name="progress_activity" spin={true} />
                                                Saving...
                                            </>
                                        ) : (
                                            <>
                                                <Icon name="save" />
                                                Save
                                            </>
                                        )}
                                    </button>
                                </div>
                            )}
                        </div>

                        {/* Images Tab */}
                        <div className="settings-modal-content">
                            <div className="form-row-block">
                                <div className="form-group">
                                    <label>Thumbnail Image</label>
                                    <Select
                                        name="thumbnail"
                                        value={settings.thumbnail || ''}
                                        onChange={handleThumbnailChange}
                                        options={[
                                            { value: '', label: 'No Thumbnail' },
                                            ...thumbnailModules.map((module, index) => {
                                                let label;
                                                if (module.type === 'video-player') {
                                                    label = module.title || `Video ${index + 1}`;
                                                } else {
                                                    label = module.caption || module.alt || `Image ${index + 1}`;
                                                }
                                                return {
                                                    value: module.id,
                                                    label: label
                                                };
                                            })
                                        ]}
                                    />
                                    {thumbnailModules.length === 0 && (
                                        <p style={{ marginTop: '10px', color: '#666', fontSize: '14px' }}>
                                            No images or videos found in this entry. Add an image or video module to select a thumbnail.
                                        </p>
                                    )}
                                    {settings.thumbnail && thumbnailModules.length > 0 && (
                                        <div style={{ marginTop: '15px' }}>
                                            <label style={{ display: 'block', marginBottom: '8px' }}>Preview:</label>
                                            {(() => {
                                                const selectedModule = thumbnailModules.find(m => m.id === settings.thumbnail);
                                                if (selectedModule) {
                                                    let imageSrc;
                                                    if (selectedModule.type === 'video-player' && selectedModule.thumbnailPath) {
                                                        imageSrc = apiBasePath() + `/video/thumb/${selectedModule.thumbnailPath}`;
                                                    } else if (selectedModule.image) {
                                                        imageSrc = apiBasePath() + `/image/journal-entries/${entry.id}/${selectedModule.image}`;
                                                    }

                                                    if (imageSrc) {
                                                        return (
                                                            <img
                                                                src={imageSrc}
                                                                alt="Thumbnail preview"
                                                                style={{
                                                                    maxWidth: '100%',
                                                                    maxHeight: '200px',
                                                                    borderRadius: '4px',
                                                                    border: '1px solid #ddd'
                                                                }}
                                                            />
                                                        );
                                                    }
                                                }
                                                return null;
                                            })()}
                                        </div>
                                    )}
                                </div>
                            </div>
                        </div>

                        {/* CSS Tab */}
                        <div className="settings-modal-content">
                            <div className="form-row-block">
                                <div className="form-group">
                                    <label htmlFor="page-css">Page CSS</label>
                                    <div className="css-editor-container">
                                        <Editor
                                            height="50vh"
                                            defaultLanguage="css"
                                            language="css"
                                            theme={theme}
                                            value={cssCode}
                                            onChange={handleEditorChange}
                                            options={{
                                                minimap: { enabled: false },
                                                lineNumbers: 'on',
                                                scrollBeyondLastLine: false,
                                                automaticLayout: true,
                                            }}
                                        />
                                    </div>
                                </div>
                            </div>
                            {hasCSSChanges && (
                                <div className="buttons">
                                    <button onClick={handleCancelCSS} className="cancel" disabled={cssSaving}>
                                        Cancel
                                    </button>
                                    <button onClick={handleSaveCSS} disabled={cssSaving}>
                                        {cssSaving ? (
                                            <>
                                                <Icon name="progress_activity" spin={true} />
                                                Saving...
                                            </>
                                        ) : (
                                            <>
                                                <Icon name="save" />
                                                Save
                                            </>
                                        )}
                                    </button>
                                </div>
                            )}
                        </div>
                    </Tabs>

                </>
            ) : (
                <>
                    <div className="tool-bar">
                        <button onClick={handleBackFromChapterManagement}>
                            <Icon name="arrow_back" /> Back
                        </button>
                    </div>
                    <JournalChapters
                        journalId={journalId}
                        onChaptersChanged={handleChaptersChanged}
                    />
                </>
            )}
        </Modal>
    );
}
