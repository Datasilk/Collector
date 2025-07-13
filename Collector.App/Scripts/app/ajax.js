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