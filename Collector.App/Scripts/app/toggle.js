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