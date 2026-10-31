(function (window, document, $) {
    "use strict";

    if (!$ || !$.fn || !$.fn.select2) return;

    const masterFieldPattern = /(meth|worker|labour|location|party|supplier|vehicle|driver|supervisor|gateman|gate.?man|office|company|subcompany|item|category|operator|employee|person)/i;
    const fixedFieldPattern = /(status|type|shift|mode|decision|result|active|completed|inwardoutward|movement)/i;

    function fieldKey(select) {
        return [select.id, select.name, select.getAttribute("asp-for"), select.className]
            .filter(Boolean)
            .join(" ");
    }

    function shouldEnhance(select) {
        if (!(select instanceof HTMLSelectElement) || select.disabled) return false;
        if (select.dataset.searchable === "false" || select.classList.contains("no-search-select")) return false;
        if (select.closest(".dataTables_length, .dataTables_filter")) return false;
        if (select.dataset.searchable === "true" || select.classList.contains("searchable-select")) return true;

        const key = fieldKey(select);
        if (fixedFieldPattern.test(key) && !masterFieldPattern.test(key)) return false;
        return masterFieldPattern.test(key) || select.options.length >= 8;
    }

    function placeholder(select) {
        const emptyOption = Array.from(select.options).find(option => option.value === "");
        const text = emptyOption && emptyOption.text.trim();
        return text && !/^--?$/.test(text) ? text.replace(/^--\s*|\s*--$/g, "") : "Search and select";
    }

    function enhance(select) {
        if (!shouldEnhance(select) || select.dataset.searchReady === "true") return;

        const $select = $(select);
        const $modal = $select.closest(".modal");
        const options = {
            width: "100%",
            minimumResultsForSearch: 0,
            placeholder: placeholder(select),
            allowClear: !select.required && !select.multiple,
            closeOnSelect: !select.multiple,
            language: {
                noResults: function () { return "No matching record found"; },
                searching: function () { return "Searching..."; },
                inputTooShort: function () { return "Type to search"; }
            }
        };

        if ($modal.length) options.dropdownParent = $modal;
        $select.select2(options);
        select.dataset.searchReady = "true";
    }

    function scan(root) {
        if (root instanceof HTMLSelectElement) enhance(root);
        root.querySelectorAll?.("select").forEach(enhance);
    }

    $(function () {
        scan(document);

        new MutationObserver(function (mutations) {
            mutations.forEach(function (mutation) {
                mutation.addedNodes.forEach(function (node) {
                    if (node instanceof Element) scan(node);
                });
            });
        }).observe(document.body, { childList: true, subtree: true });

        document.addEventListener("reset", function (event) {
            setTimeout(function () {
                $(event.target).find("select[data-search-ready='true']").trigger("change.select2");
            }, 0);
        });
    });
})(window, document, window.jQuery);
