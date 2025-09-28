import { useState, useRef, useEffect } from 'react';
//components
import Icon from '@/components/ui/icon';
//context
import { useSession } from '@/context/session';
//api
import { Images } from '@/api/user/images';
//helpers
import { apiBasePath } from '@/helpers/endpoints.js';

export default function ImageModule({ module, entryId, onUpdate, isEditable = true, manuallyAdded = false }) {
    //state
    const [isUploading, setIsUploading] = useState(false);
    const [uploadError, setUploadError] = useState(null);
    
    //refs
    const fileInputRef = useRef(null);
    
    //context
    const session = useSession();
    const { upload } = Images(session);
    
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
                // Update the module with the image path
                onUpdate({ ...module, image: fileName });
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
    
    return (
        <div className="image-module">
            {isEditable && !module.image && (
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
            
            {uploadError && (
                <div className="error-message" style={{ color: 'red', marginTop: '0.5em' }}>
                    {uploadError}
                </div>
            )}
            
            {module.image && (
                <div className="image-preview">
                    <img 
                        src={apiBasePath() + `/image/journal-entries/${entryId}/${module.image}`} 
                        alt="Uploaded content" 
                    />
                </div>
            )}
        </div>
    );
}
