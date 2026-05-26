// ── Stat card number counter
function animateCount(el) {
  const target = parseInt(el.dataset.target, 10);
  if (isNaN(target)) return;
  const duration = 900;
  const start = performance.now();
  requestAnimationFrame(function step(now) {
    const p = Math.min((now - start) / duration, 1);
    const ease = 1 - Math.pow(1 - p, 3);
    el.textContent = Math.round(ease * target).toLocaleString();
    if (p < 1) requestAnimationFrame(step);
  });
}
document.querySelectorAll('[data-target]').forEach(animateCount);

// ── Toast notification system
const toastIcons = {
  success: 'ti-circle-check',
  warning: 'ti-alert-triangle',
  danger:  'ti-circle-x',
  info:    'ti-info-circle',
};

function showToast(title, message = '', type = 'info', duration = 4500) {
  const container = document.getElementById('toastContainer');
  if (!container) return;
  const toast = document.createElement('div');
  toast.className = `cs-toast ${type}`;
  toast.innerHTML = `
    <i class="ti ${toastIcons[type] || 'ti-info-circle'}" aria-hidden="true"></i>
    <div class="cs-toast-body">
      <div class="cs-toast-title">${title}</div>
      ${message ? `<div class="cs-toast-msg">${message}</div>` : ''}
    </div>
    <button class="cs-toast-close" aria-label="Dismiss">
      <i class="ti ti-x"></i>
    </button>`;
  toast.querySelector('.cs-toast-close').addEventListener('click', () => {
    toast.style.animation = 'cs-slide-out 0.25s ease forwards';
    setTimeout(() => toast.remove(), 260);
  });
  container.appendChild(toast);
  setTimeout(() => {
    if (toast.isConnected) {
      toast.style.animation = 'cs-slide-out 0.25s ease forwards';
      setTimeout(() => toast.remove(), 260);
    }
  }, duration);
}

// ── Nav ripple effect
document.querySelectorAll('.cs-nav-item').forEach(item => {
  item.addEventListener('click', function(e) {
    const ripple = document.createElement('span');
    const rect = this.getBoundingClientRect();
    const size = Math.max(rect.width, rect.height);
    Object.assign(ripple.style, {
      position: 'absolute',
      width: `${size}px`, height: `${size}px`,
      top: `${e.clientY - rect.top - size / 2}px`,
      left: `${e.clientX - rect.left - size / 2}px`,
      background: 'var(--cs-accent)',
      borderRadius: '50%',
      pointerEvents: 'none',
      animation: 'cs-ripple 0.5s ease-out forwards',
    });
    this.appendChild(ripple);
    setTimeout(() => ripple.remove(), 500);
  });
});

// ── Sidebar toggle (collapse/expand)
const sidebar  = document.getElementById('csSidebar');
const mainArea = document.getElementById('csMain');
const toggleBtn = document.getElementById('sidebarToggle');

if (toggleBtn && sidebar && mainArea) {
  toggleBtn.addEventListener('click', () => {
    const collapsed = sidebar.classList.toggle('cs-sidebar-collapsed');
    mainArea.classList.toggle('cs-main-expanded', collapsed);
    localStorage.setItem('sidebarCollapsed', collapsed);
  });

  // Restore state
  if (localStorage.getItem('sidebarCollapsed') === 'true') {
    sidebar.classList.add('cs-sidebar-collapsed');
    mainArea.classList.add('cs-main-expanded');
  }
}

// ── Scanner flash helper
function flashScanResult(type) {
  const overlay = document.querySelector('.cs-scanner-overlay');
  if (!overlay) return;
  overlay.className = 'cs-scanner-overlay';
  void overlay.offsetWidth;
  overlay.classList.add(type === 'success' ? 'flash-success' : 'flash-danger');
  showToast(
    type === 'success' ? 'Access granted' : 'Access denied',
    type === 'success' ? 'Entry logged successfully.' : 'Blacklisted or invalid credential.',
    type === 'success' ? 'success' : 'danger'
  );
}

// ── Live feed prepend
function prependFeedItem(html) {
  const feed = document.getElementById('scanFeed');
  if (!feed) return;
  const div = document.createElement('div');
  div.className = 'cs-feed-item cs-feed-item-new';
  div.innerHTML = html;
  feed.prepend(div);
  const items = feed.querySelectorAll('.cs-feed-item');
  if (items.length > 8) items[items.length - 1].remove();
}

// ── ⌘K / Ctrl+K search focus shortcut
document.addEventListener('keydown', e => {
  if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
    e.preventDefault();
    document.getElementById('globalSearch')?.focus();
  }
});

// ── Global search — filters page table rows AND navigates to sections
(function initGlobalSearch() {
  const searchInput = document.getElementById('globalSearch');
  if (!searchInput) return;

  // Map of page keywords → redirect URLs
  const pageMap = [
    { keywords: ['student', 'students'],    url: '/Admin/Students/Index' },
    { keywords: ['staff'],                  url: '/Admin/Staff/Index'    },
    { keywords: ['visitor', 'visitors'],    url: '/Admin/Visitors/Index' },
    { keywords: ['guard', 'guards'],        url: '/Admin/Guards/Index'   },
    { keywords: ['incident', 'incidents'],  url: '/Incidents/Index'      },
    { keywords: ['report', 'reports'],      url: '/Reports'              },
    { keywords: ['schedule'],               url: '/Schedule/Index'       },
    { keywords: ['scan', 'qr', 'scanner'],  url: '/Scan'                 },
    { keywords: ['dashboard'],              url: '/Dashboard'            },
    { keywords: ['setting', 'settings'],    url: '/Settings'             },
  ];

  let debounceTimer = null;

  searchInput.addEventListener('input', function () {
    clearTimeout(debounceTimer);
    const q = this.value.trim().toLowerCase();

    // ── 1. Filter visible table rows on the current page ─────────────
    const tables = document.querySelectorAll('.cs-table tbody');
    tables.forEach(tbody => {
      const rows = tbody.querySelectorAll('tr');
      rows.forEach(row => {
        const text = row.textContent.toLowerCase();
        row.style.display = q === '' || text.includes(q) ? '' : 'none';
      });
    });

    // ── 2. Also filter any local search inputs on the page ─────────
    const pageSearch = document.querySelector(
      '.cs-input[placeholder*="Search"]:not(#globalSearch)'
    );
    if (pageSearch) {
      pageSearch.value = q;
      pageSearch.dispatchEvent(new Event('input'));
    }

    // ── 3. After a short delay, navigate to a matching page ─────────
    if (q.length >= 3) {
      debounceTimer = setTimeout(() => {
        const lc = q;
        for (const entry of pageMap) {
          if (entry.keywords.some(kw => lc.includes(kw))) {
            // Only redirect if we are NOT already on that page
            if (!window.location.pathname.toLowerCase().includes(
                entry.url.split('/').pop().toLowerCase()
            )) {
              window.location.href = entry.url;
            }
            break;
          }
        }
      }, 800);
    }
  });

  // ── Wire up in-page search inputs to filter table rows ───────────
  document.querySelectorAll('.cs-input[placeholder*="Search"]').forEach(inp => {
    if (inp.id === 'globalSearch') return;
    inp.addEventListener('input', function () {
      const q = this.value.trim().toLowerCase();
      // Find the closest table
      const wrap = this.closest('.cs-animate') || document.body;
      const rows = wrap.querySelectorAll('.cs-table tbody tr');
      rows.forEach(row => {
        row.style.display = q === '' || row.textContent.toLowerCase().includes(q) ? '' : 'none';
      });
    });
  });
})();
