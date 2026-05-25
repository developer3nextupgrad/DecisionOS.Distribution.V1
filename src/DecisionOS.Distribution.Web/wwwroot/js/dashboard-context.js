(function () {
    'use strict';

    const ALL_BUYERS = '';

    async function fetchJson(url) {
        const res = await fetch(url, { credentials: 'same-origin' });
        if (!res.ok) throw new Error('Request failed');
        return res.json();
    }

        function setOptions(select, items, placeholder, selectedValue, allowEmptyFirst) {
        select.innerHTML = '';
        if (allowEmptyFirst !== false) {
            const ph = document.createElement('option');
            ph.value = '';
            ph.textContent = placeholder;
            select.appendChild(ph);
        }

        for (const item of items) {
            const opt = document.createElement('option');
            if (typeof item === 'string') {
                opt.value = item;
                opt.textContent = item;
            } else {
                opt.value = item.customerId ?? item.value ?? '';
                opt.textContent = item.displayName ?? item.text ?? opt.value;
            }
            if (selectedValue && opt.value === selectedValue) opt.selected = true;
            select.appendChild(opt);
        }

        select.disabled = items.length === 0 && !selectedValue;
    }

    function initContextSelector(root) {
        const dist = root.querySelector('[data-select="distributor"]');
        const cust = root.querySelector('[data-select="customer"]');
        const week = root.querySelector('[data-select="week"]');
        if (!dist || !cust || !week) return;

        const initialCustomer = root.dataset.initialCustomer || '';
        const initialPeriod = root.dataset.initialPeriod || '';

        async function loadCustomers(clientId, keepCustomer) {
            if (!clientId) {
                setOptions(cust, [], '— Select distributor first —', '');
                setOptions(week, [], '— Select buyer first —', '');
                cust.disabled = true;
                week.disabled = true;
                return;
            }

            cust.disabled = false;
            try {
                const customers = await fetchJson(`/api/tenants/${encodeURIComponent(clientId)}/customers`);
                setOptions(cust, customers, 'All buyers (distributor total)', keepCustomer ?? '', true);
            } catch {
                setOptions(cust, [], 'No buyers in import data', '');
            }
            await loadWeeks(clientId, keepCustomer ?? cust.value, initialPeriod && keepCustomer === (root.dataset.initialCustomer || '') ? initialPeriod : '');
        }

        async function loadWeeks(clientId, customerId, keepPeriod) {
            if (!clientId) {
                setOptions(week, [], '— Select distributor first —', '');
                week.disabled = true;
                return;
            }

            week.disabled = false;
            const q = customerId ? `?customerId=${encodeURIComponent(customerId)}` : '';
            try {
                const weeks = await fetchJson(`/api/tenants/${encodeURIComponent(clientId)}/weeks${q}`);
                setOptions(week, weeks, '— Select week —', keepPeriod ?? '');
            } catch {
                setOptions(week, [], 'No weeks for this selection', '');
            }
        }

        dist.addEventListener('change', () => {
            root.dataset.initialCustomer = '';
            root.dataset.initialPeriod = '';
            loadCustomers(dist.value, ALL_BUYERS);
        });

        cust.addEventListener('change', () => {
            root.dataset.initialPeriod = '';
            loadWeeks(dist.value, cust.value, '');
        });

        week.addEventListener('change', () => {
            const go = root.closest('form')?.querySelector('[type="submit"]') ||
                root.querySelector('[data-action="go"]');
            if (!go && dist.value && week.value) {
                navigate();
            }
        });

        function navigate() {
            const params = new URLSearchParams();
            if (dist.value) params.set('clientId', dist.value);
            if (cust.value) params.set('customerId', cust.value);
            if (week.value) params.set('periodEnd', week.value);
            if (!dist.value || !week.value) return;
            window.location.href = `/Dashboard?${params.toString()}`;
        }

        const goBtn = root.querySelector('[data-action="go"]');
        if (goBtn) goBtn.addEventListener('click', navigate);

        const form = root.closest('form');
        if (form) {
            form.addEventListener('submit', (e) => {
                e.preventDefault();
                navigate();
            });
        }

        if (dist.value) {
            const hasServerCustomers = cust.options.length > 1;
            const hasServerWeeks = week.options.length > 1;
            if (!hasServerCustomers) {
                loadCustomers(dist.value, initialCustomer || ALL_BUYERS);
            } else if (!hasServerWeeks) {
                loadWeeks(dist.value, cust.value || ALL_BUYERS, initialPeriod || '');
            }
        }
    }

    document.querySelectorAll('[data-context-selector]').forEach(initContextSelector);
})();
