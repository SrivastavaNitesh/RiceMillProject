(() => {
    'use strict';
    const rows = JSON.parse(document.getElementById('pending-inward-data').textContent);
    const inward = document.getElementById('ddlInwardNo');
    const vehicle = document.getElementById('ddlVehicle');
    const driver = document.getElementById('ddlDriver');
    const party = document.getElementById('inwardParty');
    const message = document.getElementById('inward-link-message');
    const save = document.getElementById('generate-rst');
    const vehicleKey = r => r.VehicleNumber.trim().toUpperCase();
    const driverKey = r => JSON.stringify([r.DriverName.trim().toLowerCase(), r.DriverMobile.trim()]);
    function options(select, list, key, label, prompt, selected = '') {
        select.replaceChildren(new Option(prompt, ''));
        const used = new Set();
        list.forEach(r => {
            const value = key(r);
            if (!used.has(value)) { select.add(new Option(label(r), value)); used.add(value); }
        });
        select.value = selected;
    }
    function inwardOptions(list, selected = '') {
        options(inward, list, r => r.InwardNo, r => `${r.InwardNo} (${r.VehicleNumber})`, '-- Select Pending Inward --', selected);
    }
    function apply(row) {
        party.value = row ? row.PartyName : '';
        save.disabled = !row;
        if (row) {
            vehicle.value = vehicleKey(row);
            driver.value = driverKey(row);
            message.textContent = `Linked inward: ${row.InwardNo}`;
        }
    }
    function chooseRelated(source) {
        const isVehicle = source === vehicle;
        const key = isVehicle ? vehicleKey : driverKey;
        const matches = source.value ? rows.filter(r => key(r) === source.value) : rows;
        inwardOptions(matches);
        apply(null);
        (isVehicle ? driver : vehicle).value = '';
        if (source.value && matches.length === 1) { inward.value = matches[0].InwardNo; apply(matches[0]); }
        else message.textContent = source.value ? 'Multiple pending inwards match. Select the exact inward number.' : 'Select a pending inward, vehicle or driver.';
    }
    const initial = inward.value;
    options(vehicle, rows, vehicleKey, r => r.VehicleNumber, '-- Select Vehicle --');
    options(driver, rows, driverKey, r => r.DriverName + (r.DriverMobile ? ' / ' + r.DriverMobile : ''), '-- Select Driver --');
    inwardOptions(rows, initial);
    apply(rows.find(r => r.InwardNo === initial));
    inward.addEventListener('change', () => {
        const row = rows.find(r => r.InwardNo === inward.value);
        if (!row) { vehicle.value = ''; driver.value = ''; inwardOptions(rows); message.textContent = ''; }
        apply(row);
    });
    vehicle.addEventListener('change', () => chooseRelated(vehicle));
    driver.addEventListener('change', () => chooseRelated(driver));
})();