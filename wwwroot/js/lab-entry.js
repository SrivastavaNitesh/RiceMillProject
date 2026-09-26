(() => {
    'use strict';
    const source = document.getElementById('lab-entry-data');
    if (!source) return;
    const data = JSON.parse(source.textContent);
    const selected = new Set(data.selected);
    const values = new Map(data.results.map(r => [`${r.ItemId}:${r.TestId}`, r.Value]));
    const categories = Array.from(document.querySelectorAll('.lab-category'));
    const itemArea = document.getElementById('lab-items');
    const resultArea = document.getElementById('lab-results');
    const save = document.getElementById('save-lab');
    const message = document.getElementById('lab-selection-message');
    const node = (tag, text, classes) => {
        const el = document.createElement(tag);
        if (text !== undefined) el.textContent = text;
        if (classes) el.className = classes;
        return el;
    };
    function renderResults() {
        resultArea.replaceChildren();
        let index = 0;
        let missing = false;
        const items = data.items.filter(i => selected.has(i.ItemId));
        for (const item of items) {
            const card = node('section', undefined, 'card lab-result-card mb-3');
            const heading = node('div', undefined, 'lab-result-heading');
            const title = node('div');
            title.append(node('span', item.CategoryName, 'lab-result-category'), node('h5', item.ItemName));
            heading.append(title, node('span', item.Tests.length + ' tests', 'lab-count'));
            card.append(heading);
            const columns = node('div', undefined, 'lab-result-columns');
            columns.setAttribute('aria-hidden', 'true');
            columns.append(node('span', 'Test / observation'), node('span', 'Result'), node('span', 'Unit / reference'));
            card.append(columns);
            if (!item.Tests.length) {
                card.append(node('p', 'No tests mapped to this item. Add its test mappings before saving.', 'text-warning'));
                missing = true;
            }
            for (const test of item.Tests) {
                const row = node('div', undefined, 'lab-result-row');
                const labelCol = node('div', undefined, 'lab-result-label');
                const label = node('label', test.TestName, 'form-label fw-bold');
                label.htmlFor = `result-${item.ItemId}-${test.TestId}`;
                labelCol.append(label);
                if (test.Description) labelCol.append(node('small', test.Description, 'd-block text-muted'));
                const inputCol = node('div', undefined, 'lab-result-value');
                const group = node('div', undefined, 'input-group');
                const input = node('input', undefined, 'form-control');
                input.id = label.htmlFor;
                input.name = `Results[${index}].Value`;
                input.type = test.ResultType === 'Number' ? 'number' : 'text';
                input.required = true;
                if (input.type === 'number') input.step = '0.000001';
                else input.maxLength = 500;
                const key = `${item.ItemId}:${test.TestId}`;
                input.value = values.get(key) ?? '';
                input.addEventListener('input', () => values.set(key, input.value));
                group.append(input);
                input.placeholder = test.ResultType === 'Number' ? 'Enter value' : 'Enter result';
                inputCol.append(group);
                for (const [field, value] of Object.entries({ CategoryId: item.CategoryId, ItemId: item.ItemId, TestId: test.TestId })) {
                    const hidden = node('input');
                    hidden.type = 'hidden'; hidden.name = `Results[${index}].${field}`; hidden.value = value;
                    inputCol.append(hidden);
                }
                const reference = node('div', test.Unit || '-', 'lab-result-reference');
                row.append(labelCol, inputCol, reference); card.append(row); index++;
            }
            resultArea.append(card);
        }
        save.disabled = !items.length || missing;
        message.textContent = missing ? 'Some selected items need test mappings.' : `${items.length} item(s), ${index} result(s) in this report.`;
        if (!items.length) resultArea.append(node('p', 'Select items to display their tests.', 'text-muted'));
    }
    function renderItems() {
        const checked = new Set(categories.filter(c => c.checked).map(c => Number(c.value)));
        document.getElementById('category-menu').textContent = checked.size ? `${checked.size} ${checked.size === 1 ? 'category' : 'categories'} selected` : 'Select categories';
        const visible = data.items.filter(i => checked.has(i.CategoryId));
        for (const id of selected) if (!visible.some(i => i.ItemId === id)) selected.delete(id);
        itemArea.replaceChildren();
        if (!visible.length) itemArea.append(node('p', 'Select at least one category above.', 'text-muted'));
        for (const item of visible) {
            const col = node('div', undefined, 'col-md-6 col-lg-4');
            const wrapper = node('div', undefined, 'form-check border rounded p-3 ps-5');
            const checkbox = node('input', undefined, 'form-check-input');
            checkbox.type = 'checkbox'; checkbox.name = 'SelectedItemIds'; checkbox.value = item.ItemId;
            checkbox.id = `item-${item.ItemId}`; checkbox.checked = selected.has(item.ItemId);
            checkbox.addEventListener('change', () => {
                checkbox.checked ? selected.add(item.ItemId) : selected.delete(item.ItemId);
                renderResults();
            });
            const label = node('label', item.ItemName, 'form-check-label'); label.htmlFor = checkbox.id;
            label.append(node('small', `${item.CategoryName} / ${item.Tests.length} test(s)`, 'd-block text-muted'));
            wrapper.append(checkbox, label); col.append(wrapper); itemArea.append(col);
        }
        renderResults();
    }
    categories.forEach(c => c.addEventListener('change', renderItems));
    document.getElementById('lab-entry-form').addEventListener('submit', () => { save.disabled = true; save.textContent = 'Saving...'; });
    renderItems();
})();
