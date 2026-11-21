# Chrome Extension

[Home](README.md) > Chrome Extension

`ChromeExtensionHub` (`/chrome-extension`) is a cookie relay between
workers and a user's browser extension.

## Flow

1. The extension connects and authenticates with the user's `appUserId`;
  the hub maps `appUserId → connectionIds` (multiple extensions per user
  supported).
2. A worker needing cookies for a restricted download (e.g. members-only
  video) calls `Workers.RequestCookiesFromClientAsync(appUserId, …)`,
  which signals the user's extension.
3. The extension responds via `HandleCookieResponse(requestId, cookieData)`
  — a `TaskCompletionSource` resolves the worker's pending request.
4. `HasConnectedExtensions(appUserId)` lets callers skip the request when
  no extension is online.

Cookies for restricted content are also stored via the `Cookies` API
(`CookiesController`, `cookies-upload-modal.jsx` in the video-player).
