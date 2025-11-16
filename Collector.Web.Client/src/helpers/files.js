// helpers/files.js
// Shared file upload helpers used by journal modules (image, pdf, general files)

import { Images } from '@/api/user/images';
import { JournalImages } from '@/api/user/journalImages';
import { Files } from '@/api/user/files';
import { JournalFiles } from '@/api/user/journalFiles';

// Upload an image for a checklist/image module and save metadata
// Path format: journal-entries/{entryId}/{timestamp-moduleId.ext}
export async function uploadImageForModule(session, { imageFile, journalId, entryId, moduleId }) {
    if (!imageFile || !entryId || !moduleId) {
        return { success: false, message: 'Missing image, entryId, or moduleId' };
    }

    const imagesApi = Images(session);
    const journalImagesApi = JournalImages(session);

    try {
        const timestamp = new Date().getTime();
        const fileExtension = imageFile.name.includes('.') ? imageFile.name.split('.').pop() : 'png';
        const fileName = `${timestamp}-${moduleId}.${fileExtension}`;
        const path = `journal-entries/${entryId}/${fileName}`;

        const response = await imagesApi.upload(path, imageFile);
        if (!response.data?.success) {
            return { success: false, message: response.data?.message || 'Failed to upload image' };
        }

        // Get image dimensions
        const img = new Image();
        const imgLoaded = new Promise((resolve) => {
            img.onload = resolve;
            img.onerror = resolve;
        });
        img.src = URL.createObjectURL(imageFile);
        await imgLoaded;

        try {
            const metadata = {
                JournalId: journalId || null,
                JournalEntryId: entryId,
                ModuleId: moduleId,
                FileName: fileName,
                Width: img.width || 0,
                Height: img.height || 0
            };
            await journalImagesApi.add(metadata);
        } catch (err) {
            // Log but still treat upload as successful for the caller
            console.error('Error saving image metadata:', err);
        }

        return { success: true, fileName };
    } catch (err) {
        console.error('Error uploading image via helper:', err);
        return { success: false, message: 'Error uploading image' };
    }
}

// Upload a PDF for a module and save metadata (JournalFiles)
// Path format: {journalId}/{entryId}/{timestamp-originalName}
export async function uploadPdfForModule(session, { pdfFile, journalId, entryId, moduleId }) {
    if (!pdfFile || !journalId || !entryId || !moduleId) {
        return { success: false, message: 'Missing PDF, journalId, entryId, or moduleId' };
    }

    const filesApi = Files(session);
    const journalFilesApi = JournalFiles(session);

    try {
        const timestamp = new Date().getTime();
        const fileName = `${timestamp}-${pdfFile.name}`;
        const path = `${journalId}/${entryId}/${fileName}`;

        const response = await filesApi.upload(path, pdfFile);
        if (!response.data?.success) {
            return { success: false, message: response.data?.message || 'Failed to upload PDF' };
        }

        try {
            const metadata = {
                JournalId: journalId,
                JournalEntryId: entryId,
                ModuleId: moduleId,
                Filename: fileName,
                FileSize: pdfFile.size,
                DateUploaded: new Date().toISOString()
            };
            await journalFilesApi.add(metadata);
        } catch (err) {
            console.error('Error saving PDF metadata:', err);
        }

        return { success: true, fileName, fileSize: pdfFile.size, originalName: pdfFile.name };
    } catch (err) {
        console.error('Error uploading PDF via helper:', err);
        return { success: false, message: 'Error uploading PDF' };
    }
}

// Upload a generic file (non-image, non-PDF) for a module and save metadata (JournalFiles)
export async function uploadFileForModule(session, { file, journalId, entryId, moduleId }) {
    if (!file || !journalId || !entryId || !moduleId) {
        return { success: false, message: 'Missing file, journalId, entryId, or moduleId' };
    }

    const filesApi = Files(session);
    const journalFilesApi = JournalFiles(session);

    try {
        const timestamp = new Date().getTime();
        const fileName = `${timestamp}-${file.name}`;
        const path = `${journalId}/${entryId}/${fileName}`;

        const response = await filesApi.upload(path, file);
        if (!response.data?.success) {
            return { success: false, message: response.data?.message || 'Failed to upload file' };
        }

        try {
            const metadata = {
                JournalId: journalId,
                JournalEntryId: entryId,
                ModuleId: moduleId,
                Filename: fileName,
                FileSize: file.size,
                DateUploaded: new Date().toISOString()
            };
            await journalFilesApi.add(metadata);
        } catch (err) {
            console.error('Error saving file metadata:', err);
        }

        return { success: true, fileName, fileSize: file.size, originalName: file.name };
    } catch (err) {
        console.error('Error uploading file via helper:', err);
        return { success: false, message: 'Error uploading file' };
    }
}
