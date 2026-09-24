(() => {
    const category = document.getElementById('mapping-category');
    const item = document.getElementById('mapping-item');
    const options = Array.from(item.options).map(o => o.cloneNode(true));
    const mappings = JSON.parse(document.getElementById('mapping-data').textContent);
    const checks = Array.from(document.querySelectorAll('.mapping-test'));
    const original = document.getElementById('mapping-original-tests');
    const toggle = document.getElementById('mapping-tests-toggle');
    function summary() {
        const selected = checks.filter(c => c.checked);
        toggle.textContent = selected.length ? `${selected.length} tests selected` : 'Select tests';
        toggle.disabled = !item.value;
        document.getElementById('mapping-selection-summary').textContent = !item.value ? 'Select an item first.' : selected.length ? selected.map(c => document.querySelector(`label[for="${c.id}"]`).textContent.trim()).join(', ') : 'No tests selected. Saving will clear this item’s active test mappings.';
    }
    function loadSelection() {
        const ids = new Set(mappings.filter(m => String(m.CategoryId) === category.value && String(m.ItemId) === item.value).map(m => String(m.TestId)));
        checks.forEach(c => { c.checked = ids.has(c.value); });
        original.replaceChildren(...Array.from(ids).map(id => {
            const input = document.createElement('input');
            input.type = 'hidden'; input.name = 'Form.OriginalTestIds'; input.value = id;
            return input;
        }));
        summary();
    }
    function filter() {
        const current = item.value;
        item.replaceChildren(...options.filter(o => !o.value || o.dataset.category === category.value).map(o => o.cloneNode(true)));
        item.value = Array.from(item.options).some(o => o.value === current) ? current : '';
    }
    category.addEventListener('change', () => { filter(); loadSelection(); });
    item.addEventListener('change', loadSelection);
    checks.forEach(c => c.addEventListener('change', summary));
    document.getElementById('mapping-form').addEventListener('submit', e => {
        if (original.children.length && !checks.some(c => c.checked) && !confirm('Remove all active test mappings for this item? Saved reports will remain unchanged.')) e.preventDefault();
    });
    // Keep server-rendered selections on edit and on a validation error.
    filter(); summary();
})();