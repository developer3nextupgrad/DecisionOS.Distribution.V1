/* ============================================================
   DecisionOS Dashboard — site.js
   Vanilla ES6+ interactivity layer
   ============================================================ */

(function () {
  'use strict';

  /* ----- 1. Tenant / Week Selector Navigation ----- */

  function initSelectorNavigation() {
    const bar = document.querySelector('.selector-bar');
    if (!bar) return;

    const handleChange = () => {
      const clientSelect = bar.querySelector('[name="clientId"]');
      const periodSelect = bar.querySelector('[name="periodEnd"]');
      if (!clientSelect || !periodSelect) return;

      const clientId = clientSelect.value;
      const periodEnd = periodSelect.value;
      if (!clientId || !periodEnd) return;

      const params = new URLSearchParams({ clientId, periodEnd });
      window.location.href = `/dashboard?${params.toString()}`;
    };

    bar.addEventListener('change', (e) => {
      if (e.target.matches('select[name="clientId"], select[name="periodEnd"]')) {
        handleChange();
      }
    });

    const goBtn = bar.querySelector('[data-action="go"]');
    if (goBtn) {
      goBtn.addEventListener('click', handleChange);
    }
  }

  /* ----- 2. Auto-refresh "Last Updated" Timer ----- */

  function initLastUpdated() {
    const el = document.querySelector('[data-last-updated]');
    if (!el) return;

    const loadTime = Date.now();

    const format = (ms) => {
      const secs = Math.floor(ms / 1000);
      if (secs < 60) return 'just now';
      const mins = Math.floor(secs / 60);
      if (mins === 1) return '1 min ago';
      if (mins < 60) return `${mins} mins ago`;
      const hrs = Math.floor(mins / 60);
      return hrs === 1 ? '1 hr ago' : `${hrs} hrs ago`;
    };

    const tick = () => {
      el.textContent = format(Date.now() - loadTime);
    };

    tick();
    setInterval(tick, 30_000);
  }

  /* ----- 3. Mobile Navigation Toggle ----- */

  function initMobileNav() {
    const toggle = document.querySelector('.navbar-toggle');
    const nav = document.querySelector('.navbar-nav');
    if (!toggle || !nav) return;

    toggle.addEventListener('click', () => {
      const isOpen = nav.classList.toggle('open');
      toggle.setAttribute('aria-expanded', String(isOpen));
    });

    document.addEventListener('click', (e) => {
      if (!nav.contains(e.target) && !toggle.contains(e.target)) {
        nav.classList.remove('open');
        toggle.setAttribute('aria-expanded', 'false');
      }
    });
  }

  /* ----- 4. Smooth Scroll to Anchors ----- */

  function initSmoothScroll() {
    document.addEventListener('click', (e) => {
      const link = e.target.closest('a[href^="#"]');
      if (!link) return;

      const id = link.getAttribute('href').slice(1);
      const target = document.getElementById(id);
      if (!target) return;

      e.preventDefault();
      target.scrollIntoView({ behavior: 'smooth', block: 'start' });

      if (history.replaceState) {
        history.replaceState(null, '', `#${id}`);
      }
    });
  }

  /* ----- 5. Animate Numbers on Load ----- */

  function animateValue(el, start, end, duration) {
    const startTime = performance.now();
    const isInt = Number.isInteger(end);

    const step = (now) => {
      const progress = Math.min((now - startTime) / duration, 1);
      const eased = 1 - Math.pow(1 - progress, 3);
      const current = start + (end - start) * eased;
      el.textContent = isInt ? Math.round(current).toLocaleString() : current.toFixed(1);

      if (progress < 1) {
        requestAnimationFrame(step);
      }
    };

    requestAnimationFrame(step);
  }

  function initNumberAnimation() {
    const elements = document.querySelectorAll('[data-animate-number]');
    if (!elements.length) return;

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (!entry.isIntersecting) return;

          const el = entry.target;
          if (el.dataset.animated) return;
          el.dataset.animated = 'true';

          const raw = el.textContent.replace(/,/g, '').trim();
          const end = parseFloat(raw);
          if (isNaN(end)) return;

          animateValue(el, 0, end, 800);
          observer.unobserve(el);
        });
      },
      { threshold: 0.3 }
    );

    elements.forEach((el) => observer.observe(el));
  }

  /* ----- 6. Staggered Card Entrance Animation ----- */

  function initCardStagger() {
    const cards = document.querySelectorAll('.card, .kpi-tile, .focus-card, .alert-banner');
    cards.forEach((card, i) => {
      card.style.animationDelay = `${i * 60}ms`;
    });
  }

  /* ----- Boot ----- */

  function init() {
    initSelectorNavigation();
    initLastUpdated();
    initMobileNav();
    initSmoothScroll();
    initNumberAnimation();
    initCardStagger();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
