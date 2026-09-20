// Enterprise ERP Global JavaScript - Active Link & DataTables Auto-Initializer
document.addEventListener("DOMContentLoaded", function () {
    // 1. ACTIVE LINK AUTO-MATCHER
    const normalizePath = path => path.toLowerCase().replace(/\/+$/, "").replace(/\/index$/, "");
    const currentPath = normalizePath(window.location.pathname);
    const links = document.querySelectorAll(".sidebar a, .navbar-nav a");

    let matchedLink = null;

    // First pass: exact URL match
    links.forEach(function (link) {
        link.classList.remove("active");
        const href = link.getAttribute("href");
        if (!href || href.startsWith("#") || href.startsWith("javascript:")) return;

        const linkPath = normalizePath(new URL(href, window.location.origin).pathname);
        if (currentPath === linkPath) {
            matchedLink = link;
        }
    });

    // Second pass: longest prefix match if exact match not found
    if (!matchedLink) {
        let longestLen = 0;
        links.forEach(function (link) {
            const href = link.getAttribute("href");
            if (!href || href.startsWith("#") || href.startsWith("javascript:")) return;

            const linkPath = normalizePath(new URL(href, window.location.origin).pathname);
            if (linkPath.split("/").filter(Boolean).length >= 2 && currentPath.startsWith(linkPath + "/")) {
                if (linkPath.length > longestLen) {
                    longestLen = linkPath.length;
                    matchedLink = link;
                }
            }
        });
    }

    if (matchedLink) {
        matchedLink.classList.add("active");

        // Auto-expand collapse parent dropdown (e.g. Master Registers)
        const collapseParent = matchedLink.closest(".collapse");
        if (collapseParent) {
            collapseParent.classList.add("show");
            const toggleBtn = document.querySelector('[href="#' + collapseParent.id + '"], [data-bs-target="#' + collapseParent.id + '"]');
            if (toggleBtn) {
                toggleBtn.setAttribute("aria-expanded", "true");
                toggleBtn.classList.add("active");
            }
        }
    } else if (currentPath === "" || currentPath === "/") {
        const dashLink = document.querySelector('.sidebar a[href*="Dashboard"], .navbar-nav a[href*="Dashboard"]');
        if (dashLink) dashLink.classList.add("active");
    }

    // 1b. SIDEBAR SCROLL PERSISTENCE & AUTO-SCROLL LOGIC
    const sidebar = document.querySelector(".sidebar");
    if (sidebar) {
        // Restore saved scroll position
        const savedScroll = sessionStorage.getItem("sidebarScrollTop");
        if (savedScroll !== null) {
            sidebar.scrollTop = parseInt(savedScroll, 10);
        }

        // Save scroll position when user clicks any link
        sidebar.querySelectorAll("a").forEach(function (link) {
            link.addEventListener("click", function () {
                // If clicking top-level links (not inside Master Data collapse), reset scroll to top
                if (!link.closest(".collapse")) {
                    sessionStorage.setItem("sidebarScrollTop", "0");
                } else {
                    sessionStorage.setItem("sidebarScrollTop", sidebar.scrollTop);
                }
            });
        });

        // Save scroll position on manual scroll
        let scrollTimeout;
        sidebar.addEventListener("scroll", function () {
            clearTimeout(scrollTimeout);
            scrollTimeout = setTimeout(function () {
                sessionStorage.setItem("sidebarScrollTop", sidebar.scrollTop);
            }, 100);
        });

        // Scroll to Top Menu button click
        const scrollTopBtn = document.getElementById("sidebarScrollTopBtn");
        if (scrollTopBtn) {
            scrollTopBtn.addEventListener("click", function (e) {
                e.preventDefault();
                sidebar.scrollTo({ top: 0, behavior: "smooth" });
                sessionStorage.setItem("sidebarScrollTop", "0");
            });
        }
    }

    // 2. ENTERPRISE AUTOMATIC DATATABLES INITIALIZER
    if (typeof $ !== 'undefined' && $.fn.DataTable) {
        // Suppress DataTables alert popups globally
        $.fn.dataTable.ext.errMode = 'none';

        const tables = $('table.table, table.erp-table, table.datatable').not('.no-datatable, .details-table');
        
        tables.each(function () {
            const $tbl = $(this);
            const $thead = $tbl.find('thead');
            const $rows = $tbl.find('tbody tr');

            // Skip if already initialized, no thead, or no rows
            if (!$.fn.DataTable.isDataTable(this) && $thead.length > 0 && $rows.length > 0) {
                // Check if table contains only a single empty 'colspan' row
                const isSingleColspanRow = $rows.length === 1 && $rows.find('td[colspan]').length > 0;
                if (isSingleColspanRow) {
                    return; // Do not initialize DataTables on empty-state single colspan rows
                }

                // Check column count match
                const thCount = $thead.find('th').length;
                const tdCount = $rows.first().find('td').length;

                if (thCount > 0 && thCount === tdCount) {
                    $tbl.DataTable({
                        pageLength: 10,
                        lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
                        language: {
                            search: "_INPUT_",
                            searchPlaceholder: "🔍 Search records...",
                            lengthMenu: "Show _MENU_ entries",
                            info: "Showing _START_ to _END_ of _TOTAL_ entries",
                            paginate: {
                                first: "«",
                                previous: "‹",
                                next: "›",
                                last: "»"
                            }
                        },
                        responsive: true,
                        ordering: true,
                        autoWidth: false
                    });
                }
            }
        });
    }
});
