const apiBasePath = () => {
    if (window.location.href.indexOf('http://localhost') >= 0) {
        return 'http://localhost:7780';
    }
    if (window.location.href.indexOf('https://localhost') >= 0) {
        return 'https://localhost:7781';
    }
    
    // For non-localhost URLs, use the same hostname but with specific ports
    const url = new URL(window.location.href);
    const port = url.protocol === 'https:' ? '7781' : '7780';
    return `${url.protocol}//${url.hostname}:${port}`;
}

export { apiBasePath }