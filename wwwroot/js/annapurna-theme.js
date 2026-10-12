(() => {
    'use strict';
    const key = 'annapurna.appearance.accent';
    const fallback = '#087f78';
    const valid = value => /^#[0-9a-f]{6}$/i.test(value || '');
    function apply(value) {
        const color = valid(value) ? value : fallback;
        const rgb = [1, 3, 5].map(i => parseInt(color.slice(i, i + 2), 16));
        const luminance = rgb.map(v => v / 255).map(v => v <= .04045 ? v / 12.92 : ((v + .055) / 1.055) ** 2.4).reduce((sum, v, i) => sum + v * [.2126, .7152, .0722][i], 0);
        const root = document.documentElement.style;
        root.setProperty('--agro-accent', color);
        root.setProperty('--agro-on-accent', luminance > .179 ? '#10232c' : '#ffffff');
        root.setProperty('--agro-accent-rgb', rgb.join(','));
        const picker = document.getElementById('agro-color');
        if (picker) picker.value = color;
        const output = document.getElementById('agro-color-value');
        if (output) output.textContent = color.toUpperCase();
        document.querySelectorAll('[data-theme-color]').forEach(button => button.setAttribute('aria-pressed', String(button.dataset.themeColor === color.toLowerCase())));
        return color;
    }
    let initial = fallback;
    try { initial = localStorage.getItem(key) || fallback; } catch { /* Storage is optional. */ }
    apply(initial);
    document.addEventListener('DOMContentLoaded', () => {
        apply(initial);
        const dialog = document.getElementById('agro-appearance');
        if (!dialog) return;
        const sidebar = document.querySelector('.sidebar, .meth-sidebar, .lab-sidebar');
        const trigger = document.createElement('button');
        trigger.type = 'button'; trigger.className = 'agro-settings-trigger';
        trigger.textContent = '⚙  Appearance settings'; trigger.setAttribute('aria-haspopup', 'dialog');
        trigger.setAttribute('aria-controls', dialog.id);
        if (sidebar) sidebar.append(trigger);
        else { trigger.classList.add('agro-settings-floating'); document.body.append(trigger); }
        trigger.addEventListener('click', () => dialog.showModal());
        dialog.querySelector('[data-close-appearance]').addEventListener('click', () => dialog.close());
        dialog.addEventListener('click', event => { if (event.target === dialog) { const r = dialog.getBoundingClientRect(); if (event.clientX < r.left || event.clientX > r.right || event.clientY < r.top || event.clientY > r.bottom) dialog.close(); } });
        function save(value) {
            const color = apply(value);
            try { localStorage.setItem(key, color); document.getElementById('agro-theme-status').textContent = 'Saved for this browser. Print documents keep their original colors.'; }
            catch { document.getElementById('agro-theme-status').textContent = 'Color applied. Browser storage is unavailable, so this choice lasts for this page only.'; }
        }
        document.getElementById('agro-color').addEventListener('input', event => save(event.target.value));
        dialog.querySelectorAll('[data-theme-color]').forEach(button => button.addEventListener('click', () => save(button.dataset.themeColor)));
        document.getElementById('agro-theme-reset').addEventListener('click', () => save(fallback));
    });
    window.addEventListener('storage', event => { if (event.key === key || event.key === null) apply(event.newValue || fallback); });
})();
