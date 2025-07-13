const toggle = document.querySelector('.toggle.for-darkmode');
if (toggle) {
    toggle.addEventListener('click', () => ui.toggle.flip(toggle, (on) => {
        ui.darkmode.toggle(on);
    }));
}