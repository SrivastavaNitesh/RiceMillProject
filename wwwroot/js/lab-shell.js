(() => {
    const toggle = document.querySelector('.lab-menu-toggle');
    const backdrop = document.querySelector('.lab-backdrop');
    const sidebar = document.getElementById('lab-sidebar');
    function setOpen(open) {
        document.body.classList.toggle('lab-menu-open', open);
        toggle.setAttribute('aria-expanded', String(open));
        backdrop.hidden = !open;
        if (open) sidebar.querySelector('a').focus();
    }
    toggle.addEventListener('click', () => setOpen(!document.body.classList.contains('lab-menu-open')));
    backdrop.addEventListener('click', () => { setOpen(false); toggle.focus(); });
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape' && document.body.classList.contains('lab-menu-open')) {
            setOpen(false); toggle.focus();
        }
    });
    matchMedia('(min-width: 992px)').addEventListener('change', () => setOpen(false));
})();
