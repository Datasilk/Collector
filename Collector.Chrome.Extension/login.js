// Login page script for Collector Cookie Bridge

document.addEventListener('DOMContentLoaded', async () => {
  const form = document.getElementById('loginForm');
  const emailInput = document.getElementById('email');
  const passwordInput = document.getElementById('password');
  const serverUrlInput = document.getElementById('serverUrl');
  const submitBtn = document.getElementById('submitBtn');
  const errorEl = document.getElementById('error');
  const successEl = document.getElementById('success');

  // Load saved server URL
  const stored = await chrome.storage.local.get(['serverUrl']);
  if (stored.serverUrl) {
    serverUrlInput.value = stored.serverUrl;
  } else {
    // Default to localhost for development
    serverUrlInput.value = 'https://localhost:7001';
  }

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const email = emailInput.value.trim();
    const password = passwordInput.value;
    const serverUrl = serverUrlInput.value.trim().replace(/\/$/, ''); // Remove trailing slash

    if (!email || !password || !serverUrl) {
      showError('Please fill in all fields');
      return;
    }

    submitBtn.disabled = true;
    submitBtn.textContent = 'Signing in...';
    hideMessages();

    try {
      // Save server URL for future logins
      await chrome.storage.local.set({ serverUrl });

      // Authenticate with Collector API
      const response = await fetch(`${serverUrl}/api/auth/login`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          username: email,
          password: password
        })
      });

      const result = await response.json();

      if (result.success && result.data) {
        // Notify background script of successful authentication
        await chrome.runtime.sendMessage({ 
          type: 'AUTH_SUCCESS',
          data: {
            appUserId: result.data.appUserId,
            displayName: result.data.displayName,
            email: email
          }
        });

        showSuccess('Login successful!');
        
        // Close tab after short delay
        setTimeout(() => {
          window.close();
        }, 1500);
      } else {
        showError(result.message || 'Invalid email or password');
      }
    } catch (err) {
      console.error('Login error:', err);
      showError(`Connection failed: ${err.message}`);
    } finally {
      submitBtn.disabled = false;
      submitBtn.textContent = 'Sign In';
    }
  });

  function showError(message) {
    errorEl.textContent = message;
    errorEl.style.display = 'block';
    successEl.style.display = 'none';
  }

  function showSuccess(message) {
    successEl.textContent = message;
    successEl.style.display = 'block';
    errorEl.style.display = 'none';
  }

  function hideMessages() {
    errorEl.style.display = 'none';
    successEl.style.display = 'none';
  }
});
