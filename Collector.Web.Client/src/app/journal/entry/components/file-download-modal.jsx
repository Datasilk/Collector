import React, { useEffect, useState } from 'react';
import Modal from '@/components/ui/modal';
import Select from '@/components/forms/select';
import Input from '@/components/forms/input';
import Checkbox from '@/components/forms/checkbox';

export default function FileDownloadModal({ module, onUpdate, onClose }) {
    const [tempStyle, setTempStyle] = useState(() => module.style || 'button outline');
    const [tempLabel, setTempLabel] = useState(() => module.label || module.originalFilename || module.filename || '');
    const [tempShowFileSize, setTempShowFileSize] = useState(() => module.showFileSize !== undefined ? module.showFileSize : true);

    useEffect(() => {
        // Update state when module prop changes
        setTempStyle(module.style || 'button outline');
        setTempLabel(module.label || module.originalFilename || module.filename || '');
        setTempShowFileSize(module.showFileSize !== undefined ? module.showFileSize : true);
    }, [module.id, module.style, module.label, module.originalFilename, module.filename, module.showFileSize]);

    const handleSave = () => {
        onUpdate({
            ...module,
            style: tempStyle,
            label: tempLabel,
            showFileSize: tempShowFileSize
        });
        onClose();
    };

    return (
        <Modal title="File Download Settings" onClose={onClose}>
            <div className="left pad-sm">
                <Input
                    id="label"
                    label="Label"
                    name="label"
                    type="text"
                    className="width-100"
                    defaultValue={tempLabel}
                    onInput={(e) => setTempLabel(e.target.value)}
                    placeholder="Enter button label"
                />
            </div>
            <div className="left pad-sm">
                <Select
                    id="button-style"
                    label="Button Style"
                    name="button-style"
                    value={tempStyle}
                    onChange={(e) => setTempStyle(e.target.value)}
                    options={[
                        { label: 'Button', value: 'button' },
                        { label: 'Button Outline', value: 'button outline' }
                    ]}
                />
            </div>
            <div className="left pad-sm">
                <Checkbox
                    id="show-file-size"
                    label="Show file size in button label"
                    name="show-file-size"
                    checked={tempShowFileSize}
                    onChange={(checked) => setTempShowFileSize(checked)}
                />
            </div>
            <div className="buttons">
                <button onClick={handleSave}>Save Changes</button>
                <button className="cancel" onClick={onClose}>Cancel</button>
            </div>
        </Modal>
    );
}
