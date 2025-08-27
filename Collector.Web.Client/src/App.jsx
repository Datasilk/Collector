/**
 * <summary>App Root Component</summary>
 * <description>The main entry point for the application. Sets up global providers including session context and routing.</description>
 */
import React, { useEffect, useRef } from 'react';
import './styles/App.css';
import { BrowserRouter as Router } from 'react-router-dom';
import Routing from './routes/routing';
import { SessionProvider } from './context/session';

function App() {
  //ref
  const scrollRef = useRef(0);

  useEffect(() => {
    window.addEventListener('resize', handleWindowResize);

    window.addEventListener('scroll', () => {
      if (window.scrollY > 0 && scrollRef.current == 0) {
        scrollRef.current = window.scrollY;
        document.body.classList.add('scrolled');
      } else if (window.scrollY <= 0) {
        scrollRef.current = 0;
        document.body.classList.remove('scrolled');
      }
    });
    handleWindowResize();
  }, []);

  const handleWindowResize = () => {
    if(window.innerWidth <= 1024){
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