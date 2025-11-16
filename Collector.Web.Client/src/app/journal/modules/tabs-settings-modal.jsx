import { useState } from 'react';
//components
import Modal from '@/components/ui/modal';
import Select from '@/components/forms/select';
//context
import { useSession } from '@/context/session';

export default function TabsSettingsModal({ module, onUpdate }) {
    const session = useSession();
    const [styleValue, setStyleValue] = useState(module.style ?? 0);

    const handleClose = () => {
        session.hideModal();
    };

    const handleStyleChange = (e) => {
        const value = parseInt(e.target.value, 10);
        setStyleValue(isNaN(value) ? 0 : value);
    };

    const handleSave = () => {
        const updatedModule = {
            ...module,
            style: styleValue
        };

        if (onUpdate) {
            onUpdate(updatedModule);
        }

        session.hideModal();
    };

    return (
        <Modal title="Tabs Settings" onClose={handleClose}>
            <Select
                label="Style"
                name="tabs-style"
                value={styleValue}
                options={[
                    { label: 'Tabs', value: 0 },
                    { label: 'Side Menu', value: 1 }
                ]}
                onChange={handleStyleChange}
            />
            <div className="buttons">
                <button onClick={handleSave}>Save</button>
                <button className="cancel" onClick={handleClose}>Cancel</button>
            </div>
        </Modal>
    );
}
