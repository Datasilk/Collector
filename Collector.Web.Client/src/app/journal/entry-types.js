import JournalEntryModal from './entry-types/journal-entry-modal';
import WebContentModal from './entry-types/web-content-modal';

const entryTypes = [
    {
        id: 'journal-entry',
        name: 'Journal Entry',
        icon: 'edit_note',
        modal: JournalEntryModal
    },
    {
        id: 'web-content',
        name: 'Web Content',
        icon: 'language',
        modal: WebContentModal
    }
];

export default entryTypes;
