/**
 * Helper for communicating with the Collector Cookie Bridge Chrome extension
 * to retrieve cookies for authenticated requests (e.g., YouTube downloads)
 */

// Chrome extension ID - update this after installing the extension
const EXTENSION_ID = 'omhamibneeckmnjdbbknljgfaagdhjhh';

/**
 * Check if the Chrome extension messaging is available
 * Note: chrome.runtime.sendMessage with extension ID is available to web pages
 * when the extension has externally_connectable configured
 * @returns {boolean} True if the extension messaging API is available
 */
export function isExtensionAvailable() {
    const available = typeof chrome !== 'undefined' &&
        chrome.runtime &&
        typeof chrome.runtime.sendMessage === 'function';
    console.log('isExtensionAvailable:', available, 'chrome:', typeof chrome, 'runtime:', chrome?.runtime);
    return available;
}

/**
 * Send a message to the Chrome extension and wait for a response
 * @param {object} message - The message to send
 * @param {number} timeoutMs - Timeout in milliseconds (default 10000)
 * @returns {Promise<object>} The response from the extension
 */
function sendExtensionMessage(message, timeoutMs = 10000) {
    return new Promise((resolve, reject) => {
        if (!isExtensionAvailable()) {
            reject(new Error('Chrome extension not available'));
            return;
        }

        const timeout = setTimeout(() => {
            reject(new Error('Extension request timed out'));
        }, timeoutMs);

        try {
            chrome.runtime.sendMessage(EXTENSION_ID, message, (response) => {
                clearTimeout(timeout);

                if (chrome.runtime.lastError) {
                    reject(new Error(chrome.runtime.lastError.message || 'Extension communication failed'));
                    return;
                }

                resolve(response);
            });
        } catch (error) {
            clearTimeout(timeout);
            reject(error);
        }
    });
}

/**
 * Request cookies for a specific domain from the Chrome extension
 * @param {string} domain - The domain to get cookies for (e.g., 'youtube.com')
 * @returns {Promise<Array>} Array of cookie objects
 */
export async function getCookiesForDomain(domain) {
    console.log('getCookiesForDomain called for:', domain);
    try {
        const response = await sendExtensionMessage({
            type: 'GET_COOKIES',
            domain: domain
        });
        console.log('getCookiesForDomain response:', response);

        if (response && response.success && response.cookies) {
            return response.cookies;
        }

        return [];
    } catch (error) {
        console.error('Failed to get cookies from extension:', error);
        return [];
    }
}

/**
 * Convert cookies array to Netscape cookie file format
 * @param {Array} cookies - Array of cookie objects from the extension
 * @returns {string} Netscape format cookie file content
 */
export function cookiesToNetscapeFormat(cookies) {
    if (!cookies || cookies.length === 0) {
        return '';
    }

    const lines = ['# Netscape HTTP Cookie File'];

    for (const cookie of cookies) {
        const domain = cookie.domain.startsWith('.') ? cookie.domain : `.${cookie.domain}`;
        const includeSubdomains = domain.startsWith('.') ? 'TRUE' : 'FALSE';
        const path = cookie.path || '/';
        const secure = cookie.secure ? 'TRUE' : 'FALSE';
        const expiry = cookie.expirationDate ? Math.floor(cookie.expirationDate) : 0;
        const name = cookie.name;
        const value = cookie.value;

        lines.push(`${domain}\t${includeSubdomains}\t${path}\t${secure}\t${expiry}\t${name}\t${value}`);
    }

    return lines.join('\n');
}

/**
 * Check if the Chrome extension is connected and authenticated
 * @returns {Promise<object>} Extension state including authentication status
 */
export async function getExtensionState() {
    try {
        const response = await sendExtensionMessage({
            type: 'GET_STATE'
        });

        return response || { isAuthenticated: false, hubConnected: false };
    } catch (error) {
        console.error('Failed to get extension state:', error);
        return { isAuthenticated: false, hubConnected: false, error: error.message };
    }
}

/**
 * Get YouTube cookies specifically, formatted for yt-dlp
 * @returns {Promise<string>} Netscape format cookie string for YouTube
 */
export async function getYouTubeCookies() {
    const cookies = await getCookiesForDomain('youtube.com');
    return cookiesToNetscapeFormat(cookies);
}
