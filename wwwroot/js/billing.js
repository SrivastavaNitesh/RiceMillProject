(() => {
    'use strict';
    const data = document.getElementById('billing-source');
    if (!data) return;
    const source = JSON.parse(data.textContent);
    const form = document.getElementById('billing-form');
    const weights = JSON.parse(document.getElementById('billing-weights').textContent);
    const n = value => Number.isFinite(Number(value)) ? Number(value) : 0;
    const round = (value, digits = 2) => Math.round((value + Number.EPSILON) * 10 ** digits) / 10 ** digits;
    const display = (value, digits = 2) => value.toLocaleString('en-IN', {minimumFractionDigits: digits, maximumFractionDigits: digits});
    const set = (id, value) => document.getElementById(id).textContent = display(value);
    function calculate() {
        let itemTotal = 0, deductions = 0, workerTotal = 0, assigned = 0;
        const rows = [...form.querySelectorAll('[data-item-id]')];
        rows.forEach(row => {
            const id = Number(row.dataset.itemId);
            const lots = source.Lots.filter(lot => lot.ItemId === id);
            const bags = lots.reduce((sum, lot) => sum + lot.BagCount, 0);
            const allocated = row.querySelector('.allocated-weight');
            const saved = weights.find(item => item.ItemId === id);
            const weight = n(saved.GrossKg);
            const marker = saved.IsEstimatedWeight ? ' *' : '';
            assigned += weight;
            allocated.textContent = display(weight, 3) + marker;
            const payable = Math.max(0, n(saved.PayableKg));
            const rate = n(row.querySelector('.item-rate').value);
            const basis = row.querySelector('.rate-basis').value;
            const qty = basis === 'Bag' ? bags : basis === 'Kg' ? payable : payable / 100;
            const amount = round(qty * rate);
            let deduction = 0;
            source.LabResults.filter(lab => lab.ItemId === id && lab.RuleId).forEach(lab => {
                if (!lab.ResultValue || !lab.ResultValue.trim()) return;
                const value = Number((lab.ResultValue || '').trim().replace(/%$/, ''));
                if (!Number.isFinite(value) || value < lab.Threshold) return;
                const d = lab.DeductionValue;
                deduction += round(({Percent: amount * d / 100, PerQuintal: payable * d / 100, PerKg: payable * d, PerBag: bags * d, Fixed: d})[lab.DeductionMode] || 0);
            });
            row.querySelector('.item-payable-kg').textContent = display(payable, 3) + marker;
            row.querySelector('.item-base').textContent = display(amount);
            row.querySelector('.item-deduction').textContent = display(deduction);
            row.querySelector('.item-amount').textContent = display(amount - deduction);
            itemTotal += amount; deductions += deduction;
        });
        workerTotal = round(n(document.getElementById('palledari-amount').value));
        const subtotal = round(itemTotal - deductions + workerTotal);
        const gst = round(subtotal * n(document.getElementById('gst-percent').value) / 100);
        set('total-items', itemTotal); set('total-deduction', deductions); set('total-workers', workerTotal);
        set('total-subtotal', subtotal); set('total-gst', gst); set('total-amount', round(subtotal + gst));
        const reconciliation = document.getElementById('weight-reconciliation');
        const matched = Math.abs(assigned - n(source.Header.NetWeight)) < .0005;
        reconciliation.textContent = 'आइटमों का वजन ' + display(assigned, 3) + ' kg / नेट ' + display(n(source.Header.NetWeight), 3) + ' kg : ' + (matched ? 'योग सही है' : 'सेव करने से पहले वजन का योग सही करें');
        reconciliation.className = 'small mt-3 ' + (matched ? 'text-success' : 'text-danger');
    }
    form.addEventListener('input', calculate);
    form.addEventListener('change', calculate);
    calculate();
})();
