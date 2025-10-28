
import CKEditorModule from './modules/ckeditor';
import ChecklistModule from './modules/checklist';
import ImageModule from './modules/image';
import TabsModule from './modules/tabs';
import ModuleListModule from './modules/module-list';
import VideoPlayerModule from './modules/video-player';

const modules = [
    {
        id: 'text-editor',
        name: 'Text Editor',
        icon: 'text_snippet',
        type: 'text-editor',
        module: CKEditorModule
    },
    {
        id: 'checklist',
        name: 'Checklist',
        icon: 'checklist',
        type: 'checklist',
        module: ChecklistModule
    },
    {
        id: 'image',
        name: 'Image',
        icon: 'image',
        type: 'image',
        module: ImageModule
    },
    {
        id: 'tabs',
        name: 'Tabs',
        icon: 'tab',
        type: 'tabs',
        module: TabsModule
    },
    {
        id: 'module-list',
        name: 'Module List',
        icon: 'list',
        type: 'module-list',
        module: ModuleListModule
    },
    {
        id: 'video-player',
        name: 'Video Player',
        icon: 'play_circle',
        type: 'video-player',
        module: VideoPlayerModule
    }
];

export default modules; 