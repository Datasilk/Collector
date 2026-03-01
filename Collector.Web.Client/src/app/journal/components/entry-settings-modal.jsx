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
                    options.push({
                        moduleId: module.id,
                        path: imagePath,
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

    const handleEntryMoved = async (targetJournal) => {
        // Navigate to the new journal/entry URL
        session.hideModal();
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

            // settings.thumbnail is now the path directly
            await updateEntryThumbnail(entry.id, settings.thumbnail || '', moduleId);

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
    const tabsList = isJournalEntry ? ['Details', 'Journal', 'Images', 'CSS'] : ['Details', 'Images', 'CSS'];
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
                                                    } else if (selected.type === 'image-gallery') {
                                                        // Use thumbnail for gallery images
                                                        imageSrc = apiBasePath() + `/image/journal-entries/${entry.id}/thumb_${selected.path}`;
                                                    } else {
                                                        imageSrc = apiBasePath() + `/image/journal-entries/${entry.id}/${selected.path}`;
                                                    }
                                                } else if (settings.thumbnail) {
                                                    // Fallback for existing thumbnails not in current modules
                                                    if (settings.thumbnail.includes('/') || settings.thumbnail.endsWith('.mp4')) {
                                                        imageSrc = apiBasePath() + `/video/thumb/${settings.thumbnail}`;
                                                    } else {
                                                        imageSrc = apiBasePath() + `/image/journal-entries/${entry.id}/${settings.thumbnail}`;
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
