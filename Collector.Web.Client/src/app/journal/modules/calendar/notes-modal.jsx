import { useState } from 'react';
import Modal from '@/components/ui/modal';
import Input from '@/components/forms/input';

export default function NotesModal({ dateString, initialNote = '', onSave, onClose }) {
    const [noteText, setNoteText] = useState(initialNote);

    const handleSave = () => {
        onSave(dateString, noteText);
        onClose();
    };

    return (
        <Modal title="Edit Day Note" onClose={onClose}>
            <Input
                name="day-note"
                label="Note"
                value={noteText}
                onInput={(e) => setNoteText(e.target.value)}
                placeholder="Enter a note for this day"
                style={{ width: '100%' }}
            />
            <div className="buttons">
                <button className="cancel" onClick={onClose}>Cancel</button>
                <button onClick={handleSave}>Save</button>
            </div>
        </Modal>
    );
}
