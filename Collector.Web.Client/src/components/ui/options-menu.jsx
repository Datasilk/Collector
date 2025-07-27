import React, {useState, useEffect} from "react";
import './options-menu.css';
//components
import Icon from '@/components/ui/icon';

export default function OptionsMenu({ items, offsetX, offsetY, onClose }) {
    const [style, setStyle] = useState({opacity:0});

    useEffect(() => {
        const newstyle = {opacity:1};
        if (offsetX) newstyle.marginLeft = offsetX;
        if (offsetY) newstyle.marginTop = offsetY;
        setStyle(newstyle);
    }, [items, offsetX, offsetY]);

    useEffect(() => {
        const rect = document.querySelector('.options-menu').getBoundingClientRect();
        const newtop = (window.innerHeight - (rect.top + rect.height + 10)) + 'px';
        if(rect.top + rect.height > window.innerHeight && style.marginTop != newtop) {
            const newstyle = {...style};
            newstyle.marginTop = newtop;
            setStyle(newstyle);
        }
    }, [style]);
    
    return (<>
    <div className="options-menu-overlay" onClick={onClose}></div>
    <ul className="options-menu" style={style}>
        {items.map(a => <li key={a.label} onClick={(e) => {a.onClick(e);onClose()}}><Icon name={a.icon}></Icon>{a.label}</li>)}
    </ul>
    </>);
}