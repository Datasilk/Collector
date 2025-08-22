import { useState, useEffect, useRef } from 'react';
import './ckeditor.css';
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
	Underline
    
} from 'ckeditor5';
import 'ckeditor5/ckeditor5.css';
import { LineHeight } from '@rickx/ckeditor5-line-height'
//helpers
import { fonts } from '@/helpers/fonts';

export default function CKEditorModule({ id, onLoad, onUpdate }) {
    //state
    const [editor, setEditor] = useState();
    const [mounted, setMounted] = useState(false);

    //ref
    const dataRef = useRef();

    //initialize WYSIWYG Editor (CKEditor)
    useEffect(() => {
        if (!mounted) { setMounted(true); return; }
        const elem = document.querySelector(`.entry .element.id-${id} > .text`);
        if(!elem) return;
        const initialData = elem.innerHTML;
        const containerHtml = `<div class="editor-textelement-${id} main-container">
			<div
				class="editor-container editor-container_inline-editor editor-container_include-style"
				id="editor-container"
			>
				<div class="editor-container__editor"><div id="editor_textelem_${id}"></div></div>
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
                    'emoji',
                    'specialCharacters',
                    'subscript',
                    'superscript',
                    'blockQuote',
                    '|',
                    'removeFormat',
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
                shouldNotGroupWhenFull: true
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
                    LineHeight
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
        let myEditor;
        InlineEditor.create(container.querySelector('#editor_textelem_' + id), editorConfig)
            .then(editor => {
                myEditor = editor;
                elem.innerHTML = '';
                elem.appendChild(container);
                
                if (typeof onLoad == 'function') onLoad();
                if (typeof onUpdate == 'function') {
                    myEditor.model.document.on('change:data', () => onUpdate(myEditor.getData()));
                }
                myEditor.editing.view.document.on('click', () => {
                    myEditor.ui.view.toolbar.element.parentNode.style.opacity = 1;
                });
                myEditor.focus();
                dataRef.current = myEditor.getData();
                setEditor(myEditor);
            });
        window.slide.draggable = false;
    }, [mounted, id]);

    return <></>;
}