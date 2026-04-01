import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';
import Routing from './routes/routing';
import Chat from './components/chat/chat';

export default function AppContent() {
  const location = useLocation();

  useEffect(() => {
    // Update body data-path based on first URL path segment
    const pathParts = location.pathname.split('/').filter(Boolean);
    const firstPath = pathParts[0] || 'home';
    document.body.setAttribute('data-path', firstPath);
  }, [location.pathname]);

  return (
    <>
      <Routing />
      <Chat />
    </>
  );
}
