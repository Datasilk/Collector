(() => {
    const ui = {
        utils: {}
    };

    ui.ajax = function ({ url, data, complete, error, json, async, contentType, method, username, password }) {
    var opt = {
        method: method ?? 'GET',
        data: JSON.stringify(data),
        url: url,
        async: async,
        username: username,
        password: password,
        contentType: contentType ?? 'text/plain; charset=utf-8',
        dataType: json ? 'json' : 'html',
        success: function (xhr) {
            if (typeof complete == 'function') { complete(xhr); }
        },
        error: function (xhr) {
            if (typeof error == 'function') { error(xhr); }
        }
    }

    //set up AJAX request
    var req = new XMLHttpRequest();

    //set up callbacks
    req.onload = function () {
        if (req.status >= 200 && req.status < 400) {
            //request success
            if (opt.success) opt.success(req);
        } else {
            //connected to server, but returned an error
            if (opt.error) opt.error(req);
        }
        ui.ajax.wait = false;
        ui.ajax.runQueue();
    };

    req.onerror = function () {
        //an error occurred before connecting to server
        if (opt.error) opt.error(req);
        ui.ajax.wait = false;
        ui.ajax.runQueue();
    };

    //finally, add AJAX request to queue
    ui.ajax.queue.unshift({ req: req, opt: opt });
    ui.ajax.runQueue();
};

ui.ajax.runQueue = () => {
    if (ui.ajax.wait === true) return;
    if (ui.ajax.queue.length == 0) return;
    ui.ajax.wait = true;
    let queue = ui.ajax.queue[ui.ajax.queue.length - 1];
    let req = queue.req;
    let opt = queue.opt;
    ui.ajax.queue.pop();
    req.open(opt.method, opt.url, opt.async, opt.username, opt.password);
    req.setRequestHeader('Content-Type', opt.contentType);
    req.send(opt.data);
};

ui.ajax.queue = [];
ui.ajax.wait = false;
ui.darkmode = { enabled: false };
ui.darkmode.load = () => {
    ui.darkmode.enabled = localStorage.getItem('darkmode') ?? false;
    ui.darkmode.toggle(ui.darkmode.enabled == 'true');
};

ui.darkmode.toggle = (on) => {
    if (on === false) {
        //light mode
        document.body.classList.remove('dark-mode');
        const elems = [...document.querySelectorAll('.toggle-dark-mode')];
        if (elems) elems.forEach(a => {
            a.querySelector('.toggle.for-darkmode').classList.remove('on');
            console.log(a, a.firstChild);
        });
        [...document.querySelectorAll('.toggle-dark-mode > span')]?.forEach(a => {
            a.innerHTML = 'Light Mode';
        })
        localStorage.setItem('darkmode', false);
        ui.darkmode.enabled = false;

    } else {
        //dark mode
        document.body.classList.add('dark-mode');
        const elems = [...document.querySelectorAll('.toggle-dark-mode')];
        if (elems) elems.forEach(a => {
            a.querySelector('.toggle.for-darkmode').classList.add('on');
            console.log(a, a.firstChild);
        });
        [...document.querySelectorAll('.toggle-dark-mode > span')]?.forEach(a => {
            a.innerHTML = 'Dark Mode';
        })
        localStorage.setItem('darkmode', true);
        ui.darkmode.enabled = true;
    }
};

let dashHub = null;
ui.hub = {};

ui.hub.load = () => {
    var consl = document.querySelector('.console');
    if (consl.className.indexOf('show') >= 0) {
        //hide console
        consl.classList.remove('show');
        consl.classList.add('hide');
        //dashHub.stop();
    } else {
        //show console and load SignalR hub
        consl.classList.remove('hide');
        consl.classList.add('show');
        if (dashHub == null) {
            dashHub = new signalR.HubConnectionBuilder().withUrl('/dashboardhub').build();
            dashHub.on('update', ui.hub.log);
            dashHub.start().catch(ui.hub.error);
            setTimeout(() => { dashHub.invoke('handshake'); }, 500);
        }
    }
};

ui.hub.error = (e) => {
    console.log(e);
};

ui.hub.log = (msg) => {
    var div = document.createElement("div");
    div.innerHTML = msg;
    document.querySelectorAll('.console .scrollable')[0].appendChild(div);
}
ui.nav = {

};
ui.toggle = {};
ui.toggle.flip = (elem, callback) => {
    if (elem.classList.contains('on')) {
        elem.classList.remove('on');
        if (callback) callback(false);
    } else {
        elem.classList.add('on');
        if (callback) callback(true);
    }
}; 
class DarkModeToggle extends HTMLElement {
    constructor() {
        super();
    }

    connectedCallback() {
        this.innerHTML = `
          <div class="toggle-dark-mode">
            <span>Dark Mode</span>
            <div class="toggle for-darkmode">
                <div class="switch">
                    <span class="light material-symbols-outlined">light_mode</span>
                    <span class="dark material-symbols-outlined">dark_mode</span>
                </div>
            </div>
        </div>
        `;
    }
}

customElements.define('darkmode-toggle', DarkModeToggle);
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
//load SVG files for logo & icons
var svg = document.createElement('div');
svg.classList.add('svg-assets');
document.body.append(svg);
ui.ajax({
    url: '/images/collector-logo.svg',
    complete: (response) => {
        svg.innerHTML += response.responseText;
    }
});
ui.ajax({
    url: '/images/icons.svg',
    complete: (response) => {
        svg.innerHTML += response.responseText;
    }
});

setTimeout(() => {
    const init = document.querySelector('.init');
    init.classList.add('fade');
    setTimeout(() => init.remove(), 1000);
}, 500);

    //load dark mode setting from local storage
    ui.darkmode.load();

    window.Collector = ui;
})();