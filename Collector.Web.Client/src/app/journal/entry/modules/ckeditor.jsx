import { useState, useEffect, useRef } from 'react';
import './ckeditor.css';
import './ckeditor/ai-generator.css';
//ckeditor
import {
    InlineEditor,
    Alignment,
    Autoformat,
    Autosave,
    BlockQuote,
    Bold,
    Emoji,
    Essentials,
    FontBackgroundColor,
    FontColor,
    FontFamily,
    FontSize,
    GeneralHtmlSupport,
    Heading,
    //Highlight,
    Indent,
    IndentBlock,
    Italic,
    Link,
    List,
    ListProperties,
    Mention,
    Paragraph,
    PasteFromOffice,
    PlainTableOutput,
    RemoveFormat,
    SpecialCharacters,
    SpecialCharactersArrows,
    SpecialCharactersCurrency,
    SpecialCharactersEssentials,
    SpecialCharactersLatin,
    SpecialCharactersMathematical,
    SpecialCharactersText,
    Strikethrough,
    Style,
    Subscript,
    Superscript,
    Table,
    TableCellProperties,
    TableColumnResize,
    TableProperties,
    TableToolbar,
    TextTransformation,
    Underline,
    Plugin,
    ButtonView
} from 'ckeditor5';
import 'ckeditor5/ckeditor5.css';
import { LineHeight } from '@rickx/ckeditor5-line-height'
// AI Generator plugin
import defineAIGeneratorPlugin from './ckeditor/ai-generator';
import AIGeneratorModal from './ckeditor/ai-generator-modal';
//helpers
import { fonts } from '@/helpers/fonts';

