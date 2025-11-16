import { useState } from 'react';
// components
import Modal from '@/components/ui/modal';
import Select from '@/components/forms/select';
import Checkbox from '@/components/forms/checkbox';
import Input from '@/components/forms/input';
// context
import { useSession } from '@/context/session';
// api
import { Journals } from '@/api/user/journals';

export default function EntriesListSettingsModal({ journalId, defaultViewType, defaultColumns, onSaved, defaultTotal }) {
    const [viewType, setViewType] = useState(defaultViewType || 'details');
    const [columns, setColumns] = useState(defaultColumns || {
        created: true,
        modified: true,
        status: true,
        chapter: true
    });
    const [entriesPerPage, setEntriesPerPage] = useState(defaultTotal ? defaultTotal : 20);
    const [isSaving, setIsSaving] = useState(false);

    const session = useSession();

    const handleClose = () => {
        session.hideModal();
    };

    const handleColumnToggle = (columnName, checked) => {
        setColumns({
            ...columns,
            [columnName]: checked
        });
    };

    const handleSave = async () => {
        if (!journalId || isSaving) return;

        setIsSaving(true);
        try {
            const api = Journals(session);
            const entryListSettings = {
                viewType: viewType,
                columns: columns,
                entriesPerPage: entriesPerPage ? parseInt(entriesPerPage, 10) : null
            };
            await api.updateEntryListSettings(journalId, entryListSettings);

            if (onSaved) {
                onSaved(viewType, columns, entriesPerPage ? parseInt(entriesPerPage, 10) : null);
            }

            session.hideModal();
        } catch (err) {
            console.error('Error saving entry list settings:', err);
        } finally {
            setIsSaving(false);
        }
    };

    return (
        <Modal title="Entry List Settings" onClose={handleClose}>
            <div className="col-2">
                <Select
                    label="View"
                    name="view-type"
                    value={viewType}
                    onChange={(e) => setViewType(e.target.value)}
                    options={[
                        { label: 'Details', value: 'details' },
                        { label: 'Cards', value: 'cards' }
                    ]}
                />
            </div>
            <div className="col-2">
                <Input
                    label="Entries per page"
                    name="entries-per-page"
                    type="text"
                    maxLength={3}
                    style={{ width: '3em' }}
                    value={entriesPerPage}
                    onInput={(e) => {
                        const numeric = (e.target.value || '').replace(/[^0-9]/g, '');
                        setEntriesPerPage(numeric);
                    }}
                />
            </div>
            {viewType === 'details' && (
                <div className="column-settings">
                    <h4>Visible Columns</h4>
                    <Checkbox
                        label="Date Created"
                        name="column-created"
                        checked={columns.created}
                        onChange={(checked) => handleColumnToggle('created', checked)}
                    />
                    <Checkbox
                        label="Date Modified"
                        name="column-modified"
                        checked={columns.modified}
                        onChange={(checked) => handleColumnToggle('modified', checked)}
                    />
                    <Checkbox
                        label="Status"
                        name="column-status"
                        checked={columns.status}
                        onChange={(checked) => handleColumnToggle('status', checked)}
                    />
                    <Checkbox
                        label="Chapter"
                        name="column-chapter"
                        checked={columns.chapter}
                        onChange={(checked) => handleColumnToggle('chapter', checked)}
                    />
                </div>
            )}
            <div className="buttons">
                <button onClick={handleSave}>{isSaving ? 'Saving...' : 'Save'}</button>
                <button className="cancel" onClick={handleClose}>Cancel</button>
            </div>
        </Modal>
    );
}

