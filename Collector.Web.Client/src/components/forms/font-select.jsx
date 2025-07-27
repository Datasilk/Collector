import React, { useEffect, useState } from 'react';
import '@/styles/forms.css';
/**
 * <summary>Font select component</summary>
 * <description>Displays a dropdown for selecting a font from a provided list.</description>
 * @param {string} name - The name attribute for the select (used in forms).
 * @param {Array<{label: string, value: string, family: string, categories: Array<string>, className: string}>} fonts - The list of font options to display.
 * @param {string} defaultValue - The value of the font to select by default.
 * @param {string} defaultFont - The font family to use as the default if no value is selected.
 * @param {function} onChange - Callback when the selected font changes, receives the selected font value as an argument.
 * @param {function} onShow - Callback when the dropdown is shown, receives the dropdown element's bounding rectangle as an argument.
 */
export default function FontSelect({ name, fonts, defaultValue, defaultFont, onChange, onShow }) {
    //render a custom select using div + unordered list so that we can display custom font families for each list item
    const [selected, setSelected] = useState(fonts.filter(a => a.value == defaultValue ? defaultFont : 'arial')[0]);
    //const [filter, setFilter] = useState({ search: '', categories: [], start: 0, length: 250 });
    const [filter] = useState({ search: '', categories: [], start: 0, length: 250 });
    const [show, setShow] = useState(false);

    useEffect(() => {
        if (!show || typeof onShow != 'function') return;
        const elem = document.querySelector('.custom-select.for-' + name + ' ul');
        elem.rect = elem.getBoundingClientRect();
        onShow(elem);
    }, [show]);

    const filterFonts = ({ search, categories }) => fonts.filter(a => (!search || (a.label.indexOf(search) >= 0)) && (!categories || (categories.filter(b => a.categories.indexOf(b) >= 0).length == categories.length)));
    
    const handleToggleFonts = () => {
        if(!show){
            document.body.addEventListener('click', handleCancelShow);
        }else{
            document.body.removeEventListener('click', handleCancelShow);
        }
        setShow(!show);
    }

    const handleCancelShow = (e) => {
        let target = e.target;
        while(target != null){
            if(target.className?.indexOf('custom-select') >= 0){return;}
            target = target.parentNode;
        }
        setShow(false);
        document.body.removeEventListener('click', handleCancelShow);
    }

    const handleOnChange = (font) => {
        setSelected(font);
        if(typeof onChange == 'function') onChange(font.value);
    }

    return (

        <div className="form-group">
            <div className={'custom-select for-' + name}
                id={name}
                name={name}
            >
                <span onClick={handleToggleFonts} className="selected-font" style={{ fontFamily: selected?.family ?? 'arial' }}>{selected?.label ?? 'Arial'}</span>
                {show &&
                    <ul>
                        {filterFonts(filter) //filter
                            .filter((a, i) => i >= filter.start && i <= filter.start + filter.length) //paging
                            .map(a => <li key={a.value} onClick={() => handleOnChange(a)} className={a.className} style={{ fontFamily: a?.family ?? 'arial' }}>{a.label}</li>)
                        }
                    </ul>
                }
            </div>
        </div>
    );
}