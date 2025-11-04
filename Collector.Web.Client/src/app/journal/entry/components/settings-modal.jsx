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

export default function SettingsModal({
    entry,
    entryJson,
    journalId,
    chapters,
    onClose,
    onSaved,
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

    // Get all image modules from entryJson
    const imageModules = entryJson?.modules?.filter(module => module.type === 'image') || [];

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

    return (
        <Modal title="Entry Settings" onClose={onClose} width="600px" className={'selected-tab-' + selectedTab}>
            {!showChapterManagement ? (
                <>
                    <Tabs tabs={['Details', 'Images']} selectedIndex={selectedTab} onChange={setSelectedTab}>
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
                                            ...imageModules.map((module, index) => ({
                                                value: module.id,
                                                label: module.caption || module.alt || `Image ${index + 1}`
                                            }))
                                        ]}
                                    />
                                    {imageModules.length === 0 && (
                                        <p style={{ marginTop: '10px', color: '#666', fontSize: '14px' }}>
                                            No images found in this entry. Add an image module to select a thumbnail.
                                        </p>
                                    )}
                                    {settings.thumbnail && imageModules.length > 0 && (
                                        <div style={{ marginTop: '15px' }}>
                                            <label style={{ display: 'block', marginBottom: '8px' }}>Preview:</label>
                                            {(() => {
                                                const selectedModule = imageModules.find(m => m.id === settings.thumbnail);
                                                if (selectedModule && selectedModule.image) {
                                                    return (
                                                        <img
                                                            src={apiBasePath() + `/image/journal-entries/${entry.id}/${selectedModule.image}`}
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
                        </div>
                    </Tabs>

                    <div className="buttons">
                        {entry.status !== 0 && (
                            <button className="btn" style={{ backgroundColor: '#c62828', color: 'white' }} onClick={handleArchiveEntry}>Archive Entry</button>
                        )}
                        {settingsChanged && (
                            <button className="btn primary" onClick={handleSaveSettings}>Save Changes</button>
                        )}
                        <button className="btn cancel" onClick={onClose}>Cancel</button>
                    </div>
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
