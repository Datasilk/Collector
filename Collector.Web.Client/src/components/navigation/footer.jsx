import React from 'react';
import './footer.css';

const Footer = () => {
  return (
    <footer className="main-footer">
      <p>&copy; {new Date().getFullYear()} Collector. All rights reserved.</p>
    </footer>
  );
};

export default Footer;
