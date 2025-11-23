import { useState, useEffect } from 'react';
// components
import Modal from '@/components/ui/modal';
import Select from '@/components/forms/select';
import Checkbox from '@/components/forms/checkbox';
import Input from '@/components/forms/input';
import NewEntryTag from '@/app/journal/components/new-entry-tag';
import TagsList from '@/app/journal/components/tags-list';
// context
import { useSession } from '@/context/session';
// api
import { Journals } from '@/api/user/journals';
import { JournalTags } from '@/api/user/journal-tags';

export default function EntriesListSettingsModal({
    journalId,
    entryId,
    module,
    defaultViewType,
    defaultColumns,
    onSaved,
    defaultTotal
}) {
    const [viewType, setViewType] = useState(defaultViewType || 'details');
    const [columns, setColumns] = useState(defaultColumns || {
        created: true,
        modified: true,
        status: true,
        chapter: true
    });
    const [entriesPerPage, setEntriesPerPage] = useState(defaultTotal ? defaultTotal : 20);
    const [isSaving, setIsSaving] = useState(false);
    const [moduleTags, setModuleTags] = useState([]);

    const session = useSession();

    useEffect(() => {
        if (!journalId) return;

        const loadModuleTags = async () => {
            try {
                const api = JournalTags(session);
                const response = await api.getTags(journalId);
                if (response.data?.success && response.data.data) {
                    const allTags = response.data.data || [];
                    const selectedIds = Array.isArray(module?.tags)
                        ? module.tags.filter(id => id != null)
                        : [];

                    if (selectedIds.length === 0) {
                        setModuleTags([]);
                        return;
                    }

                    const filteredTags = allTags
                        .filter(t => selectedIds.includes(t.id))
                        .map(t => ({
                            tagId: t.id,
                            name: t.tag
                        }));

                    setModuleTags(filteredTags);
                }
            } catch (err) {
                console.error('Error loading journal tags for entry list settings:', err);
            }
        };

        loadModuleTags();
    }, [journalId, module, session]);

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
            const parsedEntriesPerPage = entriesPerPage ? parseInt(entriesPerPage, 10) : null;
            const entryListSettings = {
                viewType: viewType,
                columns: columns,
                entriesPerPage: parsedEntriesPerPage
            };
            await api.updateEntryListSettings(journalId, entryListSettings);

            const tagIds = Array.isArray(moduleTags)
                ? moduleTags
                    .filter(t => t && t.tagId != null)
                    .map(t => t.tagId)
                : [];

            if (onSaved) {
                onSaved(viewType, columns, parsedEntriesPerPage, tagIds);
            }

            session.hideModal();
        } catch (err) {
            console.error('Error saving entry list settings:', err);
        } finally {
            setIsSaving(false);
        }
    };

    const handleTagsChanged = (tag) => {
        if (!tag || tag.id == null) return;

        // Avoid duplicates in local tag object list
        setModuleTags(prev => {
            const exists = prev.some(t => t.tagId === tag.id);
            if (exists) return prev;
            return [...prev, { tagId: tag.id, name: tag.tag }];
        });

    };

    const handleRemoveTag = (tag) => {
        if (!tag || tag.tagId == null) return;

        setModuleTags(prev => prev.filter(t => t.tagId !== tag.tagId));
    };

    const hasValidEntryId = entryId && entryId !== 'new' && entryId !== 0;
    const entryForTags = hasValidEntryId ? { id: entryId } : null;

    return (
        <Modal title="Entry List Settings" 
        onClose={handleClose}
        className="module-entires-list-modal"
        >
            <div className="row">
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
            </div>
            <div className="row">
                <div className="col-2">
                    <div className="form-group tool-bar">
                        <label>Filter by tags</label>
                        <NewEntryTag
                            entry={entryForTags}
                            journalId={journalId}
                            onAddTag={handleTagsChanged}
                        />
                        <TagsList
                            tags={moduleTags}
                            onRemoveTag={handleRemoveTag}
                        />
                    </div>
                </div>
                <div className="col-2">
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
                </div>
            </div>
            <div className="buttons">
                <button onClick={handleSave}>{isSaving ? 'Saving...' : 'Save'}</button>
                <button className="cancel" onClick={handleClose}>Cancel</button>
            </div>
        </Modal>
    );
}

