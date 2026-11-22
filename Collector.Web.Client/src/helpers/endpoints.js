// Loads /api-base-path.json synchronously and caches the resolved base
// URL on window.apiBasePath - the JSON file is required, not optional.
const loadBasePaths = () => {
    const xhr = new XMLHttpRequest();
    xhr.open('GET', '/api-base-path.json', false);
    xhr.send(null);
    const paths = JSON.parse(xhr.responseText);
    const href = window.location.href;
    if (href.indexOf('http://localhost') >= 0) {
        window.apiBasePath = paths.http;
    } else if (href.indexOf('https://localhost') >= 0) {
        window.apiBasePath = paths.https;
    } else {
        // non-localhost: same hostname as the page, configured port
        const url = new URL(href);
        const configured = url.protocol === 'https:' ? paths.https : paths.http;
        window.apiBasePath = `${url.protocol}//${url.hostname}:${new URL(configured).port}`;
    }
}

const apiBasePath = () => {
    if (!window.apiBasePath) loadBasePaths();
    return window.apiBasePath;
}

export { apiBasePath }
