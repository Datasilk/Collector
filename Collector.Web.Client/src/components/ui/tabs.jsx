import React, { useState, Children } from "react";

export default function Tabs({ children, tabs, selectedIndex }) {
    const [tabIndex, setIndex] = useState(selectedIndex ?? 0);

    const selectTab = (index) => {
        setIndex(index);
    };

    return (
        <div className="tabs">
            <div className="tabs-row">
                {tabs.map((tab, index) => (
                    <div key={tab} className={'tab-label' + (index == tabIndex ? ' selected' : '')}
                        onClick={() => {selectTab(index)}}
                    >
                        {tab}
                    </div>
                ))}
            </div>
            <div className="tab-content">{[Children.toArray(children)[tabIndex]]}</div>
        </div>
    )
}