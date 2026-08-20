window.WikiScrapper = (function () {
    const statusUrl = "/api/sync/status";
    const startUrl = "/api/sync";
    const ui = window.WikiScrapperUi || {};

    function t(key, fallback) {
        return ui[key] || fallback;
    }

    function format(template) {
        var args = Array.prototype.slice.call(arguments, 1);
        return String(template || "").replace(/\{(\d+)\}/g, function (_, index) {
            return args[Number(index)] ?? "";
        });
    }

    async function fetchStatus() {
        const response = await fetch(statusUrl, { headers: { Accept: "application/json" } });
        if (!response.ok) {
            throw new Error(t("unableReadStatus", "Unable to read sync status."));
        }
        return response.json();
    }

    function setBadge(isRunning) {
        const badge = document.getElementById("sync-badge");
        if (badge) {
            badge.classList.toggle("d-none", !isRunning);
        }
    }

    function startBadgePolling() {
        if (document.getElementById("sync-form")) {
            // Dashboard already polls more frequently while syncing.
            return;
        }
        const tick = async () => {
            try {
                const status = await fetchStatus();
                setBadge(status.isRunning);
            } catch {
                // Badge is decorative; ignore transient errors.
            }
        };
        tick();
        setInterval(tick, 2000);
    }

    function applyDashboard(status) {
        const button = document.getElementById("sync-button");
        const spinner = document.getElementById("sync-spinner");
        const label = document.getElementById("sync-button-label");
        const progress = document.getElementById("sync-progress");
        const bar = document.getElementById("sync-bar");
        const progressLabel = document.getElementById("sync-progress-label");
        const percentLabel = document.getElementById("sync-progress-percent");
        const result = document.getElementById("sync-result");
        const resultSummary = document.getElementById("sync-result-summary");
        const resultErrors = document.getElementById("sync-result-errors");

        const percent = status.percent ?? 0;
        setBadge(status.isRunning);

        if (button) {
            button.disabled = status.isRunning;
        }
        if (spinner) {
            spinner.classList.toggle("d-none", !status.isRunning);
        }
        if (label) {
            label.textContent = status.isRunning
                ? t("syncRunning", "Synchronizing…")
                : t("syncButton", "Synchronize with Wikipedia");
        }
        if (progress) {
            progress.classList.toggle("d-none", !status.isRunning);
        }
        if (bar) {
            bar.style.width = percent + "%";
        }
        if (progressLabel) {
            const current = status.currentItem ? " — " + status.currentItem : "";
            progressLabel.textContent = status.isRunning
                ? (status.processed + " / " + status.total + current)
                : "";
        }
        if (percentLabel) {
            percentLabel.textContent = status.isRunning ? Math.round(percent) + "%" : "";
        }

        if (result && resultSummary && resultErrors) {
            const showErrors = !status.isRunning && status.completedAtUtc && status.failed > 0;
            result.classList.toggle("d-none", !showErrors);
            if (showErrors) {
                resultSummary.textContent = format(
                    t("lastSync", "Last sync: {0} succeeded, {1} failed, {2} skipped."),
                    status.succeeded,
                    status.failed,
                    status.skipped);
                const shown = (status.errors || []).slice(0, 15);
                resultErrors.innerHTML = "";
                shown.forEach(function (error) {
                    const li = document.createElement("li");
                    li.textContent = error;
                    resultErrors.appendChild(li);
                });
                if ((status.errors || []).length > 15) {
                    const li = document.createElement("li");
                    li.innerHTML = format(
                        t("moreErrors", "…and {0} more. See <a href=\"/Logs?level=Error\">Logs</a>."),
                        (status.errors || []).length - 15);
                    resultErrors.appendChild(li);
                }
            }
        }
    }

    function dashboardSync(options) {
        const form = document.getElementById("sync-form");
        if (!form) {
            return;
        }

        let pollTimer = null;
        let wasRunning = options.initiallyRunning === true;

        const poll = async () => {
            try {
                const status = await fetchStatus();
                applyDashboard(status);
                if (status.isRunning) {
                    wasRunning = true;
                    return;
                }
                if (pollTimer) {
                    clearInterval(pollTimer);
                    pollTimer = null;
                }
                if (wasRunning) {
                    window.location.reload();
                }
            } catch {
                // Keep polling; a single failed read should not stop the UI.
            }
        };

        if (wasRunning) {
            pollTimer = setInterval(poll, 1000);
            poll();
        }

        form.addEventListener("submit", async function (event) {
            event.preventDefault();
            applyDashboard({ isRunning: true, processed: 0, total: 0, percent: 0, currentItem: t("starting", "Starting…") });
            try {
                const response = await fetch(startUrl, {
                    method: "POST",
                    headers: { Accept: "application/json" }
                });
                if (response.status !== 202 && response.status !== 409) {
                    throw new Error(t("unableStartSync", "Unable to start synchronization."));
                }
                wasRunning = true;
                if (!pollTimer) {
                    pollTimer = setInterval(poll, 1000);
                }
                await poll();
            } catch (error) {
                applyDashboard({ isRunning: false, processed: 0, total: 0 });
                alert(error.message || t("unableStartSync", "Unable to start synchronization."));
            }
        });
    }

    function countriesPagePath() {
        return window.location.pathname.toLowerCase().indexOf("/countries") >= 0
            ? window.location.pathname.split("?")[0]
            : "/Countries";
    }

    function truncate(text, max) {
        if (!text) {
            return "";
        }
        return text.length > max ? text.slice(0, max) + "…" : text;
    }

    function formatFetchedAt(iso) {
        if (!iso) {
            return "—";
        }
        var date = new Date(iso);
        if (Number.isNaN(date.getTime())) {
            return "—";
        }
        var pad = function (n) { return String(n).padStart(2, "0"); };
        return date.getFullYear() + "-" + pad(date.getMonth() + 1) + "-" + pad(date.getDate()) +
            " " + pad(date.getHours()) + ":" + pad(date.getMinutes());
    }

    function countriesVirtualList() {
        var root = document.getElementById("countries-virtual");
        if (!root) {
            return null;
        }

        var viewport = document.getElementById("countries-virtual-viewport");
        var spacer = document.getElementById("countries-virtual-spacer");
        var windowTable = document.getElementById("countries-virtual-window-table");
        var windowBody = document.getElementById("countries-virtual-window");
        var statusEl = document.getElementById("countries-virtual-status");
        var loadingEl = document.getElementById("countries-virtual-loading");
        if (!viewport || !spacer || !windowBody || !windowTable) {
            return null;
        }

        var ROW_HEIGHT = 52;
        var OVERSCAN = 10;
        var chunkSize = Number(root.dataset.chunkSize || 50);
        var totalCount = Number(root.dataset.totalCount || 0);
        var cache = new Map();
        var inflight = new Map();
        var itemsByIndex = [];
        var destroyed = false;

        function setLoading(isLoading) {
            loadingEl?.classList.toggle("d-none", !isLoading);
        }

        function updateStatus() {
            if (!statusEl) {
                return;
            }
            statusEl.textContent = format(
                root.dataset.labelStatus || "Showing all {0} countries — rows load as you scroll",
                totalCount);
        }

        function buildApiUrl(page) {
            var params = new URLSearchParams();
            params.set("page", String(page));
            params.set("pageSize", String(chunkSize));
            if (root.dataset.search) {
                params.set("search", root.dataset.search);
            }
            if (root.dataset.fetched === "yes") {
                params.set("fetched", "true");
            } else if (root.dataset.fetched === "no") {
                params.set("fetched", "false");
            }
            if (root.dataset.sort) {
                params.set("sort", root.dataset.sort);
            }
            if (root.dataset.dir) {
                params.set("dir", root.dataset.dir);
            }
            if (root.dataset.lang) {
                params.set("lang", root.dataset.lang);
            }
            return "/api/countries?" + params.toString();
        }

        async function ensurePage(page) {
            if (page < 1 || cache.has(page) || inflight.has(page)) {
                return inflight.get(page) || Promise.resolve();
            }
            var request = fetch(buildApiUrl(page), { headers: { Accept: "application/json" } })
                .then(function (response) {
                    if (!response.ok) {
                        throw new Error("Failed to load countries chunk");
                    }
                    return response.json();
                })
                .then(function (data) {
                    cache.set(page, data.items || []);
                    if (typeof data.totalCount === "number") {
                        totalCount = data.totalCount;
                        root.dataset.totalCount = String(totalCount);
                        spacer.style.height = (totalCount * ROW_HEIGHT) + "px";
                        updateStatus();
                    }
                    var offset = (page - 1) * chunkSize;
                    (data.items || []).forEach(function (item, i) {
                        itemsByIndex[offset + i] = item;
                    });
                })
                .finally(function () {
                    inflight.delete(page);
                    setLoading(inflight.size > 0);
                });
            inflight.set(page, request);
            setLoading(true);
            return request;
        }

        function openModal(item) {
            var modalEl = document.getElementById("country-virtual-modal");
            if (!modalEl || !item || !item.description) {
                return;
            }
            document.getElementById("country-virtual-modal-title").innerHTML =
                escapeHtml(item.name) + ' <code class="ms-2">' + escapeHtml(item.code) + "</code>";
            document.getElementById("country-virtual-modal-body").textContent = item.description;
            document.getElementById("country-virtual-modal-fetched").textContent = format(
                root.dataset.labelFetchedAt || "Fetched {0}",
                formatFetchedAt(item.fetchedAt));
            var wiki = document.getElementById("country-virtual-modal-wiki");
            if (item.wikiUrl) {
                wiki.href = item.wikiUrl;
                wiki.classList.remove("d-none");
            } else {
                wiki.classList.add("d-none");
            }
            bootstrap.Modal.getOrCreateInstance(modalEl).show();
        }

        function escapeHtml(value) {
            return String(value ?? "")
                .replace(/&/g, "&amp;")
                .replace(/</g, "&lt;")
                .replace(/>/g, "&gt;")
                .replace(/"/g, "&quot;");
        }

        function renderRow(item, index) {
            var tr = document.createElement("tr");
            if (!item) {
                tr.className = "is-placeholder";
                tr.innerHTML =
                    "<td colspan=\"4\">" + escapeHtml(root.dataset.labelLoading || "Loading…") + "</td>";
                return tr;
            }
            var fetched = !!item.description;
            if (fetched) {
                tr.className = "row-clickable";
                tr.tabIndex = 0;
                tr.setAttribute("role", "button");
                tr.innerHTML =
                    "<td><code>" + escapeHtml(item.code) + "</code></td>" +
                    "<td class=\"cell-ellipsis fw-semibold\" title=\"" + escapeHtml(item.name) + "\">" +
                    escapeHtml(item.name) + "</td>" +
                    "<td>" + escapeHtml(truncate(item.description, 120)) + "</td>" +
                    "<td class=\"text-nowrap\">" + escapeHtml(formatFetchedAt(item.fetchedAt)) + "</td>";
                tr.addEventListener("click", function () { openModal(item); });
                tr.addEventListener("keydown", function (event) {
                    if (event.key === "Enter" || event.key === " ") {
                        event.preventDefault();
                        openModal(item);
                    }
                });
            } else {
                tr.innerHTML =
                    "<td><code>" + escapeHtml(item.code) + "</code></td>" +
                    "<td class=\"cell-ellipsis fw-semibold\" title=\"" + escapeHtml(item.name) + "\">" +
                    escapeHtml(item.name) + "</td>" +
                    "<td><span class=\"text-muted\">" + escapeHtml(root.dataset.labelNotFetched || "Not fetched yet —") +
                    ' <a href="' + escapeHtml(root.dataset.dashboardUrl || "/") + '">' +
                    escapeHtml(root.dataset.labelDashboard || "Dashboard") + "</a></span></td>" +
                    "<td class=\"text-nowrap\">—</td>";
            }
            tr.dataset.index = String(index);
            return tr;
        }

        function visibleRange() {
            var start = Math.max(0, Math.floor(viewport.scrollTop / ROW_HEIGHT) - OVERSCAN);
            var visible = Math.ceil(viewport.clientHeight / ROW_HEIGHT) + OVERSCAN * 2;
            var end = Math.min(totalCount, start + visible);
            return { start: start, end: end };
        }

        function pagesForRange(start, end) {
            var first = Math.floor(start / chunkSize) + 1;
            var last = Math.floor(Math.max(start, end - 1) / chunkSize) + 1;
            var pages = [];
            for (var p = first; p <= last; p++) {
                pages.push(p);
            }
            return pages;
        }

        function render() {
            if (destroyed) {
                return;
            }
            if (totalCount === 0) {
                windowBody.innerHTML = "";
                var empty = document.createElement("tr");
                empty.innerHTML = "<td colspan=\"4\" class=\"text-muted\">" +
                    escapeHtml(root.dataset.labelNoMatch || "No countries match the current filters.") + "</td>";
                windowBody.appendChild(empty);
                spacer.style.height = ROW_HEIGHT + "px";
                updateStatus();
                return;
            }

            var range = visibleRange();
            pagesForRange(range.start, range.end).forEach(function (page) {
                ensurePage(page).then(function () {
                    if (!destroyed) {
                        paint(range.start, range.end);
                    }
                });
            });
            paint(range.start, range.end);
        }

        function paint(start, end) {
            windowTable.style.transform = "translateY(" + (start * ROW_HEIGHT) + "px)";
            windowBody.innerHTML = "";
            for (var i = start; i < end; i++) {
                windowBody.appendChild(renderRow(itemsByIndex[i], i));
            }
        }

        spacer.style.height = Math.max(totalCount, 1) * ROW_HEIGHT + "px";
        updateStatus();

        var onScroll = function () { render(); };
        viewport.addEventListener("scroll", onScroll, { passive: true });
        window.addEventListener("resize", onScroll);

        ensurePage(1).then(function () { render(); });
        render();

        return function dispose() {
            destroyed = true;
            viewport.removeEventListener("scroll", onScroll);
            window.removeEventListener("resize", onScroll);
        };
    }

    function countriesLiveSearch() {
        const input = document.getElementById("search");
        const form = input && input.closest("form");
        if (!input || !form || !document.getElementById("countries-results")) {
            return;
        }

        const debounceMs = 800;
        let timer = null;
        let lastRequested = input.value;
        let abort = null;
        let requestId = 0;
        let disposeVirtual = countriesVirtualList();

        function setBusy(isBusy) {
            const spinner = document.getElementById("search-spinner");
            const results = document.getElementById("countries-results");
            spinner?.classList.toggle("d-none", !isBusy);
            results?.setAttribute("aria-busy", isBusy ? "true" : "false");
        }

        function buildUrl(searchValue) {
            const params = new URLSearchParams();
            const term = searchValue.trim();
            if (term) {
                params.set("search", term);
            }
            const fetched = form.elements.fetched && form.elements.fetched.value;
            if (fetched) {
                params.set("fetched", fetched);
            }
            const pageSize = form.elements.pageSize && form.elements.pageSize.value;
            if (pageSize) {
                params.set("pageSize", pageSize);
            }
            var current = new URLSearchParams(window.location.search);
            if (current.get("sort")) {
                params.set("sort", current.get("sort"));
            }
            if (current.get("dir")) {
                params.set("dir", current.get("dir"));
            }
            params.set("page", "1");
            const query = params.toString();
            return countriesPagePath() + (query ? "?" + query : "");
        }

        function disposeOrphanedModals() {
            document.querySelectorAll(".modal-backdrop").forEach(function (el) { el.remove(); });
            document.body.classList.remove("modal-open");
            document.body.style.removeProperty("overflow");
            document.body.style.removeProperty("padding-right");
        }

        async function run(searchValue) {
            if (searchValue === lastRequested) {
                return;
            }
            lastRequested = searchValue;
            abort?.abort();
            abort = new AbortController();
            const currentRequest = ++requestId;
            setBusy(true);
            try {
                const url = buildUrl(searchValue);
                const response = await fetch(url, {
                    headers: { "X-Requested-With": "XMLHttpRequest" },
                    signal: abort.signal
                });
                if (!response.ok || currentRequest !== requestId) {
                    return;
                }
                const html = await response.text();
                const next = new DOMParser().parseFromString(html, "text/html")
                    .getElementById("countries-results");
                const current = document.getElementById("countries-results");
                if (!next || !current || currentRequest !== requestId) {
                    return;
                }
                disposeOrphanedModals();
                if (typeof disposeVirtual === "function") {
                    disposeVirtual();
                    disposeVirtual = null;
                }
                current.replaceWith(next);
                history.replaceState(null, "", url);
                disposeVirtual = countriesVirtualList();
            } catch (error) {
                if (error.name !== "AbortError" && currentRequest === requestId) {
                    lastRequested = null;
                }
            } finally {
                if (currentRequest === requestId) {
                    setBusy(false);
                }
            }
        }

        input.addEventListener("input", function () {
            clearTimeout(timer);
            timer = setTimeout(function () { run(input.value); }, debounceMs);
        });
    }

    startBadgePolling();
    countriesLiveSearch();

    const langSelect = document.getElementById("wiki-lang");
    if (langSelect) {
        langSelect.addEventListener("change", function () {
            const returnUrl = window.location.pathname + window.location.search;
            window.location.href = "/language?lang=" + encodeURIComponent(langSelect.value) +
                "&returnUrl=" + encodeURIComponent(returnUrl);
        });
    }

    return { dashboardSync: dashboardSync };
})();
