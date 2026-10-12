(() => {
    'use strict';
    // Existing Lab/Billing navigation already provides its own accessible drawer.
    const sidebar = document.querySelector('.sidebar, .meth-sidebar');
    const header = document.querySelector('.top-header, .meth-top-header');
    if (!sidebar || !header) return;
    if (!sidebar.id) sidebar.id = 'agro-navigation';
    const toggle = document.createElement('button');
    toggle.type = 'button'; toggle.className = 'btn btn-outline-secondary agro-menu-toggle';
    toggle.textContent = 'Menu'; toggle.setAttribute('aria-controls', sidebar.id);
    toggle.setAttribute('aria-expanded', 'false');
    const close = document.createElement('button');
    close.type = 'button'; close.className = 'btn agro-menu-close'; close.textContent = 'Close menu';
    const backdrop = document.createElement('button');
    backdrop.type = 'button'; backdrop.className = 'agro-backdrop'; backdrop.hidden = true;
    backdrop.setAttribute('aria-label', 'Close navigation');
    header.prepend(toggle); sidebar.prepend(close); document.body.append(backdrop);
    document.body.classList.add('agro-nav-ready');
    const mobile = window.matchMedia('(max-width: 991px)');
    function setOpen(open, restoreFocus = true) {
        document.body.classList.toggle('agro-nav-open', open);
        toggle.setAttribute('aria-expanded', String(open)); backdrop.hidden = !open;
        if (open) close.focus(); else if (restoreFocus && mobile.matches) toggle.focus();
    }
    toggle.addEventListener('click', () => setOpen(true));
    close.addEventListener('click', () => setOpen(false));
    backdrop.addEventListener('click', () => setOpen(false));
    document.addEventListener('keydown', e => {
        if (!document.body.classList.contains('agro-nav-open') || document.querySelector('dialog[open]')) return;
        if (e.key === 'Escape') { setOpen(false); return; }
        if (e.key === 'Tab') {
            const nodes = [...sidebar.querySelectorAll('a[href], button, input, select, [tabindex="0"]')].filter(n => n.getClientRects().length && !n.disabled);
            const first = nodes[0], last = nodes[nodes.length - 1];
            if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
            else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
        }
    });
    mobile.addEventListener('change', () => setOpen(false, false));
})();
