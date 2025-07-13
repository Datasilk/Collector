(() => {
    const ui = {
        utils: {}
    };

    /*[js libraries goes here]*/

    //load dark mode setting from local storage
    ui.darkmode.load();

    window.Collector = ui;
})();