import React, { useState, useEffect } from 'react';
import Modal from '@/components/ui/modal';
import Input from '@/components/forms/input';
import Editor from '@monaco-editor/react';
import Icon from '@/components/ui/icon';
import Tabs from '@/components/ui/tabs';
import JournalChapters from '@/components/journal/journal-chapters';
import { useSession } from '@/context/session';
import { Journals } from '@/api/user/journals';

export default function JournalSettingsModal({ journal, onClose, onSaved }) {
    const session = useSession();
    const [title, setTitle] = useState('');
    const [originalTitle, setOriginalTitle] = useState('');
    const [cssCode, setCssCode] = useState('');
    const [originalCss, setOriginalCss] = useState('');
    const [saving, setSaving] = useState(false);
    const [theme] = useState('vs-dark');
    const [selectedTab, setSelectedTab] = useState(0);

    useEffect(() => {
        if (journal) {
            const journalTitle = journal.title || '';
            setTitle(journalTitle);
            setOriginalTitle(journalTitle);
            loadJournalSettings();
        }
    }, [journal]);

    const loadJournalSettings = async () => {
        if (!journal) return;

        try {
            const api = Journals(session);
            const response = await api.getJournalSettings(journal.id);
            
            if (response.data?.success && response.data.data) {
                const settings = response.data.data;
                const css = settings.css || '';
                setCssCode(css);
                setOriginalCss(css);
            } else {
                setCssCode('');
                setOriginalCss('');
            }
        } catch (err) {
            console.error('Error loading journal settings:', err);
            setCssCode('');
            setOriginalCss('');
        }
    };

    const handleEditorChange = (value) => {
        setCssCode(value || '');
    };

    const handleSaveDetails = async () => {
        if (!journal) return;

        setSaving(true);
        try {
            const api = Journals(session);
            
            // Save title if changed
            if (title !== originalTitle) {
                await api.renameJournal(journal.id, title);
                setOriginalTitle(title);
            }

            if (onSaved) {
                onSaved({ ...journal, title }, cssCode);
            }
        } catch (err) {
            console.error('Error saving journal title:', err);
        } finally {
            setSaving(false);
        }
    };

    const handleSaveCSS = async () => {
        if (!journal) return;

        setSaving(true);
        try {
            const api = Journals(session);
            
            // Save CSS if changed
            if (cssCode !== originalCss) {
                await api.updateJournalSettings(journal.id, { css: cssCode });
                setOriginalCss(cssCode);
            }

            if (onSaved) {
                onSaved({ ...journal, settings:{...journal.settings, css:cssCode}, title }, cssCode);
            }
        } catch (err) {
            console.error('Error saving journal CSS:', err);
        } finally {
            setSaving(false);
        }
    };

    const handleCancelDetails = () => {
        setTitle(originalTitle);
    };

    const handleCancelCSS = () => {
        setCssCode(originalCss);
    };

    if (!journal) return null;

    const hasDetailsChanges = title !== originalTitle;
    const hasCSSChanges = cssCode !== originalCss;

    return (
        <Modal onClose={onClose} title="Journal Settings" className={"modal-for-settings selected-tab-" + selectedTab}>
            <div className="journal-settings-modal">
                <Tabs tabs={['Details', 'Chapters', 'CSS']} selectedIndex={0} onChange={setSelectedTab}>
                    {/* Details Tab */}
                    <div className="tab-details">
                        <div className="form-group">
                            <label htmlFor="journal-title">Journal Title</label>
                            <Input
                                id="journal-title"
                                type="text"
                                value={title}
                                onChange={(e) => setTitle(e.target.value)}
                                placeholder="Enter journal title"
                            />
                        </div>
                        {hasDetailsChanges && (
                            <div className="buttons">
                                <button onClick={handleCancelDetails} className="cancel" disabled={saving}>
                                    Cancel
                                </button>
                                <button onClick={handleSaveDetails} disabled={saving}>
                                    {saving ? (
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

                    {/* Chapters Tab */}
                    <div className="tab-chapters">
                        <JournalChapters journalId={journal?.id} />
                    </div>

                    {/* CSS Tab */}
                    <div className="tab-css">
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
                        {hasCSSChanges && (
                            <div className="buttons">
                                <button onClick={handleCancelCSS} className="cancel" disabled={saving}>
                                    Cancel
                                </button>
                                <button onClick={handleSaveCSS} disabled={saving}>
                                    {saving ? (
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
            </div>
        </Modal>
    );
}
