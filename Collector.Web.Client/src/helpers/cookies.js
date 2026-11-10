/**
 * Helper for communicating with the Collector Cookie Bridge Chrome extension
 * to retrieve cookies for authenticated requests (e.g., YouTube downloads)
 */

import React from 'react';
import ExtensionIdModal from './extension-id-modal';

// localStorage key holding the Chrome extension ID
const EXTENSION_ID_STORAGE_KEY = 'Collector:ExtentionId';

// dedupe concurrent extension-id prompts so only one modal shows at a time
let extensionIdPrompt = null;

function getStoredExtensionId() {
    return localStorage.getItem(EXTENSION_ID_STORAGE_KEY);
}

function promptForExtensionId(session) {
    if (!session || typeof session.showModal !== 'function') {
        return Promise.resolve(null);
    }
    if (!extensionIdPrompt) {
        extensionIdPrompt = new Promise((resolve) => {
            const finish = (id) => {
                extensionIdPrompt = null;
                session.hideModal();
                resolve(id);
            };
            const handleSave = (id) => {
                localStorage.setItem(EXTENSION_ID_STORAGE_KEY, id);
                finish(id);
            };
            const handleCancel = () => finish(null);
            session.showModal(() => React.createElement(ExtensionIdModal, {
                onSave: handleSave,
                onCancel: handleCancel
            }));
        });
    }
    return extensionIdPrompt;
}

/**
 * Check if the Chrome extension messaging is available
 * Note: chrome.runtime.sendMessage with extension ID is available to web pages
 * when the extension has externally_connectable configured. If no extension ID
 * is stored, the user is prompted to enter it before continuing.
 * @param {object} session - The session context (provides showModal/hideModal)
 * @returns {Promise<boolean>} True if the extension messaging API is available and an extension ID is set
 */
export async function isExtensionAvailable(session) {
    const extensionId = getStoredExtensionId() || await promptForExtensionId(session);
    const available = typeof chrome !== 'undefined' &&
        chrome.runtime &&
        typeof chrome.runtime.sendMessage === 'function' &&
        !!extensionId;
    console.log('isExtensionAvailable:', available, 'chrome:', typeof chrome, 'runtime:', chrome?.runtime);
    return available;
}

/**
 * Send a message to the Chrome extension and wait for a response
 * @param {object} session - The session context (provides showModal/hideModal)
 * @param {object} message - The message to send
 * @param {number} timeoutMs - Timeout in milliseconds (default 10000)
 * @returns {Promise<object>} The response from the extension
 */
async function sendExtensionMessage(session, message, timeoutMs = 10000) {
    if (!await isExtensionAvailable(session)) {
        throw new Error('Chrome extension not available');
    }
    const extensionId = getStoredExtensionId();

    return new Promise((resolve, reject) => {
        const timeout = setTimeout(() => {
            reject(new Error('Extension request timed out'));
        }, timeoutMs);

        try {
            chrome.runtime.sendMessage(extensionId, message, (response) => {
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
 * @param {object} session - The session context (provides showModal/hideModal)
 * @returns {Promise<Array>} Array of cookie objects
 */
export async function getCookiesForDomain(domain, session) {
    console.log('getCookiesForDomain called for:', domain);
    try {
        const response = await sendExtensionMessage(session, {
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
 * @param {object} session - The session context (provides showModal/hideModal)
 * @returns {Promise<object>} Extension state including authentication status
 */
export async function getExtensionState(session) {
    try {
        const response = await sendExtensionMessage(session, {
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
 * @param {object} session - The session context (provides showModal/hideModal)
 * @returns {Promise<string>} Netscape format cookie string for YouTube
 */
export async function getYouTubeCookies(session) {
    const cookies = await getCookiesForDomain('youtube.com', session);
    return cookiesToNetscapeFormat(cookies);
}
