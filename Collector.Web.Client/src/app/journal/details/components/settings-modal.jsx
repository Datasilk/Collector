import React, { useState, useEffect } from 'react';
import Modal from '@/components/ui/modal';
import Input from '@/components/forms/input';
import Editor from '@monaco-editor/react';
import Icon from '@/components/ui/icon';
import { useSession } from '@/context/session';
import { Journals } from '@/api/user/journals';

export default function JournalSettingsModal({ journal, onClose, onSaved }) {
    const session = useSession();
    const [title, setTitle] = useState('');
    const [cssCode, setCssCode] = useState('');
    const [originalCss, setOriginalCss] = useState('');
    const [saving, setSaving] = useState(false);
    const [theme] = useState('vs-dark');

    useEffect(() => {
        if (journal) {
            setTitle(journal.title || '');
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

    const handleSave = async () => {
        if (!journal) return;

        setSaving(true);
        try {
            const api = Journals(session);
            
            // Save title if changed
            if (title !== journal.title) {
                await api.renameJournal(journal.id, title);
            }

            // Save CSS if changed
            if (cssCode !== originalCss) {
                await api.updateJournalSettings(journal.id, { css: cssCode });
            }

            if (onSaved) {
                onSaved({ ...journal, settings:{...journal.settings, css:cssCode}, title }, cssCode);
            }
            onClose();
        } catch (err) {
            console.error('Error saving journal settings:', err);
        } finally {
            setSaving(false);
        }
    };

    const handleCancel = () => {
        setTitle(journal?.title || '');
        setCssCode(originalCss);
        onClose();
    };

    if (!journal) return null;

    return (
        <Modal onClose={handleCancel} title="Journal Settings" className="modal-for-settings">
            <div className="journal-settings-modal">
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

                <div className="buttons">
                    <button onClick={handleCancel} className="cancel" disabled={saving}>
                        Cancel
                    </button>
                    <button onClick={handleSave} disabled={saving}>
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
            </div>
        </Modal>
    );
}
