import React from "react";
import './modal.css';
import Icon from '@/components/ui/icon';


export default function Modal({children, title = "", onClose, noClose, wide = false}) {

    const handleClose = (e) => {
        if(e.target.className.indexOf('modal-overlay') >= 0 || e.target.className.indexOf('modal-container') >= 0){
            typeof onClose == 'function' && onClose();
        }
    }
    return (
        <div className="modal-overlay" onMouseDown={noClose != true ?handleClose : () => {}}>
            <div className="modal-container">
                <div className={`modal ${wide ? 'wide' : ''}`}>
                    <div className="modal-title-bar">
                        <h3>{title != "" && <span>{title}</span>}</h3>
                        {noClose != true &&<div className="modal-close-btn" onClick={onClose}><Icon name="close"></Icon></div>}
                    </div>
                    <div className="modal-body">
                        {children}
                    </div>
                </div>
            </div>
        </div>
    )
}