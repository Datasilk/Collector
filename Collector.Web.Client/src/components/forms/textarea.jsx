import { useEffect, useRef, useState } from 'react';
import '@/styles/forms.css';

export default function TextArea({ label, name, defaultValue, rows, placeholder, title, required = false, isLabel = false, maxLength = null, onInput, error, autoResize = false }) {
    //state
    const [mounted, setMounted] = useState(false);
    //set up optional properties
    const optional = {};
    if (maxLength) optional.maxLength = maxLength;
    if (title) optional.title = title;

    //refs
    const ref = useRef(null);

    //effect
    useEffect(() => {
        if(mounted) {
            handleResize();
        }else{
            setMounted(true);
        }
    }, [mounted]);

    //actions
    const handleInput = (e) => {
        if(onInput) onInput(e);
        handleResize();
    }

    const handleResize = () => {
        if(autoResize){
            ref.current.style.height = 'auto';
            ref.current.style.height = (ref.current.scrollHeight + 20) + 'px';
        }
    }

    //render input
    return (
        <div className={"form-group textarea-" + name}>
            <div className="form-label">
                {label && <label htmlFor={name}>{label}{required ? ' *' : ''}</label>}
                {error ? <span className="error">{error}</span> : <></>}
            </div>

            {!isLabel ?
                <textarea
                    id={name}
                    name={name}
                    defaultValue={defaultValue}
                    rows={rows ?? 1}
                    ref={ref}
                    onInput={handleInput}
                    onBlur={handleResize}
                    placeholder={placeholder}
                    {...(autoResize && {onLoad: handleInput})}
                    {...optional}
                ></textarea>    
                : <span className="input-islabel">{defaultValue}</span>}
        </div>
    );
}