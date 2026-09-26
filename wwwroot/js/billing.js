(() => {
    'use strict';
    const data = document.getElementById('billing-source');
    if (!data) return;
    const source = JSON.parse(data.textContent);
    const form = document.getElementById('billing-form');
    const mode = document.getElementById('weight-mode');
    const n = value => Number.isFinite(Number(value)) ? Number(value) : 0;
    const round = (value, digits = 2) => Math.round((value + Number.EPSILON) * 10 ** digits) / 10 ** digits;
    const display = (value, digits = 2) => value.toLocaleString('en-IN', {minimumFractionDigits: digits, maximumFractionDigits: digits});
    const set = (id, value) => document.getElementById(id).textContent = display(value);
    function calculate() {
        let itemTotal = 0, deductions = 0, workerTotal = 0, assigned = 0;
        const rows = [...form.querySelectorAll('[data-item-id]')];
        const bagsTotal = source.Lots.reduce((sum, lot) => sum + lot.BagCount, 0);
        rows.forEach((row, index) => {
            const id = Number(row.dataset.itemId);
            const lots = source.Lots.filter(lot => lot.ItemId === id);
            const bags = lots.reduce((sum, lot) => sum + lot.BagCount, 0);
            const measured = row.querySelector('.measured-weight');
            const allocated = row.querySelector('.allocated-weight');
            const measuredMode = mode.value === 'Measured';
            measured.hidden = !measuredMode;
            measured.required = measuredMode;
            measured.disabled = !measuredMode;
            allocated.hidden = measuredMode;
            const weight = measuredMode ? n(measured.value) : bagsTotal ? index === rows.length - 1 ? round(n(source.Header.NetWeight) - assigned, 3) : round(n(source.Header.NetWeight) * bags / bagsTotal, 3) : 0;
            assigned += weight;
            allocated.textContent = display(weight, 3);
            const bagTare = round(lots.reduce((sum, lot) => sum + lot.BagCount * lot.DeductionWeightGrams, 0) / 1000, 3);
            const payable = Math.max(0, round(weight - bagTare, 3));
            const rate = n(row.querySelector('.item-rate').value);
            const basis = row.querySelector('.rate-basis').value;
            const qty = basis === 'Bag' ? bags : basis === 'Kg' ? payable : payable / 100;
            const amount = round(qty * rate);
            let deduction = 0;
            source.LabResults.filter(lab => lab.ItemId === id && lab.RuleId).forEach(lab => {
                const value = Number((lab.ResultValue || '').trim().replace(/%$/, ''));
                if (!Number.isFinite(value) || value < lab.Threshold) return;
                const d = lab.DeductionValue;
                deduction += round(({Percent: amount * d / 100, PerQuintal: payable * d / 100, PerKg: payable * d, PerBag: bags * d, Fixed: d})[lab.DeductionMode] || 0);
            });
            row.querySelector('.item-payable-kg').textContent = display(payable, 3);
            row.querySelector('.item-base').textContent = display(amount);
            row.querySelector('.item-deduction').textContent = display(deduction);
            row.querySelector('.item-amount').textContent = display(amount - deduction);
            itemTotal += amount; deductions += deduction;
        });
        form.querySelectorAll('.worker-row').forEach(row => {
            const amount = round(n(row.dataset.bags) * n(row.querySelector('.worker-rate').value));
            row.querySelector('.worker-amount').textContent = display(amount);
            workerTotal += amount;
        });
        const subtotal = round(itemTotal - deductions + workerTotal);
        const gst = round(subtotal * n(document.getElementById('gst-percent').value) / 100);
        set('total-items', itemTotal); set('total-deduction', deductions); set('total-workers', workerTotal);
        set('total-subtotal', subtotal); set('total-gst', gst); set('total-amount', round(subtotal + gst));
        const reconciliation = document.getElementById('weight-reconciliation');
        const matched = Math.abs(assigned - n(source.Header.NetWeight)) < .0005;
        reconciliation.textContent = 'Allocated ' + display(assigned, 3) + ' kg / net ' + display(n(source.Header.NetWeight), 3) + ' kg — ' + (matched ? 'matched' : 'weights must match before saving');
        reconciliation.className = 'small mt-3 ' + (matched ? 'text-success' : 'text-danger');
    }
    form.addEventListener('input', calculate);
    form.addEventListener('change', calculate);
    calculate();
})();
