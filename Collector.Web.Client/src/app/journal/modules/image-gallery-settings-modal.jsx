import { useState } from 'react';
//components
import Modal from '@/components/ui/modal';
import Input from '@/components/forms/input';
import Select from '@/components/forms/select';

const galleryTypeOptions = [
    { label: 'Carousel', value: '0' },
    { label: 'Grid', value: '1' }
];

const thumbnailSizeOptions = [
    { label: '1:1', value: '1:1' },
    { label: '3:4', value: '3:4' },
    { label: '4:5', value: '4:5' },
    { label: '16:9', value: '16:9' },
    { label: '4:3', value: '4:3' },
    { label: '5:4', value: '5:4' },
    { label: '9:16', value: '9:16' }
];

export default function ImageGallerySettingsModal({
    thumbnailsPerRow = 6,
    minCount = 1,
    maxCount = 12,
    galleryType = 0,
    thumbnailSize = '1:1',
    onSave,
    onClose
}) {
    const [value, setValue] = useState(thumbnailsPerRow);
    const [selectedGalleryType, setSelectedGalleryType] = useState(String(galleryType ?? 0));
    const [selectedThumbnailSize, setSelectedThumbnailSize] = useState(thumbnailSize || '1:1');

    const handleValueChange = (event) => {
        const parsed = parseInt(event.target.value, 10);
        if (isNaN(parsed)) {
            setValue('');
            return;
        }
        setValue(parsed);
    };

    const handleGalleryTypeChange = (event) => {
        setSelectedGalleryType(event.target.value);
    };

    const handleThumbnailSizeChange = (event) => {
        setSelectedThumbnailSize(event.target.value);
    };

    const handleSave = () => {
        if (typeof onSave === 'function') {
            const parsed = parseInt(value, 10);
            const normalized = isNaN(parsed)
                ? thumbnailsPerRow
                : Math.min(maxCount, Math.max(minCount, parsed));

            onSave({
                thumbnailsPerRow: normalized,
                galleryType: parseInt(selectedGalleryType, 10) || 0,
                thumbnailSize: selectedThumbnailSize
            });
        }
        if (onClose) onClose();
    };

    const handleCancel = () => {
        if (onClose) onClose();
    };

    const isGridType = selectedGalleryType === '1';

    return (
        <Modal title="Image Gallery Settings" onClose={handleCancel}>
            <Input
                label="Thumbnails per row"
                name="image-gallery-thumbnails"
                type="number"
                min={minCount}
                max={maxCount}
                value={value}
                onChange={handleValueChange}
            />
            <Select
                label="Gallery Type"
                name="image-gallery-type"
                options={galleryTypeOptions}
                value={selectedGalleryType}
                onChange={handleGalleryTypeChange}
            />
            {isGridType && (
                <Select
                    label="Thumbnail Size"
                    name="image-gallery-thumbnail-size"
                    options={thumbnailSizeOptions}
                    value={selectedThumbnailSize}
                    onChange={handleThumbnailSizeChange}
                />
            )}
            <div className="buttons">
                <button onClick={handleSave}>Save</button>
                <button className="cancel" onClick={handleCancel}>Cancel</button>
            </div>
        </Modal>
    );
}
