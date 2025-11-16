import { useState, useRef, useEffect } from 'react';
//components
import Icon from '@/components/ui/icon';
//context
import { useSession } from '@/context/session';
//api
import { Files } from '@/api/user/files';
import { JournalFiles } from '@/api/user/journalFiles';
//helpers
import { apiBasePath } from '@/helpers/endpoints';
// pdf rendering
import { Document, Page, pdfjs } from 'react-pdf';
import 'react-pdf/dist/Page/AnnotationLayer.css';
import 'react-pdf/dist/Page/TextLayer.css';

// Use the locally bundled pdfjs-dist worker so the version matches pdfjs.version
// and everything stays same-origin (no CORS issues)
pdfjs.GlobalWorkerOptions.workerSrc = new URL(
    '/pdfjs/pdf.worker.min.mjs',
    import.meta.url
).toString();

export default function PdfViewerModule({ module, entryId, journalId, onUpdate, isEditable = true, manuallyAdded = false, setDeleteListener, tabButtons }) {
    //state
    const [isUploading, setIsUploading] = useState(false);
    const [uploadError, setUploadError] = useState(null);
    const [isRendering, setIsRendering] = useState(false);

    //refs
    const fileInputRef = useRef(null);
    const moduleRef = useRef(module);

    //context
    const session = useSession();
    const { upload } = Files(session);
    const { add: addFileMetadata, delete: deleteFileMetadata } = JournalFiles(session);

    useEffect(() => {
        moduleRef.current = module;
    }, [module]);

    // Auto-trigger file dialog when module is manually added
    useEffect(() => {
        if (manuallyAdded && isEditable && !module.filename && fileInputRef.current) {
            const timer = setTimeout(() => {
                fileInputRef.current.click();
            }, 100);
            return () => clearTimeout(timer);
        }
    }, [manuallyAdded, isEditable, module.filename]);

    useEffect(() => {
        if (!setDeleteListener) return;
        setDeleteListener(module, removeModule);
    }, [module.id]);

    useEffect(() => {
        if (!tabButtons || !module.filename) return;
        const fileUrl = `${apiBasePath()}/file/${journalId}/${entryId}/${module.filename}`;
        const handleDownload = () => {
            window.open(fileUrl, '_blank');
        };
        tabButtons([
            {
                icon: 'download',
                title: "Download " + (module.originalFilename || module.filename),
                callback: handleDownload
            }
        ]);
    }, [tabButtons, module.filename, module.originalFilename, journalId, entryId]);

    const handleFileChange = async (e) => {
        if (!isEditable) return;

        const file = e.target.files[0];
        if (!file) return;

        // Only accept PDFs here
        const isPdf = (file.type === 'application/pdf') || file.name.toLowerCase().endsWith('.pdf');
        if (!isPdf) {
            setUploadError('Please select a PDF file');
            return;
        }

        setIsUploading(true);
        setUploadError(null);

        try {
            const timestamp = new Date().getTime();
            const fileName = `${timestamp}-${file.name}`;
            const path = `${journalId}/${entryId}/${fileName}`;

            const response = await upload(path, file);

            if (response.data.success) {
                try {
                    const fileMetadata = {
                        JournalId: journalId,
                        JournalEntryId: entryId,
                        ModuleId: module.id,
                        Filename: fileName,
                        FileSize: file.size,
                        DateUploaded: new Date().toISOString()
                    };

                    await addFileMetadata(fileMetadata);

                    onUpdate({
                        ...module,
                        filename: fileName,
                        originalFilename: file.name,
                        fileSize: file.size
                    });
                } catch (error) {
                    console.error('Error saving PDF metadata:', error);
                    onUpdate({
                        ...module,
                        filename: fileName,
                        originalFilename: file.name,
                        fileSize: file.size
                    });
                }
            } else {
                setUploadError(response.data.message || 'Failed to upload PDF');
            }
        } catch (error) {
            console.error('Error uploading PDF:', error);
            setUploadError('An error occurred while uploading the PDF');
        } finally {
            setIsUploading(false);
        }
    };

    const removeModule = (moduleItem) => {
        if (deleteFileMetadata) {
            deleteFileMetadata(entryId, moduleItem.id);
        }
    };

    const formatFileSize = (bytes) => {
        if (!bytes) return '0 MB';
        const mb = bytes / (1024 * 1024);
        return mb.toFixed(2) + ' MB';
    };

    return (
        <div className="pdf-viewer-module">
            {isEditable && !module.filename && (
                <div className="tool-bar">
                    <div className="left-side">
                        <div className="file-upload-button-container">
                            <button disabled={isUploading}>
                                <Icon name="upload" />
                                {isUploading ? 'Uploading...' : 'Upload PDF'}
                            </button>
                            <input
                                type="file"
                                accept="application/pdf"
                                ref={fileInputRef}
                                onChange={handleFileChange}
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

            {module.filename && (
                <div className="pdf-viewer-container">
                    <div className="pdf-canvas-wrapper">
                        <Document
                            file={`${apiBasePath()}/file/${journalId}/${entryId}/${module.filename}`}
                            onLoadSuccess={() => setIsRendering(false)}
                            onLoadProgress={() => setIsRendering(true)}
                            onLoadError={(err) => {
                                console.error('Error loading PDF:', err);
                                setIsRendering(false);
                            }}
                        >
                            {isRendering && (
                                <div className="loading-message" style={{ padding: '1em', textAlign: 'center', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5em' }}>
                                    <Icon name="progress_activity" spin={true} />
                                    Loading PDF...
                                </div>
                            )}
                            <Page pageNumber={1} />
                        </Document>
                    </div>
                </div>
            )}
        </div>
    );
}
