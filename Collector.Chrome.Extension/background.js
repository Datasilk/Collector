// Background service worker for Collector Cookie Bridge
// Handles cookie requests from the React app via external messaging

const MAX_LOG_ENTRIES = 100;

// State management
let state = {
  isAuthenticated: false,
  appUserId: null,
  displayName: null,
  email: null
};

// Log storage (persisted to chrome.storage to survive service worker restarts)
let logs = [];
let logsLoaded = false;
let stateLoaded = false;

// Load logs from storage on startup
async function loadLogs() {
  if (logsLoaded) return;
  try {
    const stored = await chrome.storage.local.get(['extensionLogs']);
    if (stored.extensionLogs && Array.isArray(stored.extensionLogs)) {
      logs = stored.extensionLogs;
    }
    logsLoaded = true;
  } catch (e) {
    console.error('Failed to load logs:', e);
  }
}

// Save logs to storage
async function saveLogs() {
  try {
    await chrome.storage.local.set({ extensionLogs: logs });
  } catch (e) {
    console.error('Failed to save logs:', e);
  }
}

function addLog(type, message) {
  const entry = {
    timestamp: Date.now(),
    type: type, // 'info', 'error', 'cookie'
    message: message
  };
  
  logs.push(entry);
  
  // Limit log size
  while (logs.length > MAX_LOG_ENTRIES) {
    logs.shift();
  }
  
  // Save to storage (don't await, fire and forget)
  saveLogs();
  
  // Broadcast to popup if open
  chrome.runtime.sendMessage({ type: 'LOG_ENTRY', entry }).catch(() => {
    // Popup not open, ignore
  });
  
  // Also log to console
  console.log(`[${type}] ${message}`);
}

// Initialize on startup
chrome.runtime.onStartup.addListener(initialize);
chrome.runtime.onInstalled.addListener(initialize);

async function initialize() {
  // Clear logs on service worker restart
  logs = [];
  logsLoaded = true;
  await saveLogs();
  
  addLog('info', 'Extension initializing...');
  await loadState();
}

async function loadState() {
  if (stateLoaded) return;
  const stored = await chrome.storage.local.get([
    'isAuthenticated',
    'appUserId',
    'displayName',
    'email'
  ]);

  state.isAuthenticated = stored.isAuthenticated || false;
  state.appUserId = stored.appUserId || null;
  state.displayName = stored.displayName || null;
  state.email = stored.email || null;
  stateLoaded = true;
}

// Internal message handler (from popup, login page)
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  handleMessage(message).then(sendResponse);
  return true; // Keep channel open for async response
});

// External message handler (from React app via externally_connectable)
chrome.runtime.onMessageExternal.addListener((message, sender, sendResponse) => {
  handleExternalMessage(message, sender).then(sendResponse);
  return true; // Keep channel open for async response
});

async function handleExternalMessage(message, sender) {
  // Log external requests
  addLog('info', `External request from ${sender.origin}: ${message.type}`);
  switch (message.type) {
    case 'GET_STATE':
      return { 
        success: true,
        isAuthenticated: state.isAuthenticated
      };

    case 'GET_COOKIES':
      if (!message.domain) {
        return { success: false, error: 'Domain is required' };
      }
      try {
        const cookies = await getCookiesForDomain(message.domain);
        addLog('cookie', `Sent ${cookies.length} cookies for ${message.domain} to ${sender.origin}`);
        return { success: true, cookies: cookies };
      } catch (error) {
        addLog('error', `Failed to get cookies for ${message.domain}: ${error.message}`);
        return { success: false, error: error.message };
      }

    default:
      return { success: false, error: 'Unknown message type' };
  }
}

async function handleMessage(message) {
  // Ensure logs and state are loaded (service worker may have just woken up)
  if (!logsLoaded) {
    await loadLogs();
  }
  if (!stateLoaded) {
    await loadState();
  }
  
  switch (message.type) {
    case 'GET_STATE':
      return { ...state };

    case 'GET_LOGS':
      return { logs: [...logs] };

    case 'AUTH_SUCCESS':
      state.isAuthenticated = true;
      state.appUserId = message.data.appUserId;
      state.displayName = message.data.displayName;
      await chrome.storage.local.set({
        isAuthenticated: true,
        appUserId: message.data.appUserId,
        displayName: message.data.displayName
      });
      addLog('info', 'Authentication successful');
      broadcastStateChange();
      return { success: true };

    case 'LOGOUT':
      addLog('info', 'Logged out');
      await logout();
      return { success: true };

    default:
      return { error: 'Unknown message type' };
  }
}

async function logout() {
  // Clear state
  state = {
    isAuthenticated: false,
    appUserId: null,
    displayName: null,
    email: null
  };

  // Clear storage
  await chrome.storage.local.remove([
    'isAuthenticated',
    'appUserId',
    'displayName',
    'email'
  ]);

  broadcastStateChange();
}

async function getCookiesForDomain(domain) {
  try {
    // Get all cookies for the domain
    const cookies = await chrome.cookies.getAll({ domain: domain });
    
    // Also try with leading dot for subdomain cookies
    let allCookies = [...cookies];
    if (!domain.startsWith('.')) {
      const dotDomainCookies = await chrome.cookies.getAll({ domain: '.' + domain });
      allCookies = [...allCookies, ...dotDomainCookies];
    }

    // Remove duplicates based on name + domain + path
    const seen = new Set();
    const uniqueCookies = allCookies.filter(cookie => {
      const key = `${cookie.name}|${cookie.domain}|${cookie.path}`;
      if (seen.has(key)) return false;
      seen.add(key);
      return true;
    });

    // Convert to Netscape format compatible structure
    return uniqueCookies.map(cookie => ({
      domain: cookie.domain,
      name: cookie.name,
      value: cookie.value,
      path: cookie.path,
      secure: cookie.secure,
      httpOnly: cookie.httpOnly,
      expirationDate: cookie.expirationDate || 0,
      sameSite: cookie.sameSite
    }));
  } catch (error) {
    console.error(`Error getting cookies for ${domain}:`, error);
    return [];
  }
}

function broadcastStateChange() {
  chrome.runtime.sendMessage({ type: 'STATE_CHANGED', state: { ...state } }).catch(() => {
    // Popup might not be open, ignore error
  });
}

// Initialize immediately
initialize();
