(() => {
    'use strict';
    const button = document.querySelector('[data-password-toggle]');
    const input = document.getElementById('Password');
    if (!button || !input) return;
    button.addEventListener('click', () => {
        const visible = input.type === 'password';
        input.type = visible ? 'text' : 'password';
        button.setAttribute('aria-label', visible ? 'Hide password' : 'Show password');
        button.setAttribute('aria-pressed', String(visible));
        button.querySelector('i').className = visible ? 'bi bi-eye-slash' : 'bi bi-eye';
    });
})();
