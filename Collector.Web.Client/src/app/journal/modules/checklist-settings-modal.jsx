import { useState } from 'react';
//components
import Input from '@/components/forms/input';
import TextArea from '@/components/forms/textarea';
import Modal from '@/components/ui/modal';
//context
import { useSession } from '@/context/session';
//api
import { JournalChecklists } from '@/api/user/journalChecklists';

export default function ChecklistSettingsModal({ checklistId, title, description, items, module, onUpdate, onSaved }) {
    const [tempTitle, setTempTitle] = useState(title || '');
    const [tempDescription, setTempDescription] = useState(description || '');
    const [isSaving, setIsSaving] = useState(false);

    const session = useSession();
    const {
        updateChecklistTitle,
        updateChecklistDescription
    } = JournalChecklists(session);

    const handleClose = () => {
        session.hideModal();
    };

    const handleSave = async () => {
        if (!checklistId || isSaving) return;

        setIsSaving(true);
        try {
            await updateChecklistTitle(checklistId, tempTitle);
            await updateChecklistDescription(checklistId, tempDescription);

            if (onUpdate && module) {
                onUpdate({
                    ...module,
                    checklist: {
                        items: items || [],
                        title: tempTitle,
                        description: tempDescription
                    }
                });
            }

            if (onSaved) {
                onSaved(tempTitle, tempDescription);
            }

            session.hideModal();
        } catch (err) {
            console.error('Error saving checklist settings:', err);
        } finally {
            setIsSaving(false);
        }
    };

    const handleTitleChange = (e) => {
        setTempTitle(e.target.value);
    };

    const handleDescriptionInput = (e) => {
        setTempDescription(e.target.value);
    };

    return (
        <Modal title="Checklist Settings" onClose={handleClose}>
            <Input
                label="Title"
                name="checklist-title"
                value={tempTitle}
                onChange={handleTitleChange}
                placeholder="Enter checklist title"
            />
            <TextArea
                label="Description"
                name="checklist-description"
                defaultValue={tempDescription}
                onInput={handleDescriptionInput}
                placeholder="Enter checklist description"
                rows={3}
                autoResize={true}
            />
            <div className="buttons">
                <button onClick={handleSave}>{isSaving ? 'Saving...' : 'Save'}</button>
                <button className="cancel" onClick={handleClose}>Cancel</button>
            </div>
        </Modal>
    );
}
