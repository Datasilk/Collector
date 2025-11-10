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
    Clipboard,
    CodeBlock,
    Emoji,
    Essentials,
    FontBackgroundColor,
    FontColor,
    FontFamily,
    FontSize,
    GeneralHtmlSupport,
    Heading,
    Highlight,
    HorizontalLine,
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
import { useSession } from '@/context/session';


export default function CKEditorModule({ module, onUpdate, isEditable = true, manuallyAdded = false }) {
    //state
    const [mounted, setMounted] = useState(false);
    const [showAIModal, setShowAIModal] = useState(false);
    const [userInput, setUserInput] = useState('');
    const [showToolbar, setShowToolbar] = useState(false);

    //refs
    const htmlRef = useRef(null);
    const editorRef = useRef(null);
    const timerSave = useRef(null);
    const inModal = useRef(false);
    const isEditorActive = useRef(false);
    const defaultHtml = '<p>Type or paste your content here!</p>';

    //context
    const session = useSession();

    //effect
    useEffect(() => {
        if (mounted) return;
        setMounted(true);

        // Add event listener for AI generator button click from CK Editor AI plugin toolbar button
        document.addEventListener('aiGeneratorRequest', handleAIGeneratorRequest);

        // Auto-focus if this module was manually added
        if (module.manuallyAdded) {
            setTimeout(() => {
                showEditor();
            }, 500);
        }

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
            //elem.addEventListener('mouseover', showEditor);
            elem.addEventListener('mouseup', showEditor);
        }
    };

    const removeEditorEventListeners = () => {
        const elem = getTextElement();
        if (!elem) return;
        //elem.removeEventListener('mouseover', showEditor);
        elem.removeEventListener('mouseup', showEditor);
    };


    const loadHtml = (newhtml) => {
        if (!newhtml && manuallyAdded && (htmlRef.current == null || htmlRef.current == '')) {
            newhtml = defaultHtml;
        }
        if (!newhtml) newhtml = htmlRef.current;
        if (editorRef.current) return;
        const elem = getTextElement();
        if (!elem) return;
        elem.innerHTML = newhtml;
        //make all anchor links open in new tab
        const anchors = elem.querySelectorAll('a');
        anchors.forEach(anchor => {
            anchor.setAttribute('target', '_blank');
        });
        //re-attach secret content event listeners
        const secrets = elem.querySelectorAll('.secret-content');
        secrets.forEach(secret => {
            secret.onclick = (e) => {
                e.preventDefault();
                e.target.classList.toggle('show-secret');
            };
        });
        removeEditorEventListeners();
        addEditorEventListeners();
    };

    const handleDataChange = () => {
        let newhtml = editorRef.current.getData();

        // Filter style attributes to only allow specific properties
        const tempDiv = document.createElement('div');
        tempDiv.innerHTML = newhtml;

        // Allowed style properties
        const allowedStyles = [
            'color',
            'font-family',
            'font-size',
            'background',
            'background-color',
            'background-image',
            'font-weight',
            'line-height',
            'text-decoration'
        ];

        // Find all elements with style attributes and filter them
        const elementsWithStyle = tempDiv.querySelectorAll('[style]');
        elementsWithStyle.forEach(element => {
            const currentStyle = element.getAttribute('style');
            if (currentStyle) {
                // Parse the style attribute
                const styleObj = {};
                currentStyle.split(';').forEach(rule => {
                    const [property, value] = rule.split(':').map(s => s.trim());
                    if (property && value && allowedStyles.includes(property.toLowerCase())) {
                        styleObj[property] = value;
                    }
                });

                // Rebuild the style attribute with only allowed properties
                const newStyle = Object.entries(styleObj)
                    .map(([prop, val]) => `${prop}: ${val}`)
                    .join('; ');

                if (newStyle) {
                    element.setAttribute('style', newStyle);
                } else {
                    element.removeAttribute('style');
                }
            }
        });

        const elementsWidthData = tempDiv.querySelectorAll('[data-placeholder]');
        elementsWidthData.forEach(element => {
            element.removeAttribute('data-placeholder');
        });

        // Get the cleaned HTML
        newhtml = tempDiv.innerHTML;
        htmlRef.current = newhtml;
        handlePostponeSave();
    };

    const handlePostponeSave = () => {
        if (timerSave.current) clearTimeout(timerSave.current);
        timerSave.current = setTimeout(() => {
            onUpdate({
                ...module, html: htmlRef.current,
                userInput: [...(module.userInput ? module.userInput.filter(a => a != userInput) : []), userInput]
            });
            timerSave.current = null;
        }, 3000);
    };

    const handleKeyDown = (input) => {
        if (timerSave.current != null) handlePostponeSave();
    };

    const handleClickOutside = (event) => {
        if (inModal.current) return;
        const editorContainer = document.querySelector(`.module-id-${module.id}`);
        if (!editorContainer) { hideEditor(); return; }
        const toolbar = document.querySelector('.ck-toolbar');
        let elem = event.target;
        while (elem && elem != null) {
            if (editorContainer?.contains(elem) ||
                toolbar?.contains(elem) ||
                elem?.classList?.contains('ck')) {
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

        // Don't initialize again if already initialized
        if (editorRef.current) return;

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

        setShowToolbar(true);

        //add event listener to hide editor
        //elem.removeEventListener('mouseover', showEditor);
        elem.removeEventListener('mouseup', showEditor);
        document.removeEventListener('mousedown', handleClickOutside);

        //make all anchor links open in new tab
        const anchors = elem.querySelectorAll('a');
        anchors.forEach(anchor => {
            anchor.setAttribute('target', '_blank');
        });

        //re-attach secret content event listeners
        const secrets = elem.querySelectorAll('.secret-content');
        secrets.forEach(secret => {
            secret.onclick = (e) => {
                e.preventDefault();
                e.target.classList.toggle('show-secret');
            };
        });

        //load initial data into CKEditor
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
                    'alignment:left',
                    'alignment:center',
                    'alignment:right',
                    'alignment:justify',
                    '|',
                    'bulletedList',
                    'numberedList',
                    'outdent',
                    'indent',
                    "|",
                    'horizontalLine',
                    'removeFormat',
                    "|",
                    {
                        label: 'Advanced',
                        icon: 'threeVerticalDots',
                        items: [
                            'specialCharacters',
                            'subscript',
                            'superscript',
                            'blockQuote'
                        ]
                    },
                    '-',
                    'fontSize',
                    'lineheight',
                    '|',
                    'fontFamily',
                    'fontColor',
                    'fontBackgroundColor',
                    'highlight',
                    '|',
                    'bold',
                    'italic',
                    'underline',
                    'strikethrough',
                    '|',
                    'link',
                    '|',
                    'insertTable',
                    'codeBlock',
                    'emoji',
                    'aiGenerator',
                ],
                shouldNotGroupWhenFull: true,
                viewportOffset: { top: 160 }
            },
            plugins: [
                Alignment,
                Autoformat,
                Autosave,
                BlockQuote,
                Bold,
                Clipboard,
                CodeBlock,
                Emoji,
                Essentials,
                FontBackgroundColor,
                FontColor,
                FontFamily,
                FontSize,
                GeneralHtmlSupport,
                Heading,
                Highlight,
                HorizontalLine,
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
                        name: /^(div|span|section|article|aside|header|footer|nav|main|figure|figcaption|mark|time|details|summary|br|hr)$/,
                        styles: false,
                        attributes: false,
                        classes: false
                    },
                    {
                        name: 'p',
                        styles: false,
                        attributes: false,
                        classes: false
                    },
                    {
                        name: /^h[1-6]$/,
                        styles: false,
                        attributes: false,
                        classes: false
                    },
                    {
                        name: /^(ul|ol|li)$/,
                        styles: false,
                        attributes: false,
                        classes: false
                    },
                    {
                        name: 'a',
                        styles: false,
                        attributes: false,
                        classes: false
                    },
                    {
                        name: /^(strong|b|em|i|u|s|sub|sup|code|pre)$/,
                        styles: false,
                        attributes: false,
                        classes: false
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
                        name: 'Button',
                        element: 'a',
                        classes: ['button']
                    },
                    {
                        name: 'Button Outlined',
                        element: 'a',
                        classes: ['button outline']
                    },
                    {
                        name: 'Button Outlined',
                        element: 'a',
                        classes: ['button', 'outline']
                    },
                    {
                        name: 'Button Inline',
                        element: 'a',
                        classes: ['button', 'inline']
                    },
                    {
                        name: 'Note',
                        element: 'p',
                        classes: ['note', 'grey']
                    },
                    {
                        name: 'Note - Red',
                        element: 'p',
                        classes: ['note', 'red']
                    },
                    {
                        name: 'Note - Green',
                        element: 'p',
                        classes: ['note', 'green']
                    },
                    {
                        name: 'Note - Blue',
                        element: 'p',
                        classes: ['note', 'blue']
                    },
                    {
                        name: 'Note - Yellow',
                        element: 'p',
                        classes: ['note', 'yellow']
                    },
                    {
                        name: 'Note - Orange',
                        element: 'p',
                        classes: ['note', 'orange']
                    },
                    {
                        name: 'Note - Purple',
                        element: 'p',
                        classes: ['note', 'purple']
                    },
                    {
                        name: 'Note - Pink',
                        element: 'p',
                        classes: ['note', 'pink']
                    },
                    {
                        name: 'Information',
                        element: 'p',
                        classes: ['information']
                    },
                    {
                        name: 'Warning',
                        element: 'p',
                        classes: ['warning']
                    },
                    {
                        name: 'Error',
                        element: 'p',
                        classes: ['error']
                    },
                    {
                        name: 'Secret',
                        element: 'span',
                        classes: ['secret-content']
                    },
                    {
                        name: 'Inline Code',
                        element: 'span',
                        classes: ['inline-code']
                    },
                    {
                        name: 'Monospace',
                        element: 'span',
                        classes: ['monospace']
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
            },
            codeBlock: {
                languages: [
                    { language: 'plaintext', label: 'Plain text' },
                    { language: 'html', label: 'HTML' },
                    { language: 'css', label: 'CSS' },
                    { language: 'javascript', label: 'JavaScript' }
                ],
                indentSequence: '    ' // Example: 4 spaces for indentation
            },
            table: {
                contentToolbar: [
                    'tableColumn',
                    'tableRow',
                    'mergeTableCells',
                    'tableProperties',
                    'tableCellProperties',
                ],
            },
        };

        [...document.querySelectorAll('.ck-body')].forEach(a => a.remove());
        const container = document.createElement('div');
        container.innerHTML = containerHtml;

        InlineEditor.create(container.querySelector('#editor_textelem_' + module.id), editorConfig)
            .then(editor => {
                elem.innerHTML = '';
                elem.appendChild(container);

                // Strip unwanted attributes on paste while preserving block structure
                editor.plugins.get('ClipboardPipeline').on('contentInsertion', (evt, data) => {
                    const content = data.content;

                    // Define allowed attributes
                    const allowedAttributes = [
                        'linkHref',
                        'highlight',
                        'bold',
                        'italic',
                        'underline',
                        'strikethrough',
                        'code',
                        'subscript',
                        'superscript',
                        'alignment',
                        'indent',
                        'blockIndent',
                        'listType',
                        'listIndent',
                        'listItemId',
                        'tableCellWidth',
                        'tableCellHeight',
                        'tableCellPadding',
                        'tableCellBorderStyle',
                        'tableCellBorderColor',
                        'tableCellBorderWidth',
                        'tableCellBackgroundColor',
                        'colspan',
                        'rowspan',
                        'htmlAttributes',
                        'htmlContentAttributes'
                    ];

                    // Process the model fragment to remove unwanted attributes
                    editor.model.change(writer => {
                        // Recursive function to process element and all its children
                        const processElement = (element) => {
                            // Check if element has getAttributeKeys method (it's a model element)
                            if (element && element.getAttributeKeys) {
                                // Get all attributes
                                const attributes = Array.from(element.getAttributeKeys());
                                if (element._removeAttribute) {
                                    element._removeAttribute('style');
                                }

                                // Remove all attributes except essential CKEditor ones
                                attributes.forEach(attr => {
                                    // Keep only CKEditor's internal attributes and allowed styles
                                    if (!attr.startsWith('html') && !allowedAttributes.includes(attr)) {
                                        writer.removeAttribute(attr, element);
                                    }
                                });

                                // Process all children recursively
                                if (typeof element.getChildren === 'function') {
                                    for (const child of element.getChildren()) {
                                        processElement(child);
                                    }
                                }
                            }
                        };

                        const range = writer.createRangeIn(content);

                        for (const item of range.getItems()) {
                            processElement(item);
                        }
                    });
                }, { priority: 'high' });

                // Add line breaks after htmlDivParagraph elements after content is inserted
                let isPasting = false;
                editor.plugins.get('ClipboardPipeline').on('inputTransformation', () => {
                    isPasting = true;
                }, { priority: 'highest' });

                editor.model.document.on('change:data', () => {
                    if (!isPasting) return;
                    isPasting = false;

                    editor.model.change(writer => {
                        const root = editor.model.document.getRoot();
                        const range = writer.createRangeIn(root);
                        const textNodesToProcess = [];
                        const htmlDivParagraphs = [];

                        // Find all text nodes with \n and htmlDivParagraph elements
                        for (const item of range.getItems()) {
                            if (item && item.data && item.data.indexOf('\n') >= 0) {
                                console.log('text node with \n found', item);
                                textNodesToProcess.push(item);
                            }
                            if (item && item.name === 'htmlDivParagraph') {
                                console.log('htmlDivParagraph found', item);
                                htmlDivParagraphs.push(item);
                            }
                        }

                        // Replace \n with softBreak in text nodes
                        // Process in reverse order to avoid index shifting issues
                        for (let i = textNodesToProcess.length - 1; i >= 0; i--) {
                            const textNode = textNodesToProcess[i];
                            const parent = textNode.parent;
                            if (!parent) continue;
                            
                            const text = textNode.data;
                            const parts = text.split('\n');

                            if (parts.length > 1) {
                                // Get position before the text node
                                const insertPosition = writer.createPositionBefore(textNode);
                                
                                // Remove the original text node
                                writer.remove(textNode);

                                // Insert text parts with softBreak elements between them
                                let currentPosition = insertPosition;
                                for (let j = 0; j < parts.length; j++) {
                                    const part = parts[j];
                                    if (part.length > 0) {
                                        writer.insertText(part, currentPosition);
                                        currentPosition = currentPosition.getShiftedBy(part.length);
                                    }
                                    // Don't add softBreak after the last part
                                    if (j < parts.length - 1) {
                                        writer.insert(writer.createElement('softBreak'), currentPosition);
                                        currentPosition = currentPosition.getShiftedBy(1);
                                    }
                                }
                            }
                        }

                        // Insert line breaks after each htmlDivParagraph
                        htmlDivParagraphs.forEach(element => {
                            const parent = element.parent;
                            if (parent) {
                                const position = writer.createPositionAfter(element);
                                writer.insert(writer.createElement('softBreak'), position);
                            }
                        });
                    });
                });

                editor.model.document.on('change:data', handleDataChange);
                editor.editing.view.document.on('keydown', handleKeyDown);
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
        }
        session.hideModal();
        setShowToolbar(false);
        addEditorEventListeners();
    };

    const disableEditorEvents = () => {
        document.removeEventListener('mousedown', handleClickOutside);
        if (editorRef.current) editorRef.current.model.document.off('change:data', handleDataChange);
        if (editorRef.current) editorRef.current.editing.view.document.off('keydown', handleKeyDown);
    };

    const enableEditorEvents = () => {
        document.addEventListener('mousedown', handleClickOutside);
        if (editorRef.current) editorRef.current.model.document.on('change:data', handleDataChange);
        if (editorRef.current) editorRef.current.editing.view.document.on('keydown', handleKeyDown);
    };

    // AI Generator handlers
    const handleAIGeneratorRequest = () => {
        const ckeditorDiv = document.querySelector(`.module-id-${module.id} .ck.ck-content`);
        if (!ckeditorDiv) return;
        inModal.current = true; // Set inModal ref to true when opening modal
        session.showModal(<AIGeneratorModal module={module} onClose={handleCloseAIModal} onGenerated={handleContentGenerated} />);
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
            <div className={"text-editor" + (showToolbar ? ' active' : '')}><div className="text"></div></div>
        </>
    );
}