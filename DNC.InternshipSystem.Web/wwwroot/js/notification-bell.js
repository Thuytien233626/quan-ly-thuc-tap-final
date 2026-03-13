/**
 * Notification Bell Component
 * Dung chung cho Student, Lecturer, Admin
 * Su dung localStorage de luu trang thai "da doc" va "da xoa"
 */
(function () {
    'use strict';

    const STORAGE_KEY_READ = 'notif_read';
    const STORAGE_KEY_DISMISSED = 'notif_dismissed';

    function getSet(key) {
        try {
            return new Set(JSON.parse(localStorage.getItem(key) || '[]'));
        } catch { return new Set(); }
    }

    function saveSet(key, s) {
        localStorage.setItem(key, JSON.stringify([...s]));
    }

    function getNotifId(n) {
        return btoa(unescape(encodeURIComponent(n.title + '|' + n.message))).substring(0, 32);
    }

    function initNotificationBell(apiUrl) {
        const bell = document.getElementById('notifBell');
        const badge = document.getElementById('notifBadge');
        const listEl = document.getElementById('notifList');
        const emptyEl = document.getElementById('notifEmpty');
        const btnReadAll = document.getElementById('notifReadAll');
        const btnClearAll = document.getElementById('notifClearAll');

        if (!bell || !listEl) return;

        let allNotifs = [];

        async function load() {
            try {
                const res = await fetch(apiUrl);
                const data = await res.json();
                const dismissed = getSet(STORAGE_KEY_DISMISSED);

                // Loc bo thong bao da xoa
                allNotifs = data.filter(n => !dismissed.has(getNotifId(n)));
                render();
            } catch (e) {
                console.error('Notification error:', e);
            }
        }

        function render() {
            const readSet = getSet(STORAGE_KEY_READ);
            const unreadCount = allNotifs.filter(n => !readSet.has(getNotifId(n))).length;

            // Badge
            if (unreadCount > 0) {
                badge.textContent = unreadCount > 9 ? '9+' : unreadCount;
                badge.classList.remove('d-none');
            } else {
                badge.classList.add('d-none');
            }

            // Buttons
            if (btnReadAll) btnReadAll.style.display = allNotifs.length > 0 ? '' : 'none';
            if (btnClearAll) btnClearAll.style.display = allNotifs.length > 0 ? '' : 'none';

            // List
            if (allNotifs.length === 0) {
                listEl.innerHTML = '';
                if (emptyEl) emptyEl.style.display = '';
                return;
            }
            if (emptyEl) emptyEl.style.display = 'none';

            listEl.innerHTML = allNotifs.map(n => {
                const id = getNotifId(n);
                const isRead = readSet.has(id);
                return `
                    <div class="notif-item d-flex gap-2 p-2 rounded position-relative ${isRead ? 'notif-read' : 'notif-unread'}" data-nid="${id}">
                        <div class="rounded-circle d-flex align-items-center justify-content-center flex-shrink-0 notif-icon-${n.color}"
                             style="width: 34px; height: 34px; font-size: 0.8rem;">
                            <i class="fa ${n.icon}"></i>
                        </div>
                        <div class="flex-grow-1 overflow-hidden">
                            <div class="fw-semibold small text-truncate">${n.title}</div>
                            <div class="text-muted" style="font-size: 0.72rem; line-height: 1.3;">${n.message}</div>
                            ${n.time ? `<div class="text-muted mt-1" style="font-size: 0.65rem;"><i class="fa fa-clock me-1"></i>${n.time}</div>` : ''}
                        </div>
                        <button class="btn btn-link btn-sm text-muted p-0 notif-dismiss" title="Xóa" data-nid="${id}"
                                style="position:absolute; top:4px; right:6px; font-size:0.7rem; opacity:0; transition: opacity 0.15s;">
                            <i class="fa fa-times"></i>
                        </button>
                        ${!isRead ? '<span class="notif-dot"></span>' : ''}
                    </div>
                `;
            }).join('');

            // Click → danh dau da doc
            listEl.querySelectorAll('.notif-item').forEach(el => {
                el.addEventListener('click', function (e) {
                    if (e.target.closest('.notif-dismiss')) return;
                    const nid = this.dataset.nid;
                    const r = getSet(STORAGE_KEY_READ);
                    r.add(nid);
                    saveSet(STORAGE_KEY_READ, r);
                    render();
                });
            });

            // X button → xoa
            listEl.querySelectorAll('.notif-dismiss').forEach(btn => {
                btn.addEventListener('click', function (e) {
                    e.stopPropagation();
                    const nid = this.dataset.nid;
                    const d = getSet(STORAGE_KEY_DISMISSED);
                    d.add(nid);
                    saveSet(STORAGE_KEY_DISMISSED, d);
                    allNotifs = allNotifs.filter(n => getNotifId(n) !== nid);
                    render();
                });
            });
        }

        // Doc tat ca
        if (btnReadAll) {
            btnReadAll.addEventListener('click', function (e) {
                e.stopPropagation();
                const r = getSet(STORAGE_KEY_READ);
                allNotifs.forEach(n => r.add(getNotifId(n)));
                saveSet(STORAGE_KEY_READ, r);
                render();
            });
        }

        // Xoa tat ca
        if (btnClearAll) {
            btnClearAll.addEventListener('click', function (e) {
                e.stopPropagation();
                const d = getSet(STORAGE_KEY_DISMISSED);
                allNotifs.forEach(n => d.add(getNotifId(n)));
                saveSet(STORAGE_KEY_DISMISSED, d);
                allNotifs = [];
                render();
            });
        }

        load();
    }

    window.initNotificationBell = initNotificationBell;
})();
