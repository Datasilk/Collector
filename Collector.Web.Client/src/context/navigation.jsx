import React, { createContext, useContext } from 'react';

const NavigationContext = createContext({
  onToggleSidebar: null,
});

export const NavigationProvider = ({ children, onToggleSidebar }) => {
  return (
    <NavigationContext.Provider value={{ onToggleSidebar }}>
      {children}
    </NavigationContext.Provider>
  );
};

export const useNavigationContext = () => {
  return useContext(NavigationContext);
};
