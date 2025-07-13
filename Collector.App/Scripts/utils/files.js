ui.utils.addStyleSheet = (id, url) => {
    const link = document.createElement('link');
    link.rel = 'stylesheet';
    link.src = url;
    link.id = id;
    document.querySelector('head').appendChild(link);
};

ui.utils.loadJsFile = (id, url) => {
    const js = document.createElement('script');
    js.src = url;
    js.id = id;
    document.querySelector('body').appendChild(js);
};

ui.utils.injectJs = (id, sourcecode) => {
    const js = document.createElement('script');
    js.id = id;
    js.innerText = sourcecode;
    document.querySelector('body').appendChild(js);
};