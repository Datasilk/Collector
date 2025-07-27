import { useState, useEffect, useRef, useMemo } from 'react';
import './texteditor.css'
import { CKEditor } from '@ckeditor/ckeditor5-react';
import {
    ClassicEditor,
	// InlineEditor,
	Autoformat,
	AutoImage,
	Autosave,
	BlockQuote,
	Bold,
	CloudServices,
	Emoji,
	Essentials,
	Heading,
	ImageBlock,
	ImageCaption,
	ImageInline,
	ImageInsertViaUrl,
	ImageResize,
	ImageStyle,
	ImageTextAlternative,
	ImageToolbar,
	ImageUpload,
	Indent,
	IndentBlock,
	Italic,
	Link,
	LinkImage,
	List,
	ListProperties,
	MediaEmbed,
	Mention,
	Paragraph,
    Alignment,
    FontSize,
    FontFamily,
    FontColor,
    FontBackgroundColor,
    SourceEditing,
    //CodeBlock,
    //HtmlEmbed,
	PasteFromOffice,
	SpecialCharacters,
	SpecialCharactersArrows,
	SpecialCharactersCurrency,
	SpecialCharactersEssentials,
	SpecialCharactersLatin,
	SpecialCharactersMathematical,
	SpecialCharactersText,
	Table,
	TableCaption,
	TableCellProperties,
	TableColumnResize,
	TableProperties,
	TableToolbar,
	TextTransformation,
	TodoList,
	Underline,
	WordCount
} from 'ckeditor5';
import 'ckeditor5/ckeditor5.css';

