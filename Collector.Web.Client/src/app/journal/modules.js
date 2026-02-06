
import CKEditorModule from './modules/ckeditor';
import ChecklistModule from './modules/checklist';
import ImageModule from './modules/image';
import ImageGalleryModule from './modules/image-gallery';
import TabsModule from './modules/tabs';
import ModuleListModule from './modules/module-list';
import VideoPlayerModule from './modules/video-player';
import FileDownloadModule from './modules/file-download';
import PdfViewerModule from './modules/pdf-viewer';
import EntriesListModule from './modules/entries-list';
import CustomModule from './modules/custom';
import GenerateContentModule from './modules/generate-content';

const modules = [
    {
        id: 'entries-list',
        type: 'entries-list',
        name: 'Entries List',
        icon: 'list',
        module: EntriesListModule,
        showTab: false
    },
    {
        id: 'text-editor',
        name: 'Text Editor',
        icon: 'text_snippet',
        type: 'text-editor',
        module: CKEditorModule
    },
    {
        id: 'image',
        name: 'Image',
        icon: 'image',
        type: 'image',
        module: ImageModule
    },
    {
        id: 'image-gallery',
        name: 'Image Gallery',
        icon: 'collections',
        type: 'image-gallery',
        module: ImageGalleryModule
    },
    {
        id: 'tabs',
        name: 'Tabs',
        icon: 'tab',
        type: 'tabs',
        module: TabsModule
    },
    {
        id: 'video-player',
        name: 'Video Player',
        icon: 'play_circle',
        type: 'video-player',
        module: VideoPlayerModule
    },
    {
        id: 'module-list',
        name: 'Module List',
        icon: 'list',
        type: 'module-list',
        module: ModuleListModule
    },
    {
        id: 'checklist',
        name: 'Checklist',
        icon: 'checklist',
        type: 'checklist',
        module: ChecklistModule
    },
    {
        id: 'file-download',
        name: 'File Download',
        icon: 'download',
        type: 'file-download',
        module: FileDownloadModule
    },
    {
        id: 'pdf-viewer',
        name: 'PDF Viewer',
        icon: 'picture_as_pdf',
        type: 'pdf-viewer',
        module: PdfViewerModule
    },
    {
        id: 'custom',
        name: 'Custom Module',
        icon: 'extension',
        type: 'custom',
        module: CustomModule
    },
    {
        id: 'generate-content',
        name: 'Generate Content',
        icon: 'auto_awesome',
        type: 'generate-content',
        module: GenerateContentModule
    }
];

export default modules; 