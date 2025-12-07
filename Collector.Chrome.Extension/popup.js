// Popup script for Collector Cookie Bridge

document.addEventListener('DOMContentLoaded', async () => {
  const statusEl = document.getElementById('status');
  const userInfoEl = document.getElementById('userInfo');
  const authBtn = document.getElementById('authBtn');
  const logoutBtn = document.getElementById('logoutBtn');
  const logsBtn = document.getElementById('logsBtn');
  const logsContainer = document.getElementById('logsContainer');
  const logsEl = document.getElementById('logs');

  let logsVisible = false;

  // Get current state from background
  const state = await chrome.runtime.sendMessage({ type: 'GET_STATE' });
  updateUI(state);

  // Load and display logs
  await loadLogs();

  authBtn.addEventListener('click', async () => {
    // Open login page in new tab
    chrome.tabs.create({ url: chrome.runtime.getURL('login.html') });
    window.close();
  });

  logoutBtn.addEventListener('click', async () => {
    await chrome.runtime.sendMessage({ type: 'LOGOUT' });
    const newState = await chrome.runtime.sendMessage({ type: 'GET_STATE' });
    updateUI(newState);
  });

  logsBtn.addEventListener('click', () => {
    logsVisible = !logsVisible;
    logsContainer.classList.toggle('visible', logsVisible);
    logsBtn.textContent = logsVisible ? 'Hide Logs' : 'View Logs';
    if (logsVisible) {
      // Scroll to bottom when showing logs
      logsEl.scrollTop = logsEl.scrollHeight;
    }
  });

  // Listen for state changes and log updates
  chrome.runtime.onMessage.addListener((message) => {
    if (message.type === 'STATE_CHANGED') {
      updateUI(message.state);
    } else if (message.type === 'LOG_ENTRY') {
      appendLogEntry(message.entry);
    }
  });

  async function loadLogs() {
    try {
      const result = await chrome.runtime.sendMessage({ type: 'GET_LOGS' });
      if (result && result.logs && result.logs.length > 0) {
        logsEl.innerHTML = '';
        result.logs.forEach(entry => appendLogEntry(entry));
      } else {
        logsEl.innerHTML = '<div class="log-entry"><span class="log-info">No logs yet</span></div>';
      }
    } catch (error) {
      console.error('Failed to load logs:', error);
      logsEl.innerHTML = '<div class="log-entry"><span class="log-error">Failed to load logs</span></div>';
    }
  }

  function appendLogEntry(entry) {
    const div = document.createElement('div');
    div.className = 'log-entry';
    
    const time = new Date(entry.timestamp).toLocaleTimeString();
    let typeClass = 'log-info';
    if (entry.type === 'error') typeClass = 'log-error';
    else if (entry.type === 'cookie') typeClass = 'log-cookie';
    
    div.innerHTML = `<span class="log-time">[${time}]</span> <span class="${typeClass}">${escapeHtml(entry.message)}</span>`;
    logsEl.appendChild(div);
    
    // Auto-scroll to bottom if logs are visible
    if (logsVisible) {
      logsEl.scrollTop = logsEl.scrollHeight;
    }
    
    // Limit log entries to prevent memory issues
    while (logsEl.children.length > 100) {
      logsEl.removeChild(logsEl.firstChild);
    }
  }

  function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
  }

  function updateUI(state) {
    if (state.isAuthenticated) {
      authBtn.style.display = 'none';
      logoutBtn.style.display = 'block';
      userInfoEl.style.display = 'block';
      userInfoEl.textContent = `Logged in as: ${state.displayName || state.email || 'User'}`;
      statusEl.className = 'status connected';
      statusEl.textContent = 'Ready to provide cookies';
    } else {
      authBtn.style.display = 'block';
      logoutBtn.style.display = 'none';
      userInfoEl.style.display = 'none';
      statusEl.className = 'status disconnected';
      statusEl.textContent = 'Not authenticated';
    }
  }
});
