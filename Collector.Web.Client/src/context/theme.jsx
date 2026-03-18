import React, { createContext, useContext, useState, useEffect } from 'react';

const ThemeContext = createContext();

export const ThemeProvider = ({ children }) => {
  const [theme, setTheme] = useState(() => {
    const savedTheme = localStorage.getItem('collector:theme');
    return savedTheme || 'light';
  });

  useEffect(() => {
    // Save theme to localStorage
    localStorage.setItem('collector:theme', theme);
    
    // Update body className
    document.body.className = theme;
    
    // Dynamically load the appropriate theme CSS file
    const existingThemeLink = document.getElementById('theme-stylesheet');
    if (existingThemeLink) {
      existingThemeLink.remove();
    }
    
    const link = document.createElement('link');
    link.id = 'theme-stylesheet';
    link.rel = 'stylesheet';
    link.href = `/src/styles/${theme}.css`;
    document.head.appendChild(link);
  }, [theme]);

  const toggleTheme = () => {
    setTheme(prevTheme => prevTheme === 'light' ? 'dark' : 'light');
  };

  return (
    <ThemeContext.Provider value={{ theme, setTheme, toggleTheme }}>
      {children}
    </ThemeContext.Provider>
  );
};

export const useTheme = () => {
  const context = useContext(ThemeContext);
  if (!context) {
    throw new Error('useTheme must be used within a ThemeProvider');
  }
  return context;
};
