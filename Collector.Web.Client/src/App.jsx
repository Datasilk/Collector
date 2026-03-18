/**
 * <summary>App Root Component</summary>
 * <description>The main entry point for the application. Sets up global providers including session context and routing.</description>
 */
import React, { useEffect, useRef } from 'react';
import './styles/App.css';
import { BrowserRouter as Router } from 'react-router-dom';
import Routing from './routes/routing';
import { SessionProvider } from './context/session';
import { WorkerHubProvider } from './context/workerhub';
import { VideoPiPProvider } from './context/videopip';
import { ThemeProvider } from './context/theme';
import { addSvg } from './helpers/svg';
import Chat from './components/chat/chat';

function App() {
  //ref
  const scrollRef = useRef(0);
  const timerRef = useRef(null);

  useEffect(() => {
    window.addEventListener('resize', handleWindowResize);

    window.addEventListener('scroll', () => {
      if(timerRef.current){
          clearTimeout(timerRef.current);
        }
      if (window.scrollY > 0 && scrollRef.current == 0) {
        scrollRef.current = window.scrollY;
        document.body.classList.add('scrolled');
      } else if (window.scrollY <= 0) {
        scrollRef.current = 0;
        timerRef.current = setTimeout(() => {
          document.body.classList.remove('scrolled');
        }, 250);
      }
    });
    handleWindowResize();

    //load all neccessary svgs
    addSvg('/svgs/svgfiles.svg');
  }, []);

  const handleWindowResize = () => {
    if(window.innerWidth <= 1024){
      document.body.classList.add('is-mobile');
    }else{
      document.body.classList.remove('is-mobile');
    }
  };

  return (
    <ThemeProvider>
      <Router>
        <SessionProvider>
          <WorkerHubProvider>
            <VideoPiPProvider>
              <Routing />
              <Chat />
            </VideoPiPProvider>
          </WorkerHubProvider>
        </SessionProvider>
      </Router>
    </ThemeProvider>
  );
}

export default App;