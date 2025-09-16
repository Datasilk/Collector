
import CKEditorModule from './modules/ckeditor';
import ChecklistModule from './modules/checklist';

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
    }
];

export default modules; 