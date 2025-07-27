/**
 * <summary>App Root Component</summary>
 * <description>The main entry point for the application. Sets up global providers including session context and routing.</description>
 */
import React, { useEffect } from 'react';
import './styles/App.css';
import { BrowserRouter as Router } from 'react-router-dom';
import Routing from './routes/routing';
import { SessionProvider } from './context/session';

function App() {

  useEffect(() => {
    window.addEventListener('resize', handleWindowResize);
  }, []);

  const handleWindowResize = () => {
    if(window.innerWidth <= 500){
      document.body.classList.add('is-mobile');
    }else{
      document.body.classList.remove('is-mobile');
    }
  };

  return (
    <SessionProvider>
      <Router>
        <Routing />
      </Router>
    </SessionProvider>
  );
}

export default App;