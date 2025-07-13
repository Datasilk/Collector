
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