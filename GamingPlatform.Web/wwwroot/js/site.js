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
            el.classList.add('leaving'); // leaves along the way it came in (see .toast-msg in site.css)
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

    // ---------- Store filters: <select data-autosubmit>, <input type=checkbox data-autosubmit> ----------
    // The store searches on the server; changing a filter just submits its form (it works without JS too).
    document.querySelectorAll('[data-autosubmit]').forEach(function (el) {
        el.addEventListener('change', function () { if (el.form) el.form.requestSubmit(); });
    });

    // ---------- Cover image live preview on game forms ----------
    var coverInput = document.getElementById('CoverFile');
    var coverPreview = document.getElementById('cover-preview');
    if (coverInput && coverPreview) {
        var render = function (url) {
            coverPreview.innerHTML = '';
            if (!url) { coverPreview.textContent = 'Cover preview'; return; }
            var img = new Image();
            img.onerror = function () { coverPreview.textContent = 'Could not load image'; };
            img.src = url;
            coverPreview.appendChild(img);
        };
        coverInput.addEventListener('change', function () {
            var f = coverInput.files[0];
            render(f ? URL.createObjectURL(f) : coverPreview.dataset.current);
        });
        render(coverPreview.dataset.current); // existing cover on Edit
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
