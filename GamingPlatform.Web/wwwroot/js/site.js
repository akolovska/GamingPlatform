// GamingPlatform front-end behaviour (vanilla JS, no extra dependencies)
(function () {
    'use strict';

    // ---------- Toasts (flash messages) ----------
    function toast(message, kind) {
        var stack = document.getElementById('toast-stack');
        if (!stack) return;
        var el = document.createElement('div');
        el.className = 'toast-msg ' + (kind || '');
        el.textContent = message;
        stack.appendChild(el);
        setTimeout(function () {
            el.style.transition = 'opacity .3s, transform .3s';
            el.style.opacity = '0';
            el.style.transform = 'translateX(30px)';
            setTimeout(function () { el.remove(); }, 300);
        }, 4500);
    }
    window.gpToast = toast;

    document.querySelectorAll('[data-toast]').forEach(function (n) {
        toast(n.dataset.toast, n.dataset.kind);
    });

    // ---------- Confirm modal: <form data-confirm="Are you sure?"> ----------
    var confirmModalEl = document.getElementById('confirmModal');
    var pendingForm = null;
    if (confirmModalEl && window.bootstrap) {
        var confirmModal = new bootstrap.Modal(confirmModalEl);
        document.addEventListener('submit', function (e) {
            var form = e.target;
            if (!form.dataset || !form.dataset.confirm || form.dataset.confirmed) return;
            e.preventDefault();
            pendingForm = form;
            confirmModalEl.querySelector('.confirm-text').textContent = form.dataset.confirm;
            confirmModal.show();
        }, true);
        document.getElementById('confirmYes').addEventListener('click', function () {
            if (!pendingForm) return;
            pendingForm.dataset.confirmed = '1';
            confirmModal.hide();
            pendingForm.requestSubmit();
        });
    }

    // ---------- Button loading state on submit (prevents double posts) ----------
    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (e.defaultPrevented) return;
        // let jquery-validation block invalid forms first
        if (window.jQuery && jQuery(form).data('validator') && !jQuery(form).valid()) return;
        var btn = form.querySelector('button[type=submit]:not([data-no-loading])');
        if (btn) setTimeout(function () { btn.classList.add('is-loading'); }, 0);
    });

    // ---------- Broken image fallback ----------
    document.addEventListener('error', function (e) {
        var img = e.target;
        if (img.tagName !== 'IMG' || !img.closest('.cover, .detail-wrap')) return;
        var ph = document.createElement('div');
        ph.className = 'cover-placeholder';
        ph.textContent = 'No cover';
        img.replaceWith(ph);
    }, true);

    // ---------- Store: live search, genre chips, free-only, sort ----------
    var grid = document.getElementById('game-grid');
    if (grid) {
        var search = document.getElementById('game-search');
        var navSearch = document.getElementById('nav-search');
        var sortSel = document.getElementById('game-sort');
        var freeChip = document.getElementById('chip-free');
        var emptyMsg = document.getElementById('no-results');
        var count = document.getElementById('result-count');
        var cols = Array.prototype.slice.call(grid.children);

        function apply() {
            var q = ((search && search.value) || '').trim().toLowerCase();
            var free = freeChip && freeChip.classList.contains('active');
            var shown = 0;
            cols.forEach(function (c) {
                var ok = (!q || c.dataset.title.indexOf(q) > -1 || c.dataset.meta.indexOf(q) > -1) &&
                         (!free || parseFloat(c.dataset.price) === 0);
                c.classList.toggle('d-none', !ok);
                if (ok) shown++;
            });
            if (emptyMsg) emptyMsg.classList.toggle('d-none', shown !== 0);
            if (count) count.textContent = shown + (shown === 1 ? ' game' : ' games');
        }

        function sort() {
            var mode = sortSel ? sortSel.value : 'default';
            var sorted = cols.slice();
            var by = {
                'name': function (a, b) { return a.dataset.title.localeCompare(b.dataset.title); },
                'price-asc': function (a, b) { return a.dataset.price - b.dataset.price; },
                'price-desc': function (a, b) { return b.dataset.price - a.dataset.price; },
                'newest': function (a, b) { return (b.dataset.date || '').localeCompare(a.dataset.date || ''); }
            };
            if (by[mode]) sorted.sort(by[mode]);
            sorted.forEach(function (c) { grid.appendChild(c); });
        }

        if (search) search.addEventListener('input', apply);
        if (navSearch) {
            navSearch.addEventListener('input', function () { if (search) { search.value = navSearch.value; apply(); } });
        }
        if (sortSel) sortSel.addEventListener('change', sort);
        if (freeChip) freeChip.addEventListener('click', function () { freeChip.classList.toggle('active'); apply(); });
        apply();
    }

    // Navbar search on non-store pages goes to the store with a query
    var navForm = document.getElementById('nav-search-form');
    if (navForm && !document.getElementById('game-grid')) {
        navForm.addEventListener('submit', function (e) {
            e.preventDefault();
            var q = document.getElementById('nav-search').value;
            location.href = navForm.dataset.storeUrl + '?q=' + encodeURIComponent(q);
        });
    } else if (navForm) {
        navForm.addEventListener('submit', function (e) { e.preventDefault(); });
        var initial = new URLSearchParams(location.search).get('q');
        if (initial) {
            var s = document.getElementById('game-search'), n = document.getElementById('nav-search');
            if (s) { s.value = initial; s.dispatchEvent(new Event('input')); }
            if (n) n.value = initial;
        }
    }

    // ---------- Cover image live preview on game forms ----------
    var coverInput = document.getElementById('CoverImageUrl');
    var coverPreview = document.getElementById('cover-preview');
    if (coverInput && coverPreview) {
        var render = function () {
            var url = coverInput.value.trim();
            coverPreview.innerHTML = '';
            if (!url) { coverPreview.textContent = 'Cover preview'; return; }
            var img = new Image();
            img.onerror = function () { coverPreview.textContent = 'Could not load image'; };
            img.src = url;
            coverPreview.appendChild(img);
        };
        coverInput.addEventListener('input', render);
        render();
    }

    // ---------- Avatar upload preview ----------
    var avatarFile = document.getElementById('avatarFile');
    var avatarPreview = document.getElementById('avatarPreview');
    if (avatarFile && avatarPreview) {
        avatarFile.addEventListener('change', function () {
            var f = avatarFile.files[0];
            if (f) avatarPreview.src = URL.createObjectURL(f);
        });
    }

    // ---------- Password show/hide: <button data-toggle-password="#Input_Password"> ----------
    document.querySelectorAll('[data-toggle-password]').forEach(function (b) {
        b.addEventListener('click', function () {
            var input = document.querySelector(b.dataset.togglePassword);
            if (!input) return;
            var show = input.type === 'password';
            input.type = show ? 'text' : 'password';
            b.textContent = show ? 'Hide' : 'Show';
        });
    });

    // ---------- Tooltips ----------
    if (window.bootstrap) {
        document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) { new bootstrap.Tooltip(el); });
    }
})();
