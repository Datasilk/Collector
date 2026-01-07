import React from 'react';
import './layout.css';

function FeedsLayout({ children }) {
  return (
    <div className="feeds">
        {children}
    </div>
  );
}

export default FeedsLayout;
