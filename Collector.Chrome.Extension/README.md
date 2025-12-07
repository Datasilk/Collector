# Collector Cookie Bridge - Chrome Extension

This Chrome extension provides browser cookies to the Collector application for authenticated downloads (e.g., YouTube videos requiring login).

## Setup

1. **Add Extension Icons** (optional)
   Place icon files (16x16, 48x48, 128x128 PNG) in the `icons/` folder.

2. **Load Extension in Chrome**
   1. Open Google Chrome
   2. Type `chrome://extensions/` in the address bar and press Enter
   3. Enable **Developer mode** by clicking the toggle switch in the top-right corner
   4. Click the **Load unpacked** button that appears
   5. In the file browser, navigate to and select the `Collector.Chrome.Extension` folder
   6. The extension should now appear in your extensions list
   7. Click the puzzle piece icon (Extensions) in Chrome's toolbar
   8. Click the pin icon next to "Collector Cookie Bridge" to keep it visible in your toolbar

## Usage

1. Click the extension icon in Chrome's toolbar
2. Click "Authenticate" to log in with your Collector credentials
3. Once authenticated, the extension will provide cookies when requested by the Collector React app

## How It Works

1. **Authentication**: The extension authenticates against the Collector API using your email and password
2. **External Messaging**: The Collector React app communicates with the extension via Chrome's external messaging API
3. **Cookie Requests**: When a Collector worker needs cookies for a domain (e.g., youtube.com), the React app requests them from the extension
4. **Cookie Response**: The extension retrieves cookies from Chrome and sends them back to the React app, which forwards them to the worker

## Security

- Credentials are only sent to your configured Collector server
- Authentication state is stored in Chrome's local storage (extension-only access)
- Cookies are only sent to origins configured in `externally_connectable` (localhost and collector.ai)
- The extension only has access to cookies, not page content

## Troubleshooting

- **Authentication Failed**: Verify your email and password are correct
- **Cookies Not Working**: Ensure you're logged into the target site (e.g., YouTube) in Chrome
- **Extension Not Responding**: Try reloading the extension from `chrome://extensions/`
