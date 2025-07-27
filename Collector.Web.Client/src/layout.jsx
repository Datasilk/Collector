import React, { useEffect, useState } from 'react';
import { useLocation } from 'react-router-dom';
import PropTypes from 'prop-types';
import Navigation from '@/components/navigation/navigation';
import Footer from '@/components/navigation/footer';
import '@/styles/layout.css';

const Layout = ({ children }) => {
  const location = useLocation();
  const [path, setPath] = useState('');

  useEffect(() => {
    let paths = location.pathname.split('/').filter(a => a != '');
    let pathclass = 'path-' + paths[0];
    if(paths.length > 1){
        pathclass += ' ' + paths.slice(0, 2).join('-');
    }
    setPath(pathclass);
  }, [location]);

  return (
    <div className={"app-container " + path}>
      <header className="main-header">
        <Navigation />
      </header>

      <main className="main-content">
        {children}
      </main>

      <Footer />
    </div>
  );
}; 

Layout.propTypes = {
  children: PropTypes.node.isRequired,
};

export default Layout;