export default function TextEditor({value, defaultValue, placeholder, onReady, onChange})  {
    const [text, setText] = useState();
	const editorContainerRef = useRef(null);
	const editorRef = useRef(null);
	const editorWordCountRef = useRef(null);
	const [mounted, setMounted] = useState(false);
    const LICENSE_KEY = 'GPL';

	useEffect(() => {
        if(!text) {setText(value);}
		if(!mounted){setMounted(true);}
	}, []);

    useEffect(() => {
        if(text != defaultValue){
            setText(defaultValue);
        }
    }, [defaultValue]);

	const { editorConfig } = useMemo(() => {
		if (!mounted) {
			return {};
		}

		return {
			editorConfig: {
				toolbar: {
					items: [
						'heading',
						'|',
						'bold',
						'italic',
						'underline',
						'|',
                        'fontSize',
                        'fontFamily', 
                        'fontColor', 
                        'fontBackgroundColor',
						'|',
                        'alignment',
						'bulletedList',
						'numberedList',
						'todoList',
						'outdent',
						'indent',
                        '|',
						'emoji',
						'specialCharacters',
						'link',
						'mediaEmbed',
						'insertTable',
						'blockQuote',
                        '|',
                        'sourceEditing',
                        //'htmlEmbed',
                        //'codeBlock'
					],
					shouldNotGroupWhenFull: false
				},
				plugins: [
					Autoformat,
					AutoImage,
					Autosave,
					BlockQuote,
					Bold,
					CloudServices,
					Emoji,
					Essentials,
					Heading,
					ImageBlock,
					ImageCaption,
					ImageInline,
					ImageInsertViaUrl,
					ImageResize,
					ImageStyle,
					ImageTextAlternative,
					ImageToolbar,
					ImageUpload,
					Indent,
					IndentBlock,
					Italic,
					Link,
					LinkImage,
					List,
					ListProperties,
					MediaEmbed,
					Mention,
					Paragraph,
                    Alignment,
                    FontSize,
                    FontFamily,
                    FontColor,
                    FontBackgroundColor,
                    SourceEditing,
                    //CodeBlock,
                    //HtmlEmbed,
					PasteFromOffice,
					SpecialCharacters,
					SpecialCharactersArrows,
					SpecialCharactersCurrency,
					SpecialCharactersEssentials,
					SpecialCharactersLatin,
					SpecialCharactersMathematical,
					SpecialCharactersText,
					Table,
					TableCaption,
					TableCellProperties,
					TableColumnResize,
					TableProperties,
					TableToolbar,
					TextTransformation,
					TodoList,
					Underline,
					WordCount
				],
				heading: {
					options: [
						{
							model: 'paragraph',
							title: 'Paragraph',
						},
						{
							model: 'heading1',
							view: 'h1',
							title: 'Heading 1',
						},
						{
							model: 'heading2',
							view: 'h2',
							title: 'Heading 2',
						},
						{
							model: 'heading3',
							view: 'h3',
							title: 'Heading 3',
						},
						{
							model: 'heading4',
							view: 'h4',
							title: 'Heading 4',
						},
						{
							model: 'heading5',
							view: 'h5',
							title: 'Heading 5',
						},
						{
							model: 'heading6',
							view: 'h6',
							title: 'Heading 6',
						}
					]
				},
				image: {
					toolbar: [
						'toggleImageCaption',
						'imageTextAlternative',
						'|',
						'imageStyle:inline',
						'imageStyle:wrapText',
						'imageStyle:breakText',
						'|',
						'resizeImage'
					]
				},
				licenseKey: LICENSE_KEY,
				link: {
					addTargetToExternalLinks: true,
					defaultProtocol: 'https://',
					decorators: {
						toggleDownloadable: {
							mode: 'manual',
							label: 'Downloadable',
							attributes: {
								download: 'file'
							}
						}
					}
				},
				list: {
					properties: {
						styles: true,
						startIndex: true,
						reversed: true
					}
				},
				mention: {
					feeds: [
						{
							marker: '@',
							feed: [
								/* See: https://ckeditor.com/docs/ckeditor5/latest/features/mentions.html */
							]
						}
					]
				},
				placeholder: placeholder ?? '',
				table: {
					contentToolbar: ['tableColumn', 'tableRow', 'mergeTableCells', 'tableProperties', 'tableCellProperties']
				},
                // fontFamily: {
                //     options: [
                //         'default',
                //         'Ubuntu, Arial, sans-serif',
                //         'Ubuntu Mono, Courier New, Courier, monospace'
                //     ]
                // },
                //fontSize: {
                //    options: [
                //        'tiny',
                //        'default',
                //        'big'
                //    ],
                //    supportAllValues: true
                //},
                //htmlSupport: {
                //    // List the HTML features to be supported
                //    elements: ['p', 'h1', 'h2', 'h3', 'ul', 'ol', 'li', 'strong', 'em'],
                //    attributes: ['class', 'data-custom'],
                //    styles: ['color', 'font-size']
                //},
                fontColor: {
                    colors: [
                        {
                            color: 'hsl(0, 0%, 0%)',
                            label: 'Black'
                        },
                        {
                            color: 'hsl(0, 0%, 30%)',
                            label: 'Dim grey'
                        },
                        {
                            color: 'hsl(0, 0%, 60%)',
                            label: 'Grey'
                        },
                        {
                            color: 'hsl(0, 0%, 90%)',
                            label: 'Light grey'
                        },
                        {
                            color: 'hsl(0, 0%, 100%)',
                            label: 'White',
                            hasBorder: true
                        },
                        // More colors.
                        // ...
                    ]
                },
                fontBackgroundColor: {
                    colors: [
                        {
                            color: 'hsl(0, 75%, 60%)',
                            label: 'Red'
                        },
                        {
                            color: 'hsl(30, 75%, 60%)',
                            label: 'Orange'
                        },
                        {
                            color: 'hsl(60, 75%, 60%)',
                            label: 'Yellow'
                        },
                        {
                            color: 'hsl(90, 75%, 60%)',
                            label: 'Light green'
                        },
                        {
                            color: 'hsl(120, 75%, 60%)',
                            label: 'Green'
                        },
                        // More colors.
                        // ...
                    ]
                },
                toolbarStartupExpanded :true,
                toolbarCanCollapse :true,
                minimap:true,
                ui:{
                    poweredBy:{
                        verticalOffset:-999999
                    }
                }
			}
		};
	}, [mounted]);

	return (
		<div className="main-container">
			<div className="editor-container editor-container_inline-editor editor-container_include-word-count" ref={editorContainerRef}>
				<div className="editor-container__editor">
					<div ref={editorRef}>
						{editorConfig && (
							<CKEditor
								onReady={editor => {
									const wordCount = editor.plugins.get('WordCount');
									editorWordCountRef.current.appendChild(wordCount.wordCountContainer);
                                    if(typeof onReady == 'function') onReady();
								}}
                                onChange={onChange}
								onAfterDestroy={() => {
									//Array.from(editorWordCountRef.current.children).forEach(child => child.remove());
								}}
								editor={ClassicEditor}
								config={editorConfig}
                                data={text}
							/>
						)}
					</div>
				</div>
				<div className="editor_container__word-count" ref={editorWordCountRef}></div>
			</div>
		</div>
	);

}