export default function CKEditorModule({ module, onUpdate, isEditable = true }) {
    //state
    const [mounted, setMounted] = useState(false);
    const [showAIModal, setShowAIModal] = useState(false);
    const [userInput, setUserInput] = useState('');

    //refs
    const htmlRef = useRef(null);
    const editorRef = useRef(null);
    const timerSave = useRef(null);
    const inModal = useRef(false);
    const isEditorActive = useRef(false);

    //effect
    useEffect(() => {
        if (mounted) return;
        setMounted(true);

        // Add event listener for AI generator button click
        document.addEventListener('aiGeneratorRequest', handleAIGeneratorRequest);

        return () => {
            document.removeEventListener('aiGeneratorRequest', handleAIGeneratorRequest);
        };
    }, []);

    useEffect(() => {
        if (module.html != htmlRef.current) {
            htmlRef.current = module.html;
        }
        loadHtml(module.html);
    }, [module]);

    // Effect to handle changes to isEditable prop
    useEffect(() => {
        removeEditorEventListeners();
        if (!isEditable && editorRef.current) {
            // If editor is active and editing is disabled, hide the editor
            hideEditor();
        } else {
            addEditorEventListeners();
        }
        isEditorActive.current = isEditable;
    }, [isEditable]);

    // Cleanup on unmount
    useEffect(() => {
        return () => {
            if (editorRef.current) {
                editorRef.current.destroy();
            }
        };
    }, [editorRef]);

    //actions
    const getTextElement = () => {
        return document.querySelector(`.module-id-${module.id} .text-editor > .text`);
    };

    const addEditorEventListeners = () => {
        const elem = getTextElement();
        if (!elem) return;
        if (isEditable) {
            elem.addEventListener('mouseover', showEditor);
            elem.addEventListener('click', showEditor);
        }
    };

    const removeEditorEventListeners = () => {
        const elem = getTextElement();
        if (!elem) return;
        elem.removeEventListener('mouseover', showEditor);
        elem.removeEventListener('click', showEditor);
    };

    const loadHtml = (newhtml) => {
        if (!newhtml) newhtml = htmlRef.current;
        if(editorRef.current) return;
        const elem = getTextElement();
        if (!elem) return;
        elem.innerHTML = newhtml;
        removeEditorEventListeners();
        addEditorEventListeners();
    };

    const handleDataChange = () => {
        let newhtml = editorRef.current.getData();

        // Strip all style attributes from the HTML
        const tempDiv = document.createElement('div');
        tempDiv.innerHTML = newhtml;

        // Find all elements with style attributes and remove them
        const elementsWithStyle = tempDiv.querySelectorAll('[style]');
        elementsWithStyle.forEach(element => {
            element.removeAttribute('style');
        });
        const elementsWidthData = tempDiv.querySelectorAll('[data-placeholder]');
        elementsWidthData.forEach(element => {
            element.removeAttribute('data-placeholder');
        });

        // Get the cleaned HTML
        newhtml = tempDiv.innerHTML;

        htmlRef.current = newhtml;
        if (timerSave.current) clearTimeout(timerSave.current);
        timerSave.current = setTimeout(() => {
            onUpdate({
                ...module, html: newhtml,
                userInput: [...(module.userInput ? module.userInput.filter(a => a != userInput) : []), userInput]
            });
        }, 3000);
    };

    const handleClickOutside = (event) => {
        if (inModal.current) return;
        const editorContainer = document.querySelector(`.module-id-${module.id}`);
        const toolbar = document.querySelector('.ck-toolbar');
        let elem = event.target;
        while (elem && elem != null) {
            if (editorContainer.contains(elem) ||
                toolbar.contains(elem) ||
                elem.classList.contains('ck')) {
                return;
            }
            elem = elem.parentNode;
        }
        hideEditor();
    };

    //initialize WYSIWYG Editor (CKEditor)
    const showEditor = () => {
        // Don't show editor if editing is disabled
        if (isEditorActive.current == false) return;

        const elem = getTextElement();
        if (!elem) return;
        if (elem.querySelector('.main-container')) return; //editor already loaded
        //hide all other modules
        const allModules = window.entry?.textEditors;
        if (!window.entry) window.entry = { modules: {} };
        if (!window.entry.textEditors) window.entry.textEditors = {};
        if (allModules) Object.keys(allModules).forEach(key => allModules[key]());
        window.entry.textEditors = {};
        window.entry.textEditors[module.id] = hideEditor;

        //add event listener to hide editor
        elem.removeEventListener('mouseover', showEditor);
        elem.removeEventListener('click', showEditor);
        document.removeEventListener('mousedown', handleClickOutside);
        const initialData = elem.innerHTML;
        setTimeout(() => {
            document.addEventListener('mousedown', handleClickOutside);
        }, 100);

        const containerHtml = `<div class="main-container">
			<div
				class="editor-container editor-container_inline-editor editor-container_include-style"
				id="editor-container"
			>
				<div class="editor-container__editor"><div id="editor_textelem_${module.id}"></div></div>
			</div>
		</div>`;

        /*
         * Create a free account with a trial: https://portal.ckeditor.com/checkout?plan=free
         */
        const LICENSE_KEY = 'GPL';

        const fontSizes = [5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 36, 38, 40, 42, 45, 48, 52, 56, 60, 64, 72, 90, 100, 105, 110, 115, 120, 125, 130, 140, 150, 160, 170, 180, 190, 200, 225, 250, 275, 300, 325, 350, 375, 400, 425, 450, 475, 500];
        const lineHeights = [0.0, 0.1, 0.2, 0.25, 0.3, 0.4, 0.5, 0.6, 0.7, 0.75, 0.8, 0.9, 1.0, 1.05, 1.1, 1.15, 1.2, 1.25, 1.3, 1.35, 1.4, 1.45, 1.5, 1.55, 1.6, 1.65, 1.7, 1.75, 1.8, 1.85, 1.9, 1.95, 2.0, 2.25, 2.5, 2.75, 3.0, 3.25, 3.5, 3.75, 4, 4.25, 4.5, 4.75, 5];

        const editorConfig = {
            toolbar: {
                items: [
                    'heading',
                    'style',
                    '|',
                    'lineheight',
                    '|',
                    'link',
                    'emoji',
                    'specialCharacters',
                    'subscript',
                    'superscript',
                    'blockQuote',
                    '|',
                    'removeFormat',
                    'aiGenerator',
                    '-',
                    'fontSize',
                    'fontFamily',
                    'fontColor',
                    'fontBackgroundColor',
                    //'highlight',
                    '|',
                    'bold',
                    'italic',
                    'underline',
                    'strikethrough',
                    '|',
                    'alignment',
                    '|',
                    'bulletedList',
                    'numberedList',
                    'outdent',
                    'indent',
                ],
                shouldNotGroupWhenFull: true,
                viewportTopOffset: 160,
                viewportOffset: { top: 160 }
            },
            plugins: [
                Alignment,
                Autoformat,
                Autosave,
                BlockQuote,
                Bold,
                Emoji,
                Essentials,
                FontBackgroundColor,
                FontColor,
                FontFamily,
                FontSize,
                GeneralHtmlSupport,
                Heading,
                //Highlight,
                Indent,
                IndentBlock,
                Italic,
                Link,
                List,
                ListProperties,
                Mention,
                Paragraph,
                PasteFromOffice,
                PlainTableOutput,
                RemoveFormat,
                SpecialCharacters,
                SpecialCharactersArrows,
                SpecialCharactersCurrency,
                SpecialCharactersEssentials,
                SpecialCharactersLatin,
                SpecialCharactersMathematical,
                SpecialCharactersText,
                Strikethrough,
                Style,
                Subscript,
                Superscript,
                Table,
                TableCellProperties,
                TableColumnResize,
                TableProperties,
                TableToolbar,
                TextTransformation,
                Underline,
                LineHeight,
                // Create the AI Generator plugin using the factory function
                defineAIGeneratorPlugin(Plugin, ButtonView)
            ],
            fontFamily: {
                options: [
                    ...fonts.map(a => a.family)
                ],
                supportAllValues: true
            },
            fontSize: {
                options: ['default', ...fontSizes.map(a => { return { title: a, model: a + 'pt' } })],
                supportAllValues: true
            },
            heading: {
                options: [
                    {
                        model: 'paragraph',
                        title: 'Paragraph'
                    },
                    {
                        model: 'heading1',
                        view: 'h1',
                        title: 'Heading 1'
                    },
                    {
                        model: 'heading2',
                        view: 'h2',
                        title: 'Heading 2'
                    },
                    {
                        model: 'heading3',
                        view: 'h3',
                        title: 'Heading 3'
                    },
                    {
                        model: 'heading4',
                        view: 'h4',
                        title: 'Heading 4'
                    },
                    {
                        model: 'heading5',
                        view: 'h5',
                        title: 'Heading 5'
                    },
                    {
                        model: 'heading6',
                        view: 'h6',
                        title: 'Heading 6'
                    }
                ]
            },
            htmlSupport: {
                allow: [
                    {
                        name: /^.*$/,
                        styles: true,
                        attributes: true,
                        classes: true
                    }
                ]
            },
            initialData: initialData,
            startupFocus: true,
            licenseKey: LICENSE_KEY,
            placeholder: 'Type or paste your content here!',
            style: {
                definitions: [
                    {
                        name: 'Highlighted',
                        element: 'span',
                        classes: ['highlighted']
                    },
                ]
            },
            lineHeight: {
                options: [
                    {
                        title: 'default',
                        model: '1.15',
                    },
                    ...lineHeights.map(a => { return { title: a, model: a } })
                ],
                supportAllValues: true
            },
            indentBlock: {
                offset: 1,
                unit: 'em'
            }
        };

        [...document.querySelectorAll('.ck-body')].forEach(a => a.remove());
        const container = document.createElement('div');
        container.innerHTML = containerHtml;
        elem.appendChild(container);

        InlineEditor.create(container.querySelector('#editor_textelem_' + module.id), editorConfig)
            .then(editor => {
                elem.innerHTML = '';
                elem.appendChild(container);

                editor.model.document.on('change:data', handleDataChange);
                editor.focus();
                editorRef.current = editor;
            });
    };

    //destroy WYSIWYG Editor
    const hideEditor = () => {
        // Don't hide the editor if the modal is currently open
        if (inModal.current) return;
        document.removeEventListener('mousedown', handleClickOutside);
        removeEditorEventListeners();
        if (editorRef.current) {
            //destroy editor and load html
            editorRef.current.destroy();
            editorRef.current = null;
            loadHtml();
            console.log('destroyed editor');
        }else if (!isEditorActive.current) {
            //editor is not active, add event listeners instead
            addEditorEventListeners();
            console.log('added event listeners');
        }
    };

    const disableEditorEvents = () => {
        document.removeEventListener('mousedown', handleClickOutside);
    };

    const enableEditorEvents = () => {
        document.addEventListener('mousedown', handleClickOutside);
    };

    // AI Generator handlers
    const handleAIGeneratorRequest = () => {
        const ckeditorDiv = document.querySelector(`.module-id-${module.id} .ck.ck-content`);
        if (!ckeditorDiv) return;
        inModal.current = true; // Set inModal ref to true when opening modal
        setShowAIModal(true);
        disableEditorEvents();
    };

    const handleCloseAIModal = () => {
        inModal.current = false; // Set inModal ref to false when closing modal
        setShowAIModal(false);
    };

    const handleContentGenerated = (userInput, generatedContent) => {
        // Insert the generated content into the editor
        if (editorRef.current) {
            // Insert content using the model API directly
            const viewFragment = editorRef.current.data.processor.toView(
                `<div class="ai-generated">${generatedContent}</div>`);
            const modelFragment = editorRef.current.data.toModel(viewFragment);

            editorRef.current.model.change(writer => {
                editorRef.current.model.insertContent(modelFragment);
            });
        }
        inModal.current = false; // Set inModal ref to false when closing modal
        setShowAIModal(false);
        setUserInput(userInput);
        enableEditorEvents();
    };

    return (
        <>
            <div className="text-editor"><div className="text"></div></div>

            {showAIModal && <AIGeneratorModal module={module} onClose={handleCloseAIModal} onGenerated={handleContentGenerated} />}
        </>
    );
}