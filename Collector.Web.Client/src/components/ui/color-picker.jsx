import { useRef } from "react";
import './color-picker.css';
import { hexToHsv, hsvToHex } from "@/helpers/colors";

export default function ColorPicker({ defaultColor, onChange, onChangeComplete }) {
    //refs
    const color = useRef(defaultColor);
    const saved = useRef(defaultColor);
    const hsv = useRef(hexToHsv(defaultColor));
    const box = useRef();
    const selection = useRef();
    const slider = useRef();
    const sliderSelection = useRef();
    const offsetLeft = 3; //left offset from edge of color gradient
    const offsetWidth = 0.94; //right / bottom offset from edge of color gradient

    //functions
    const getGradientBoxColor = () => '#' + hsvToHex({ h: hsv.current.h, s: 100, v: 100 });
    const getSelectionTop = () => ((100 - hsv.current.v * offsetWidth) - offsetLeft) + '%';
    const getSelectionLeft = () => ((hsv.current.s * offsetWidth) + offsetLeft) + '%';
    const getSliderTop = () => ((((offsetWidth * 100) / 360.0) * hsv.current.h) + offsetLeft) + '%';
    const getSliderColor = () => '#' + hsvToHex({ h: hsv.current.h, s: 100, v: 100 });
    const updateColorFromHsv = () => {
        const hex = hsvToHex(hsv.current);
        color.current = hex;
        box.current.style.background = getGradientBoxColor();
        selection.current.style.top = getSelectionTop();
        selection.current.style.left = getSelectionLeft();
        selection.current.style.background = '#' + hex;
        sliderSelection.current.style.top = getSliderTop();
        sliderSelection.current.style.background = getSliderColor();
    }

    //actions
    const handleMouseDownSelection = () => {
        document.body.addEventListener('mousemove', handleMouseMoveSelection);
        document.body.addEventListener('mouseup', handleMouseUpSelection);
    }

    const handleMouseMoveSelection = (e) => {
        const rect = box.current.getBoundingClientRect();
        let x = e.clientX - rect.x;
        let y = e.clientY - rect.y;
        if (x < 0) x = 0;
        if (x > rect.width) x = rect.width;
        if (y < 0) y = 0;
        if (y > rect.height) y = rect.height;
        const s = (100 / rect.width) * x; //saturation
        const v = 100 - (100 / rect.height) * y;
        hsv.current = { h: hsv.current.h, s: s, v: v };
        updateColorFromHsv();
        if (typeof onChange == 'function') onChange(color.current);
    }

    const handleMouseUpSelection = () => {
        document.body.removeEventListener('mousemove', handleMouseMoveSelection);
        document.body.removeEventListener('mouseup', handleMouseUpSelection);
        if (saved.current != color.current) {
            if (typeof onChangeComplete == 'function') onChangeComplete(color.current);
            saved.current = color.current;
        }
    }

    const handleMouseDownSlider = () => {
        document.body.addEventListener('mousemove', handleMouseMoveSlider);
        document.body.addEventListener('mouseup', handleMouseUpSlider);
    }

    const handleMouseMoveSlider = (e) => {
        const rect = slider.current.getBoundingClientRect();
        let y = e.clientY - rect.y;
        if (y < 0) y = 0;
        if (y > rect.height) y = rect.height;
        const h = (360.0 / rect.height) * y;
        hsv.current = { h: h, s: hsv.current.s, v: hsv.current.v };
        updateColorFromHsv();
        if (typeof onChange == 'function') onChange(color.current);
    }

    const handleMouseUpSlider = () => {
        document.body.removeEventListener('mousemove', handleMouseMoveSlider);
        document.body.removeEventListener('mouseup', handleMouseUpSlider);
        if (saved.current != color.current) {
            if (typeof onChangeComplete == 'function') onChangeComplete(color.current);
            saved.current = color.current;
        }
    }

    return (
        <div className="color-picker">
            <div
                className="gradient-box"
                ref={box}
                onMouseDown={handleMouseDownSelection}
                style={{ background: getGradientBoxColor() }}
            >
                <div className="white-gradient"></div>
                <div className="black-gradient"></div>
                <div
                    className="selection"
                    ref={selection}
                    style={{
                        top: getSelectionTop(),
                        left: getSelectionLeft(),
                        background: '#' + color.current
                    }}
                ></div>
            </div>
            <div
                className="gradient-slider"
                ref={slider}
                onMouseDown={handleMouseDownSlider}
            >
                <div
                    className="selection"
                    ref={sliderSelection}
                    style={{
                        top: getSliderTop(),
                        background: getSliderColor()
                    }}
                ></div>
            </div>
        </div>
    );
}