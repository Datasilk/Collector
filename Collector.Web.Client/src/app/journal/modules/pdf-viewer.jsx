import { useState, useRef, useEffect, useCallback } from 'react';
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
    const [numPages, setNumPages] = useState(0);
    const [currentPage, setCurrentPage] = useState(1);
    const [pageInput, setPageInput] = useState('1');
    const [zoom, setZoom] = useState(1);
    const [viewportSize, setViewportSize] = useState({ width: 0, height: 0 });
    const [pageDimensions, setPageDimensions] = useState(null);

    const MIN_ZOOM = 0.5;
    const MAX_ZOOM = 5;
    const ZOOM_STEP = 0.25;

    //refs
    const fileInputRef = useRef(null);
    const moduleRef = useRef(module);
    const viewportRef = useRef(null);
    const pointerStartRef = useRef({ x: 0, y: 0 });
    const panStartRef = useRef({ x: 0, y: 0 });
    const pointerIdRef = useRef(null);
    const panOffsetRef = useRef({ x: 0, y: 0 });
    const rafRef = useRef(null);
    const isPanningRef = useRef(false);
    const pageCanvasRef = useRef(null);
    const textLayerRef = useRef(null);
    const annotationLayerRef = useRef(null);
    const controlsRef = useRef(null);

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

    const applyPanTransforms = useCallback(() => {
        const targets = [pageCanvasRef.current, textLayerRef.current, annotationLayerRef.current];
        const { x, y } = panOffsetRef.current;
        targets.forEach((target) => {
            if (!target) return;
            target.style.transform = `translate3d(${x}px, ${y}px, 0)`;
            target.style.transformOrigin = 'center center';
        });
    }, []);

    const schedulePanRender = useCallback(() => {
        if (rafRef.current) cancelAnimationFrame(rafRef.current);
        rafRef.current = requestAnimationFrame(() => {
            rafRef.current = null;
            applyPanTransforms();
        });
    }, [applyPanTransforms]);

    useEffect(() => {
        schedulePanRender();
        return () => {
            if (rafRef.current) cancelAnimationFrame(rafRef.current);
        };
    }, [schedulePanRender]);

    useEffect(() => {
        if (!viewportRef.current || typeof ResizeObserver === 'undefined') return;
        const observer = new ResizeObserver((entries) => {
            for (const entry of entries) {
                const { width = 0, height = 0 } = entry.contentRect || {};
                if (width > 0 && height > 0) {
                    setViewportSize(prev => {
                        if (Math.abs(prev.width - width) < 0.5 && Math.abs(prev.height - height) < 0.5) {
                            return prev;
                        }
                        return { width, height };
                    });
                }
            }
        });
        observer.observe(viewportRef.current);
        return () => observer.disconnect();
    }, []);

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

    const processPdfFile = async (file, { fromClipboard = false } = {}) => {
        if (!isEditable || !file) return;

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

                    const updatedModule = {
                        ...module,
                        filename: fileName,
                        originalFilename: file.name,
                        fileSize: file.size,
                        uploadFromClipboard: null
                    };
                    moduleRef.current = updatedModule;
                    onUpdate(updatedModule);
                } catch (error) {
                    console.error('Error saving PDF metadata:', error);
                    const updatedModule = {
                        ...module,
                        filename: fileName,
                        originalFilename: file.name,
                        fileSize: file.size,
                        uploadFromClipboard: null
                    };
                    moduleRef.current = updatedModule;
                    onUpdate(updatedModule);
                }
            } else {
                setUploadError(response.data.message || 'Failed to upload PDF');
            }
        } catch (error) {
            console.error('Error uploading PDF:', error);
            setUploadError('An error occurred while uploading the PDF');
        } finally {
            setIsUploading(false);
            if (fromClipboard) {
                moduleRef.current = { ...moduleRef.current, uploadInProgress: false };
            }
        }
    };

    const handleFileChange = async (e) => {
        if (!isEditable) return;
        const file = e.target.files[0];
        if (!file) return;
        await processPdfFile(file);
    };

    useEffect(() => {
        if (!isEditable || !module.uploadFromClipboard || moduleRef.current?.uploadInProgress) return;
        if (typeof window === 'undefined') return;

        const clipboardFiles = window.clipboardFileBuffer || {};
        const file = clipboardFiles[module.id];

        if (!file) {
            onUpdate({ ...module, uploadFromClipboard: null });
            return;
        }

        moduleRef.current = { ...module, uploadInProgress: true };
        processPdfFile(file, { fromClipboard: true }).finally(() => {
            if (window.clipboardFileBuffer) {
                delete window.clipboardFileBuffer[module.id];
            }
            moduleRef.current = { ...moduleRef.current, uploadInProgress: false };
        });
    }, [isEditable, module.uploadFromClipboard, module.id]);

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

    const getScaledDimensions = () => {
        const baseWidth = viewportSize.width || pageDimensions?.width || 0;
        if (!baseWidth) {
            return { baseWidth: 0, baseHeight: 0, scaledWidth: 0, scaledHeight: 0 }; 
        }
        const aspectRatio = pageDimensions?.width && pageDimensions?.height
            ? (pageDimensions.height / pageDimensions.width)
            : (viewportSize.height && viewportSize.width ? viewportSize.height / viewportSize.width : 11 / 8.5);
        const baseHeight = baseWidth * aspectRatio;
        return {
            baseWidth,
            baseHeight,
            scaledWidth: baseWidth * zoom,
            scaledHeight: baseHeight * zoom
        };
    };

    const clampPanOffset = (offset) => {
        const { scaledWidth, scaledHeight } = getScaledDimensions();
        const maxX = scaledWidth > viewportSize.width ? (scaledWidth - viewportSize.width) / 2 : 0;
        const maxY = scaledHeight > viewportSize.height ? (scaledHeight - viewportSize.height) / 2 : 0;
        const clamped = {
            x: Math.min(maxX, Math.max(-maxX, offset.x)),
            y: Math.min(maxY, Math.max(-maxY, offset.y))
        };
        panOffsetRef.current = clamped;
        schedulePanRender();
        return clamped;
    };

    useEffect(() => {
        clampPanOffset(panOffsetRef.current);
    }, [zoom, viewportSize.width, viewportSize.height, pageDimensions?.width, pageDimensions?.height]);

    useEffect(() => {
        panOffsetRef.current = { x: 0, y: 0 };
        schedulePanRender();
    }, [currentPage, schedulePanRender]);

    useEffect(() => {
        const controlsEl = controlsRef.current;
        const viewportEl = viewportRef.current;
        if (!controlsEl || !viewportEl) return;

        const baseRight = 12;
        const minViewportTop = 12 * 16; // 20em in px

        const updatePosition = (onceMore) => {
            const viewportRect = viewportEl.getBoundingClientRect();
            const controlsHeight = controlsEl.offsetHeight || 0;
            const shouldStick = viewportRect.top < minViewportTop && (viewportRect.bottom - controlsHeight) > minViewportTop;

            if (shouldStick) {
                controlsEl.style.position = 'fixed';
                controlsEl.style.top = `${minViewportTop}px`;
                controlsEl.style.right = `${Math.max(baseRight, window.innerWidth - viewportRect.right + baseRight)}px`;
            } else {
                controlsEl.style.position = 'absolute';
                controlsEl.style.top = '0.75em';
                controlsEl.style.right = '0.75em';
            }
            if(onceMore !== true){
                setTimeout(() => updatePosition(true), 2000);
            }
        };

        updatePosition();
        window.addEventListener('scroll', updatePosition, { passive: true });
        window.addEventListener('resize', updatePosition);
        return () => {
            window.removeEventListener('scroll', updatePosition);
            window.removeEventListener('resize', updatePosition);
        };
    }, [journalId, entryId, module.filename]);

    const setPanningActive = (isActive) => {
        const viewportEl = viewportRef.current;
        isPanningRef.current = isActive;
        if (viewportEl) {
            viewportEl.classList.toggle('is-panning', Boolean(isActive));
        }
    };

    const handlePointerDown = (event) => {
        const isPrimaryButton = event.button === undefined || event.button === 0;
        if (!isPrimaryButton) return;
        if (event.target.closest('.pdf-controls-overlay')) return;
        const { scaledWidth, scaledHeight } = getScaledDimensions();
        const canPanX = scaledWidth > viewportSize.width + 1;
        const canPanY = scaledHeight > viewportSize.height + 1;
        if (!canPanX && !canPanY) return;
        event.preventDefault();
        pointerIdRef.current = event.pointerId;
        pointerStartRef.current = { x: event.clientX, y: event.clientY };
        panStartRef.current = { ...panOffsetRef.current };
        setPanningActive(true);
        viewportRef.current?.setPointerCapture?.(event.pointerId);
    };

    const handlePointerMove = (event) => {
        if (!isPanningRef.current) return;
        event.preventDefault();
        const deltaX = event.clientX - pointerStartRef.current.x;
        const deltaY = event.clientY - pointerStartRef.current.y;
        clampPanOffset({
            x: panStartRef.current.x + deltaX,
            y: panStartRef.current.y + deltaY
        });
    };

    const endPan = (event) => {
        if (!isPanningRef.current) return;
        if (event) {
            viewportRef.current?.releasePointerCapture?.(event.pointerId ?? pointerIdRef.current);
        }
        setPanningActive(false);
    };

    const loadingIndicator = (
        <div className="pdf-loading-message">
            <Icon name="progress_activity" spin={true} />
            <span>Loading PDF...</span>
        </div>
    );

    const { baseWidth, scaledWidth, scaledHeight } = getScaledDimensions();
    const pageWidth = scaledWidth || undefined;
    const canPan = Boolean(
        (scaledWidth && viewportSize.width && scaledWidth > viewportSize.width + 1) ||
        (scaledHeight && viewportSize.height && scaledHeight > viewportSize.height + 1)
    );

    useEffect(() => {
        const viewportEl = viewportRef.current;
        if (!viewportEl) return;
        viewportEl.classList.toggle('can-pan', canPan);
        if (!canPan) {
            setPanningActive(false);
            panOffsetRef.current = { x: 0, y: 0 };
            schedulePanRender();
        }
    }, [canPan, schedulePanRender]);

    return (
        <div className="pdf-viewer-module">
            {isEditable && module.uploadFromClipboard && (
                <div className="uploading-message">
                    <Icon name="progress_activity" spin={true} />
                    <span>Uploading PDF…</span>
                </div>
            )}

            {isEditable && !module.filename && !module.uploadFromClipboard && (
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
                        <div className="pdf-page-viewport">
                            <div
                                className="pdf-page-viewport-content"
                                ref={viewportRef}
                                onPointerDown={handlePointerDown}
                                onPointerMove={handlePointerMove}
                                onPointerUp={endPan}
                                onPointerLeave={endPan}
                                style={{ touchAction: canPan ? 'none' : 'auto' }}
                            >
                                <Document
                                    file={`${apiBasePath()}/file/${journalId}/${entryId}/${module.filename}`}
                                    onLoadSuccess={({ numPages: totalPages }) => {
                                        setIsRendering(false);
                                        setNumPages(totalPages);
                                        setCurrentPage(prev => {
                                            const nextPage = Math.min(prev, totalPages) || 1;
                                            setPageInput(String(nextPage));
                                            return nextPage;
                                        });
                                        setZoom(1);
                                        panOffsetRef.current = { x: 0, y: 0 };
                                        schedulePanRender();
                                    }}
                                    onLoadProgress={() => setIsRendering(true)}
                                    onLoadError={(err) => {
                                        console.error('Error loading PDF:', err);
                                        setIsRendering(false);
                                    }}
                                    loading={loadingIndicator}
                                >
                                    {numPages > 0 && (
                                        <div className="pdf-controls-overlay" ref={controlsRef}>
                                            <div className="zoom-buttons">
                                                <button
                                                    className="icon"
                                                    type="button"
                                                    onClick={() => setZoom(prev => Math.max(MIN_ZOOM, +(prev - ZOOM_STEP).toFixed(2)))}
                                                    disabled={zoom <= MIN_ZOOM}
                                                    title="Zoom out"
                                                >
                                                    <Icon name="zoom_out" />
                                                </button>
                                                <span className="zoom-label">{Math.round(zoom * 100)}%</span>
                                                <button
                                                    className="icon"
                                                    type="button"
                                                    onClick={() => setZoom(prev => Math.min(MAX_ZOOM, +(prev + ZOOM_STEP).toFixed(2)))}
                                                    disabled={zoom >= MAX_ZOOM}
                                                    title="Zoom in"
                                                >
                                                    <Icon name="zoom_in" />
                                                </button>
                                            </div>
                                            <div className="page-indicator">
                                                <Icon name="description" />
                                                <input
                                                    type="number"
                                                    min={1}
                                                    max={numPages}
                                                    value={pageInput}
                                                    onChange={(e) => {
                                                        const value = e.target.value;
                                                        setPageInput(value);
                                                    }}
                                                    onBlur={() => {
                                                        const parsed = parseInt(pageInput, 10);
                                                        if (!parsed || parsed < 1) {
                                                            setPageInput(String(currentPage));
                                                            return;
                                                        }
                                                        const clamped = Math.min(numPages, Math.max(1, parsed));
                                                        setCurrentPage(clamped);
                                                        setPageInput(String(clamped));
                                                    }}
                                                    onKeyDown={(e) => {
                                                        if (e.key === 'Enter') {
                                                            e.target.blur();
                                                        }
                                                    }}
                                                />
                                                <span className="page-total">/ {numPages}</span>
                                            </div>
                                            <div className="page-buttons">
                                                <button
                                                    className="icon"
                                                    onClick={() => {
                                                        setCurrentPage(prev => {
                                                            const next = Math.max(1, prev - 1);
                                                            setPageInput(String(next));
                                                            return next;
                                                        });
                                                    }}
                                                    disabled={currentPage <= 1}
                                                    title="Previous page"
                                                >
                                                    <Icon name="chevron_left" />
                                                </button>
                                                <button
                                                    className="icon"
                                                    onClick={() => {
                                                        setCurrentPage(prev => {
                                                            const next = Math.min(numPages, prev + 1);
                                                            setPageInput(String(next));
                                                            return next;
                                                        });
                                                    }}
                                                    disabled={currentPage >= numPages}
                                                    title="Next page"
                                                >
                                                    <Icon name="chevron_right" />
                                                </button>
                                            </div>
                                        </div>
                                    )}
                                    {isRendering && (
                                        <div className="loading-message">
                                            <Icon name="progress_activity" spin={true} />
                                            <span>Loading PDF...</span>
                                        </div>
                                    )}
                                    <Page
                                        pageNumber={currentPage}
                                        width={pageWidth}
                                        canvasRef={pageCanvasRef}
                                        textLayerRef={textLayerRef}
                                        annotationLayerRef={annotationLayerRef}
                                        onLoadSuccess={(page) => {
                                            const width = page.originalWidth || page.width;
                                            const height = page.originalHeight || page.height;
                                            setPageDimensions({ width, height });
                                                clampPanOffset(panOffsetRef.current);
                                        }}
                                    />
                                </Document>
                            </div>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
