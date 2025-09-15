const apiBasePath = () => {
    if (window.location.href.indexOf('http://localhost') >= 0) {
        return 'http://localhost:7780';
    }
    if (window.location.href.indexOf('https://localhost') >= 0) {
        return 'https://localhost:7781';
    }
    
    // Extract the full origin (protocol, domain, and port) from the URL
    const url = new URL(window.location.href);
    return url.origin;
}

export { apiBasePath }