document.querySelectorAll('[data-purchase-report-picker]').forEach(function (picker) {
    picker.addEventListener('change', function () {
        if (picker.value) window.location.assign(picker.value);
    });
});
