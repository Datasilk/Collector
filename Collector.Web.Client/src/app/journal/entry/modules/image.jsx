import { useState, useRef, useEffect } from 'react';
//components
import Icon from '@/components/ui/icon';
//context
import { useSession } from '@/context/session';
//api
import { Images } from '@/api/user/images';
import { JournalImages } from '@/api/user/journalImages';
//helpers
import { apiBasePath } from '@/helpers/endpoints.js';

export default function ImageModule({ module, entryId, journalId, onUpdate, isEditable = true, manuallyAdded = false, setDeleteListener }) {
    //state
    const [isUploading, setIsUploading] = useState(false);
    const [isLoadingImage, setIsLoadingImage] = useState(false);
    const [uploadError, setUploadError] = useState(null);
    
    //refs
    const fileInputRef = useRef(null);
    
    //context
    const session = useSession();
    const { upload } = Images(session);
    const { add: addImageMetadata, delete: deleteImageMetadata } = JournalImages(session);
    
    // Auto-trigger file dialog when module is manually added
    useEffect(() => {
        if (manuallyAdded && isEditable && fileInputRef.current) {
            // Small delay to ensure the DOM is fully rendered
            const timer = setTimeout(() => {
                fileInputRef.current.click();
            }, 100);
            
            return () => clearTimeout(timer);
        }
    }, [manuallyAdded, isEditable]);

    useEffect(() => {
        if (!setDeleteListener) return;
        setDeleteListener(module, removeModule);
    }, [module.id]);
    
    const handleFileChange = async (e) => {
        if (!isEditable) return;
        
        const file = e.target.files[0];
        if (!file) return;
        
        // Check if file is an image
        if (!file.type.startsWith('image/')) {
            setUploadError('Please select an image file');
            return;
        }
        
        setIsUploading(true);
        setUploadError(null);
        
        try {
            // Generate a unique filename using timestamp and original filename
            const timestamp = new Date().getTime();
            const fileExtension = file.name.split('.').pop();
            const fileName = `${timestamp}-${module.id}.${fileExtension}`;
            
            // Path format: journal-entries/{entryId}/{moduleId}.{extension}
            const path = `journal-entries/${entryId}/${fileName}`;
            
            const response = await upload(path, file);
            
            if (response.data.success) {
                setIsLoadingImage(true);
                // Get image dimensions
                const img = new Image();
                img.onload = async () => {
                    try {
                        // Save image metadata to JournalImages table
                        const imageMetadata = {
                            JournalId: journalId,
                            JournalEntryId: entryId,
                            ModuleId: module.id,
                            FileName: fileName,
                            Width: img.width,
                            Height: img.height
                        };
                        
                        await addImageMetadata(imageMetadata);
                        
                        // Update the module with the image path
                        onUpdate({ ...module, image: fileName });
                        setIsLoadingImage(false);
                    } catch (error) {
                        console.error('Error saving image metadata:', error);
                        // Still update the module even if metadata save fails
                        onUpdate({ ...module, image: fileName });
                        setIsLoadingImage(false);
                    }
                };
                img.onerror = () => {
                    // If image fails to load, still update the module
                    onUpdate({ ...module, image: fileName });
                    setIsLoadingImage(false);
                };
                img.src = URL.createObjectURL(file);
            } else {
                setUploadError(response.data.message || 'Failed to upload image');
            }
        } catch (error) {
            console.error('Error uploading image:', error);
            setUploadError('An error occurred while uploading the image');
        } finally {
            setIsUploading(false);
        }
    };

    const removeModule = (moduleItem) => {
        if (deleteImageMetadata) {
            deleteImageMetadata(entryId, moduleItem.id);
        }
    };
    
    return (
        <div className="image-module">
            {isEditable && !module.image && !isLoadingImage && (
                <div className="tool-bar">
                    <div className="left-side">
                        <div className="image-upload-button-container">
                            <button disabled={isUploading}>
                                <Icon name="upload" />
                                {isUploading ? 'Uploading...' : 'Upload Image'}
                            </button>
                            <input 
                                type="file" 
                                ref={fileInputRef}
                                onChange={handleFileChange}
                                accept="image/*"
                                disabled={isUploading}
                            />
                        </div>
                    </div>
                </div>
            )}
            
            {isLoadingImage && (
                <div className="loading-message" style={{ padding: '1em', textAlign: 'center', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5em' }}>
                    <Icon name="progress_activity" spin={true} />
                    Loading image...
                </div>
            )}
            
            {uploadError && (
                <div className="error-message" style={{ color: 'red', marginTop: '0.5em' }}>
                    {uploadError}
                </div>
            )}
            
            {module.image && !isLoadingImage && (
                <div className="image-preview">
                    <img loading="lazy"
                        src={apiBasePath() + `/image/journal-entries/${entryId}/${module.image}`} 
                        alt="Uploaded content" 
                    />
                </div>
            )}
        </div>
    );
}
