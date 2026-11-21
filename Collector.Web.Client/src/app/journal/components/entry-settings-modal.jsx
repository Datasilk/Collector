import { hideModal } from '@/context/modal';
import React, { useState, useEffect, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import Modal from '@/components/ui/modal';
import Tabs from '@/components/ui/tabs';
import Icon from '@/components/ui/icon';
import Input from '@/components/forms/input';
import Select from '@/components/forms/select';
import ToggleSwitch from '@/components/ui/toggle-switch';
import JournalChapters from '@/components/journal/journal-chapters';
import MoveEntryModal from './move-entry-modal';
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
    const navigate = useNavigate();
    const { setEntryEncrypted, setEntryPublished, updateEntryCreated, setEntryChapter, updateEntryThumbnail, archiveEntry, getJournal, archiveJournal } = Journals(session);

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
    const [showMoveModal, setShowMoveModal] = useState(false);
    const [jsonDump, setJsonDump] = useState(null);
    const [saveStatus, setSaveStatus] = useState(null);

    const [journalTitle, setJournalTitle] = useState('');
    const [originalJournalTitle, setOriginalJournalTitle] = useState('');
    const [journalCategoryId, setJournalCategoryId] = useState('');
    const [originalJournalCategoryId, setOriginalJournalCategoryId] = useState('');
    const [categories, setCategories] = useState([]);
    const [journalData, setJournalData] = useState(null);
    const [cssCode, setCssCode] = useState('');
    const [originalCss, setOriginalCss] = useState('');
    const [journalSaving, setJournalSaving] = useState(false);
    const [cssSaving, setCssSaving] = useState(false);
    const [thumbnailSaving, setThumbnailSaving] = useState(false);
    const [originalThumbnail] = useState(entry.thumbnail || null);
    const [theme] = useState('vs-dark');

    // Recursive function to flatten module hierarchy
    const flattenModules = (modules) => {
        if (!modules || !Array.isArray(modules)) return [];

        const flattened = [];
        for (const module of modules) {
            flattened.push(module);
            // Recursively flatten nested modules
            if (module.modules && Array.isArray(module.modules)) {
                flattened.push(...flattenModules(module.modules));
            }
            // Handle tabs module structure where modules are in tabs[x].modules
            if (module.tabs && Array.isArray(module.tabs)) {
                for (const tab of module.tabs) {
                    if (tab.modules && Array.isArray(tab.modules)) {
                        flattened.push(...flattenModules(tab.modules));
                    }
                }
            }
        }
        return flattened;
    };

    // Get all image modules and video modules with thumbnails from entryJson
    const thumbnailOptions = useMemo(() => {
        const modules = entryJson?.modules || [];
        const allModules = flattenModules(modules);
        const options = [];
        let videoIndex = 0;
        let imageIndex = 0;
        let galleryIndex = 0;

        allModules.forEach(module => {
            if (module.type === 'video-player' && (module.thumbnailPath || module.videoPath)) {
                videoIndex++;
                options.push({
                    moduleId: module.id,
                    path: module.thumbnailPath || module.videoPath,
                    label: module.title || `Video ${videoIndex}`,
                    type: 'video',
                    thumbnailPath: module.thumbnailPath,
                    videoId: module.videoId
                });
            } else if (module.type === 'image' && module.image) {
                imageIndex++;
                options.push({
                    moduleId: module.id,
                    path: module.image,
                    label: module.caption || module.alt || `Image ${imageIndex}`,
                    type: 'image'
                });
            } else if (module.type === 'image-gallery' && module.images && module.images.length > 0) {
                galleryIndex++;
                // Add each image from the gallery as a separate option
                module.images.forEach((imagePath, imgIndex) => {
                    // gallery images may be bare paths or {url,...} objects
                    const path = typeof imagePath === 'string' ? imagePath : (imagePath?.url || imagePath?.path || '');
                    if (!path) return;
                    options.push({
                        moduleId: module.id,
                        path: path,
                        label: `Gallery ${galleryIndex} - Image ${imgIndex + 1}`,
                        type: 'image-gallery'
                    });
                });
            }
        });

        return options;
    }, [entryJson?.modules]);
    useEffect(() => {
        if (!journalId) return;

        const loadJournalDetails = async () => {
            try {
                const api = Journals(session);
                const [journalResponse, categoriesResponse] = await Promise.all([
                    api.getJournal(journalId),
                    api.getCategories()
                ]);

                if (journalResponse.data?.success && journalResponse.data.data) {
                    const journal = journalResponse.data.data;
                    setJournalData(journal);
                    const title = journal.title || '';
                    const categoryId = journal.categoryId ? journal.categoryId.toString() : '';
                    setJournalTitle(title);
                    setOriginalJournalTitle(title);
                    setJournalCategoryId(categoryId);
                    setOriginalJournalCategoryId(categoryId);
                } else {
                    setJournalTitle('');
                    setOriginalJournalTitle('');
                    setJournalCategoryId('');
                    setOriginalJournalCategoryId('');
                }

                if (categoriesResponse.data?.success) {
                    setCategories(categoriesResponse.data.data || []);
                }
            } catch (err) {
                console.error('Error loading journal details:', err);
                setJournalTitle('');
                setOriginalJournalTitle('');
                setJournalCategoryId('');
                setOriginalJournalCategoryId('');
            }
        };

        loadJournalDetails();
    }, [journalId, session]);

    useEffect(() => {
        const css = entryJson && entryJson.css ? entryJson.css : '';
        setCssCode(css);
        setOriginalCss(css);
    }, [entryJson]);

    // Research/sources/resources live in research.json beside entry.json —
    // load them when the settings modal opens.
    const [researchJson, setResearchJson] = useState(null);
    useEffect(() => {
        if (!entry?.id) return;
        const api = Journals(session);
        api.getEntryResearch(entry.id).then(response => {
            if (response.data?.success && response.data.data) {
                try { setResearchJson(JSON.parse(response.data.data)); }
                catch { setResearchJson(null); }
            }
        }).catch(err => {
            console.error('Error loading entry research:', err);
        });
    }, [entry?.id, session]);

    // Merged view: research.json wins, embedded entryJson values are the
    // legacy fallback for entries that haven't been re-saved yet.
    const research = researchJson?.research || entryJson?.research || null;
    const resources = researchJson?.resources || entryJson?.resources || [];

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

            let thumbnailModuleId = entry.thumbnailModuleId;
            if (settings.thumbnail !== originalThumbnail) {
                const selectedOption = thumbnailOptions.find(opt => opt.path === settings.thumbnail);
                thumbnailModuleId = selectedOption?.moduleId || null;
                promises.push(updateEntryThumbnail(entry.id, settings.thumbnail || '', thumbnailModuleId));
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
                thumbnail: settings.thumbnail,
                thumbnailModuleId: thumbnailModuleId
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

    const handleMoveEntry = () => {
        setShowMoveModal(true);
    };

    const handleMoveModalClose = () => {
        setShowMoveModal(false);
    };

    const handleOpenJsonDump = (e) => {
        e.preventDefault();
        setJsonDump({
            file: e.currentTarget.dataset.file,
            url: e.currentTarget.href
        });
    };

    const handleCloseJsonDump = () => {
        setJsonDump(null);
    };

    const handleEntryMoved = async (targetJournal) => {
        // Navigate to the new journal/entry URL
        hideModal();
        navigate(`/journal/${targetJournal.id}/entry/${entry.id}`);
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

    const handleJournalCategoryChange = (e) => {
        setJournalCategoryId(e.target.value);
    };

    const handleJournalCancelDetails = () => {
        setJournalTitle(originalJournalTitle);
        setJournalCategoryId(originalJournalCategoryId);
    };

    const handleJournalSaveDetails = async () => {
        if (!journalId) return;

        const titleChanged = journalTitle !== originalJournalTitle;
        const categoryChanged = journalCategoryId !== originalJournalCategoryId;

        if (!titleChanged && !categoryChanged) {
            return;
        }

        setJournalSaving(true);
        try {
            const api = Journals(session);

            if (titleChanged) {
                await api.renameJournal(journalId, journalTitle);
            }

            if (categoryChanged) {
                await api.changeJournalCategory(journalId, parseInt(journalCategoryId));
                // Refresh the page to reload journal categories
                window.location.reload();
                return;
            }

            setOriginalJournalTitle(journalTitle);
            setOriginalJournalCategoryId(journalCategoryId);
        } catch (err) {
            console.error('Error saving journal details:', err);
        } finally {
            setJournalSaving(false);
        }
    };

    const handleArchiveJournal = async () => {
        if (!window.confirm(`Do you really want to archive the journal, "${journalTitle}"?`)) return;

        try {
            await archiveJournal(journalId);
            localStorage.setItem('collector:journal:selected', null);
            navigate('/journal');
        } catch (err) {
            console.error('Error archiving journal:', err);
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

    const handleCancelThumbnail = () => {
        setSettings(prev => ({ ...prev, thumbnail: originalThumbnail }));
    };

    const handleSaveThumbnail = async () => {
        if (settings.thumbnail === originalThumbnail) return;

        setThumbnailSaving(true);
        try {
            // Find the moduleId for the selected thumbnail
            const selectedOption = thumbnailOptions.find(opt => opt.path === settings.thumbnail);
            const moduleId = selectedOption?.moduleId || null;

            // settings.thumbnail is now the path directly; sourceEntryId lets the
            // server expand bare image filenames into the journal-relative images path
            await updateEntryThumbnail(entry.id, settings.thumbnail || '', moduleId, entry.id);

            // Update entry with new thumbnail and moduleId
            if (onSaved) {
                onSaved({ ...entry, thumbnail: settings.thumbnail, thumbnailModuleId: moduleId });
            }
        } catch (err) {
            console.error('Error saving thumbnail:', err);
        } finally {
            setThumbnailSaving(false);
        }
    };

    const hasJournalDetailsChanges = journalTitle !== originalJournalTitle || journalCategoryId !== originalJournalCategoryId;
    const isJournalEntry = journalData && journalData.entryId === entry.id;
    const tabsList = isJournalEntry
        ? ['Details', 'Journal', 'Images', 'CSS', 'Research', 'Sources', 'Resources']
        : ['Details', 'Images', 'CSS', 'Research', 'Sources', 'Resources'];
    const hasCSSChanges = cssCode !== originalCss;
    const hasThumbnailChanges = settings.thumbnail !== originalThumbnail;

    if (showMoveModal) {
        return (
            <MoveEntryModal
                entry={entry}
                currentJournalId={journalId}
                onClose={handleMoveModalClose}
                onMoved={handleEntryMoved}
            />
        );
    }

    return (
        <>
        <Modal title="Entry Settings" onClose={onClose} width="600px" className={'entry-settings-modal selected-tab-' + selectedTab}>
            {!showChapterManagement ? (
                <>
                    <Tabs tabs={tabsList} selectedIndex={selectedTab} onChange={setSelectedTab}>
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
                                    <>
                                        <button className="btn" style={{ backgroundColor: '#c62828', color: 'white' }} onClick={handleArchiveEntry}>Archive</button>
                                        <button className="btn" onClick={handleMoveEntry}>Move</button>
                                    </>
                                )}
                                {settingsChanged && (
                                    <button className="btn primary" onClick={handleSaveSettings}>Save Changes</button>
                                )}
                                <button className="btn cancel" onClick={onClose}>Cancel</button>
                            </div>
                        </div>

                        {/* Journal Tab */}
                        {isJournalEntry &&
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
                                <div className="form-row-block">
                                    <div className="form-group">
                                        <label htmlFor="journal-category">Category</label>
                                        <Select
                                            id="journal-category"
                                            name="category"
                                            value={journalCategoryId}
                                            onChange={handleJournalCategoryChange}
                                            options={[
                                                { value: '', label: 'Select a category...' },
                                                ...categories.map(cat => ({
                                                    value: cat.id.toString(),
                                                    label: cat.title
                                                }))
                                            ]}
                                        />
                                    </div>
                                </div>
                                <div className="buttons">
                                    <button className="btn" style={{ backgroundColor: '#c62828', color: 'white' }} onClick={handleArchiveJournal}>Archive</button>
                                    {hasJournalDetailsChanges && (
                                        <>
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
                                        </>
                                    )}
                                </div>
                            </div>
                        }

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
                                            ...thumbnailOptions.map(opt => ({
                                                value: opt.path,
                                                label: opt.label
                                            }))
                                        ]}
                                    />
                                    {thumbnailOptions.length === 0 && (
                                        <p style={{ marginTop: '10px', color: '#666', fontSize: '14px' }}>
                                            No images or videos found in this entry. Add an image or video module to select a thumbnail.
                                        </p>
                                    )}
                                    {settings.thumbnail && (
                                        <div style={{ marginTop: '15px' }}>
                                            <label style={{ display: 'block', marginBottom: '8px' }}>Preview:</label>
                                            {(() => {
                                                const selected = thumbnailOptions.find(opt => opt.path === settings.thumbnail);
                                                let imageSrc;

                                                if (selected) {
                                                    if (selected.type === 'video') {
                                                        if (selected.thumbnailPath) {
                                                            imageSrc = apiBasePath() + `/video/thumb/${selected.thumbnailPath}`;
                                                        } else if (selected.videoId) {
                                                            imageSrc = apiBasePath() + `/video/thumbnail/${selected.videoId}`;
                                                        }
                                                    } else if (typeof selected.path !== 'string') {
                                                        const gallerySrc = selected.path?.url || '';
                                                        imageSrc = /^https?:\/\//i.test(gallerySrc)
                                                            ? gallerySrc
                                                            : (gallerySrc.includes('/')
                                                                ? apiBasePath() + `/image/${gallerySrc}`
                                                                : apiBasePath() + `/image/${journalId}/${entry.id}/images/${gallerySrc}`);
                                                    } else if (/^https?:\/\//i.test(selected.path)) {
                                                        imageSrc = selected.path;
                                                    } else if (selected.path.includes('/')) {
                                                        imageSrc = apiBasePath() + `/image/${selected.path}`;
                                                    } else if (selected.type === 'image-gallery') {
                                                        // Use thumbnail for gallery images
                                                        imageSrc = apiBasePath() + `/image/${journalId}/${entry.id}/images/thumb_${selected.path}`;
                                                    } else {
                                                        imageSrc = apiBasePath() + `/image/${journalId}/${entry.id}/images/${selected.path}`;
                                                    }
                                                } else if (settings.thumbnail) {
                                                    // Fallback for existing thumbnails not in current modules —
                                                    // thumbnail may arrive as a path string or a {url|path} object
                                                    const thumb = typeof settings.thumbnail === 'string'
                                                        ? settings.thumbnail
                                                        : (settings.thumbnail.path || settings.thumbnail.url || '');
                                                    if (thumb.split('/').length >= 3) {
                                                        imageSrc = apiBasePath() + `/image/${thumb}`;
                                                    } else if (thumb.includes('/') || thumb.endsWith('.mp4')) {
                                                        imageSrc = apiBasePath() + `/video/thumb/${thumb}`;
                                                    } else if (thumb) {
                                                        imageSrc = apiBasePath() + `/image/${journalId}/${entry.id}/images/${thumb}`;
                                                    }
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
                                                return null;
                                            })()}
                                        </div>
                                    )}
                                </div>
                            </div>
                            {hasThumbnailChanges && (
                                <div className="buttons">
                                    <button onClick={handleCancelThumbnail} className="cancel" disabled={thumbnailSaving}>
                                        Cancel
                                    </button>
                                    <button onClick={handleSaveThumbnail} disabled={thumbnailSaving}>
                                        {thumbnailSaving ? (
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

                        {/* Research Tab — read-only topic/fact accordions */}
                        <div className="settings-modal-content research-tab">
                            {research?.topics?.length > 0 ? (
                                research.topics.map((topic, i) => (
                                    <ResearchTopicAccordion key={i} topic={topic} depth={0} />
                                ))
                            ) : (
                                <p className="research-empty">No research recorded yet — it appears here once the AI collects research for this entry.</p>
                            )}
                        </div>

                        {/* Sources Tab — spreadsheet of scraped pages + their parser dumps */}
                        <div className="settings-modal-content sources-tab">
                            {(() => {
                                const srcItems = Array.isArray(research?.sources)
                                    ? research.sources
                                    : (research?.sources?.items || []);
                                return srcItems.length > 0 ? (
                                    <table className="sources-table">
                                        <thead>
                                            <tr>
                                                <th>Source</th>
                                                <th className="col-file">Dump</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            {srcItems.map((src, i) => (
                                                <tr key={i}>
                                                    <td>
                                                        <a href={src.url} target="_blank" rel="noopener noreferrer">
                                                            {src.title || src.url}
                                                        </a>
                                                    </td>
                                                    <td className="col-file">
                                                        {src.file
                                                            ? <a href={`${apiBasePath()}/file/${journalId}/${String(entry.id).replaceAll('-', '')}/research/${src.file}`}
                                                                data-file={src.file}
                                                                onClick={handleOpenJsonDump}>
                                                                {src.file}
                                                            </a>
                                                            : '—'}
                                                    </td>
                                                </tr>
                                            ))}
                                        </tbody>
                                    </table>
                                ) : (
                                    <p className="research-empty">No sources recorded yet.</p>
                                );
                            })()}
                        </div>

                        {/* Resources Tab — downloaded assets: image grid + files table */}
                        <div className="settings-modal-content resources-tab">
                            <ResourcesTab resources={resources} />
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
        {jsonDump && (
            <JsonDumpModal
                file={jsonDump.file}
                url={jsonDump.url}
                onClose={handleCloseJsonDump}
            />
        )}
        </>
    );
}

// Read-only Resources tab — images downloaded into the entry shown in a
// 3-per-row grid, every other resource in a Filename/Source/Download table.
function ResourcesTab({ resources }) {
    const isImage = (r) => /\.(jpe?g|png|gif|webp|svg|bmp|avif|tiff?|heic|heif)$/i.test(r?.filename || '');
    const basename = (p) => (p || '').split('/').filter(Boolean).pop() || '';
    const images = resources.filter(isImage);
    const files = resources.filter(r => !isImage(r));

    if (resources.length === 0) {
        return <p className="research-empty">No resources downloaded yet — they appear here once the AI saves images or files into this entry.</p>;
    }

    return (
        <>
            <h4 className="resources-heading">Images</h4>
            {images.length > 0 ? (
                <div className="resources-image-grid">
                    {images.map((r, i) => (
                        <a key={i} href={apiBasePath() + `/image/${r.filename}`} target="_blank" rel="noopener noreferrer"
                            title={basename(r.filename)}>
                            <img src={apiBasePath() + `/image/${r.filename}`} alt={basename(r.filename)} />
                        </a>
                    ))}
                </div>
            ) : (
                <p className="research-empty">No images downloaded.</p>
            )}

            <h4 className="resources-heading">Files</h4>
            {files.length > 0 ? (
                <table className="resources-table">
                    <thead>
                        <tr>
                            <th>Filename</th>
                            <th>Source</th>
                            <th className="col-download">Download</th>
                        </tr>
                    </thead>
                    <tbody>
                        {files.map((r, i) => (
                            <tr key={i}>
                                <td>{basename(r.filename)}</td>
                                <td>
                                    {r.page
                                        ? <a href={r.page} target="_blank" rel="noopener noreferrer">{r.page}</a>
                                        : '—'}
                                </td>
                                <td className="col-download">
                                    <a href={apiBasePath() + `/image/${r.filename}`} download={basename(r.filename)} title="Download">
                                        <Icon name="download" />
                                    </a>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            ) : (
                <p className="research-empty">No files downloaded.</p>
            )}
        </>
    );
}

// Read-only accordion for the Research tab — one <details> per topic, its
// content blocks rendered beneath it, subtopics nested recursively.
// Archived topics show a badge but remain visible for context.
function ResearchTopicAccordion({ topic, depth }) {
    const archived = topic.status === 'archived';
    const content = Array.isArray(topic.content) ? topic.content : null;
    const title = topic.title || topic.topic || 'Untitled';

    // Legacy fallback: fact-list research objects that predate typed blocks.
    if (!content) {
        const facts = topic.facts || [];
        const subtopics = topic.subtopics || [];
        return (
            <details className={`research-topic depth-${depth}`} open={depth === 0}>
                <summary>
                    <span className="research-topic-title">{title}</span>
                    {archived && <span className="research-archived-badge">archived</span>}
                    <span className="research-fact-count">{facts.length} {facts.length === 1 ? 'fact' : 'facts'}</span>
                </summary>
                {facts.length > 0 && (
                    <ul className="research-facts">
                        {facts.map((f, i) => (
                            <li key={i}>
                                {f.text}
                                {f.source != null && <span className="research-source-ref">[{f.source}]</span>}
                            </li>
                        ))}
                    </ul>
                )}
                {subtopics.map((st, i) => (
                    <ResearchTopicAccordion key={i} topic={st} depth={depth + 1} />
                ))}
            </details>
        );
    }

    const blockCount = content.filter(b => b?.type !== 'sub_topics').length;
    return (
        <details className={`research-topic depth-${depth}`} open={depth === 0}>
            <summary>
                <span className="research-topic-title">{title}</span>
                {archived && <span className="research-archived-badge">archived</span>}
                <span className="research-fact-count">{blockCount} {blockCount === 1 ? 'item' : 'items'}</span>
            </summary>
            {content.map((block, i) => (
                <ResearchBlock key={i} block={block} depth={depth} />
            ))}
        </details>
    );
}

// One typed research content block — title → h4, paragraph → p, lists →
// ul/ol (object items with a url render as anchors), table → table,
// image → img, image-gallery → grid, sub_topics → nested accordions.
function ResearchBlock({ block, depth }) {
    if (!block || typeof block !== 'object') return null;
    const src = block.source != null
        ? <span className="research-source-ref">[{block.source}]</span>
        : null;

    const cellText = (c) => (c && typeof c === 'object' ? c.text : c) ?? '';
    const cellUrl = (c) => (c && typeof c === 'object' ? c.url : null);

    switch (block.type) {
        case 'title':
            return <h4 className="research-block-title">{block.text}{src}</h4>;
        case 'paragraph':
            return <p className="research-paragraph">{block.text}{src}</p>;
        case 'bullet_list':
        case 'numbered_list': {
            const ListTag = block.type === 'bullet_list' ? 'ul' : 'ol';
            return (
                <ListTag className="research-list">
                    {(block.items || []).map((item, i) => {
                        const text = cellText(item);
                        const url = cellUrl(item);
                        return (
                            <li key={i}>
                                {url
                                    ? <a href={url} target="_blank" rel="noopener noreferrer">{text}</a>
                                    : text}
                            </li>
                        );
                    })}
                    {src}
                </ListTag>
            );
        }
        case 'table':
            return (
                <table className="research-table">
                    {block.caption && <caption>{block.caption}{src}</caption>}
                    {block.headers?.length > 0 && (
                        <thead>
                            <tr>{block.headers.map((h, i) => <th key={i}>{cellText(h)}</th>)}</tr>
                        </thead>
                    )}
                    <tbody>
                        {(block.rows || []).map((row, i) => (
                            <tr key={i}>
                                {(row || []).map((cell, j) => {
                                    const text = cellText(cell);
                                    const url = cellUrl(cell);
                                    return (
                                        <td key={j}>
                                            {url
                                                ? <a href={url} target="_blank" rel="noopener noreferrer">{text}</a>
                                                : text}
                                        </td>
                                    );
                                })}
                            </tr>
                        ))}
                    </tbody>
                </table>
            );
        case 'image':
            return (
                <figure className="research-image">
                    <a href={block.url} target="_blank" rel="noopener noreferrer">
                        <img src={block.url} alt={block.alt || block.caption || ''} />
                    </a>
                    {block.caption && <figcaption>{block.caption}{src}</figcaption>}
                </figure>
            );
        case 'image-gallery':
            return (
                <div className="research-image-grid">
                    {(block.items || []).map((img, i) => {
                        const url = typeof img === 'object' ? img.url : img;
                        const alt = typeof img === 'object' ? (img.alt || img.caption || '') : '';
                        if (!url) return null;
                        return (
                            <a key={i} href={url} target="_blank" rel="noopener noreferrer" title={alt}>
                                <img src={url} alt={alt} />
                            </a>
                        );
                    })}
                </div>
            );
        case 'sub_topics':
            return (block.items || []).map((sub, i) => (
                <ResearchTopicAccordion key={i} topic={sub} depth={depth + 1} />
            ));
        default:
            return null;
    }
}

// Stacked modal that fetches a research dump (research/####.json) and renders
// it as a collapsible, syntax-highlighted JSON tree.
function JsonDumpModal({ file, url, onClose }) {
    const [data, setData] = useState(undefined);
    const [error, setError] = useState(null);
    const [expandSignal, setExpandSignal] = useState({ v: 0, expand: true });

    useEffect(() => {
        let cancelled = false;
        fetch(url)
            .then(r => {
                if (!r.ok) throw new Error(`HTTP ${r.status}`);
                return r.json();
            })
            .then(j => { if (!cancelled) setData(j); })
            .catch(e => { if (!cancelled) setError(e.message); });
        return () => { cancelled = true; };
    }, [url]);

    const handleExpandAll = (e) => {
        e.preventDefault();
        setExpandSignal(prev => ({ v: prev.v + 1, expand: true }));
    };

    const handleCollapseAll = (e) => {
        e.preventDefault();
        setExpandSignal(prev => ({ v: prev.v + 1, expand: false }));
    };

    return (
        <Modal title={file} onClose={onClose} wide className="json-dump-modal">
            {error && <p className="research-empty">Failed to load {file}: {error}</p>}
            {!error && data === undefined && <p className="research-empty">Loading…</p>}
            {data !== undefined && !error && (
                <>
                    <div className="json-toolbar">
                        <a href="#" onClick={handleExpandAll}>expand all</a>
                        <span className="json-toolbar-sep">·</span>
                        <a href="#" onClick={handleCollapseAll}>collapse all</a>
                    </div>
                    <div className="json-tree">
                        <JsonNode name={null} value={data} depth={0} signal={expandSignal} />
                    </div>
                </>
            )}
        </Modal>
    );
}

// One node of the JSON tree — objects/arrays collapse, primitives render
// inline with type-based coloring. The `signal` prop broadcasts expand-all /
// collapse-all from the modal toolbar to every node.
function JsonNode({ name, value, depth, signal }) {
    const [expanded, setExpanded] = useState(depth < 1);

    useEffect(() => {
        if (signal && signal.v > 0) setExpanded(signal.expand);
    }, [signal]);

    const handleToggle = () => {
        setExpanded(prev => !prev);
    };

    const isObject = value !== null && typeof value === 'object';
    const label = name === null ? null
        : <span className="json-key">{typeof name === 'number' ? name : `"${name}"`}</span>;
    if (!isObject) {
        return (
            <div className="json-leaf">
                {label}
                {label && <span className="json-punct">: </span>}
                <JsonPrimitive value={value} />
            </div>
        );
    }

    const isArray = Array.isArray(value);
    const entries = isArray ? value.map((v, i) => [i, v]) : Object.entries(value);
    const open = isArray ? '[' : '{';
    const close = isArray ? ']' : '}';

    if (entries.length === 0) {
        return (
            <div className="json-leaf">
                {label}
                {label && <span className="json-punct">: </span>}
                <span className="json-punct">{open}{close}</span>
            </div>
        );
    }

    return (
        <div className="json-node">
            <div className="json-row" onClick={handleToggle}>
                <span className="json-toggle"><Icon name={expanded ? 'expand_more' : 'chevron_right'} /></span>
                {label}
                {label && <span className="json-punct">: </span>}
                <span className="json-punct">{open}</span>
                {!expanded && <span className="json-preview"> {entries.length} {isArray ? 'items' : 'keys'} </span>}
                {!expanded && <span className="json-punct">{close}</span>}
            </div>
            {expanded && (
                <div className="json-children">
                    {entries.map(([k, v]) => (
                        <JsonNode key={k} name={k} value={v} depth={depth + 1} signal={signal} />
                    ))}
                    <span className="json-punct">{close}</span>
                </div>
            )}
        </div>
    );
}

function JsonPrimitive({ value }) {
    if (value === null) return <span className="json-null">null</span>;
    if (typeof value === 'boolean') return <span className="json-bool">{String(value)}</span>;
    if (typeof value === 'number') return <span className="json-number">{String(value)}</span>;
    return <span className="json-string">"{String(value)}"</span>;
}
