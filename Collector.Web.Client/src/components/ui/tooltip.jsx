import React, { useState, useEffect } from "react";
import './tooltip.css';
import Icon from '@/components/ui/icon';

export default function Tooltip({ name, children, title, offsetX, offsetY, target, width, placement }) {
    const [show, setShow] = useState(false);
    const [style, setStyle] = useState({});
    const [tooltipStyle, setTooltipStyle] = useState({});
    const targetRect = target && typeof target == 'function' && show ? target().getBoundingClientRect() : {};
    const navHeight = 72;

    useEffect(() => {
        const newstyle = {};
        if (offsetX) newstyle.left = offsetX;
        if (offsetY) newstyle.top = offsetY;
        if(width) newstyle.maxWidth = width;
        setStyle(newstyle);
    }, [offsetX, offsetY]);

    useEffect(() => {
        if(show){
            const rect = document.querySelector('.tooltip-' + name).getBoundingClientRect();
            const newstyle = { ...tooltipStyle };
            if (rect.bottom > window.innerHeight) {
                newstyle.bottom = '10px';
            }
            if (rect.right > window.innerHeight) {
                newstyle.right = '-10px';
            }

            if(target && typeof target == 'function'){
                if(placement == 'above' && rect.height + 10 < targetRect.top - 10 - navHeight ||
                    targetRect.bottom + 10 + rect.height > window.innerHeight){
                    //above target
                    let top = targetRect.top - rect.height - 10;
                    if(top < navHeight + 10) top = navHeight + 10;
                    newstyle.top = top + 'px';
                }else{
                    //below target
                    newstyle.top = (targetRect.bottom + 10) + 'px';
                }
                newstyle.right = (window.innerWidth - targetRect.right) + 'px';
                newstyle.position = 'fixed';
                newstyle.bottom = null;
                newstyle.left = null;
            }
            setTooltipStyle(newstyle);
        }
    }, [show]);

    const handleShowTooltip = () => setShow(true);
    const handleClose = () => setShow(false);

    const buttonOptions = { className: 'icon' };
    if (title) buttonOptions.title = title;

    return (
        <div className="tooltip-container" style={style}>
            {show && <>
                <div className="tooltip-overlay" onClick={handleClose}>
                    {target && typeof target == 'function' ? <>
                        <div className="overlay-top" style={{height:targetRect.top + 'px'}}></div>
                        <div className="overlay-left" style={{width:targetRect.left + 'px', height:(targetRect.bottom - targetRect.top) + 'px'}}></div>
                        <div className="overlay-right" style={{width:(window.innerWidth - targetRect.right) + 'px', height:(targetRect.bottom - targetRect.top) + 'px'}}></div>
                        <div className="overlay-bottom" style={{height:(window.innerHeight - targetRect.bottom) + 'px'}}></div>
                    </> : <>
                        <div className="overlay"></div>
                    </>}
                </div>
                <div className={"tooltip tooltip-" + name} style={tooltipStyle}><Icon name="info"></Icon><span className="text">{children}</span></div>
                </>}
            <button onClick={handleShowTooltip} {...buttonOptions}>
                <Icon name="info"></Icon>
            </button>
        </div>
    );
}