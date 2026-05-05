(function () {
    const sidebarId = 'superAdminSidebar';
    const navGroupSelector = '#superAdminNav .nav-group';

    function getSidebar() {
        return document.getElementById(sidebarId);
    }

    function setBodyCollapsed(isCollapsed) {
        document.body.classList.toggle('sidebar-collapsed', isCollapsed);
    }

    function applyInitialSidebarState() {
        const sidebar = getSidebar();
        if (!sidebar) return;
        const isCollapsed = localStorage.getItem('superAdminSidebarCollapsed') === 'true';
        sidebar.classList.toggle('collapsed', isCollapsed);
        setBodyCollapsed(isCollapsed);
        sidebar.classList.remove('notransition');
    }

    function toggleSidebar() {
        const sidebar = getSidebar();
        if (!sidebar) return;
        const isCollapsed = sidebar.classList.toggle('collapsed');
        localStorage.setItem('superAdminSidebarCollapsed', isCollapsed);
        setBodyCollapsed(isCollapsed);
    }

    function toggleNavGroup(btn) {
        if (!btn) return;
        const sidebar = getSidebar();
        if (sidebar && sidebar.classList.contains('collapsed')) return;
        const group = btn.closest('.nav-group');
        if (!group) return;

        group.classList.toggle('open');

        const allGroups = document.querySelectorAll(navGroupSelector);
        const index = Array.from(allGroups).indexOf(group);
        if (index >= 0) {
            localStorage.setItem('superAdminNavGroup_' + index, group.classList.contains('open'));
        }
    }

    function toggleMobileSidebar() {
        const sidebar = getSidebar();
        const overlay = document.getElementById('mobileOverlay');
        if (!sidebar || !overlay) return;
        sidebar.classList.toggle('mobile-open');
        overlay.classList.toggle('active');
        document.body.style.overflow = sidebar.classList.contains('mobile-open') ? 'hidden' : '';
    }

    function closeMobileSidebar() {
        const sidebar = getSidebar();
        const overlay = document.getElementById('mobileOverlay');
        if (!sidebar || !overlay) return;
        sidebar.classList.remove('mobile-open');
        overlay.classList.remove('active');
        document.body.style.overflow = '';
    }

    function restoreNavGroups() {
        document.querySelectorAll(navGroupSelector).forEach(function (group, index) {
            const saved = localStorage.getItem('superAdminNavGroup_' + index);
            if (saved === 'true') {
                group.classList.add('open');
            } else if (saved === 'false') {
                group.classList.remove('open');
            }
        });
    }

    function wireSidebarHandlers() {
        const toggleBtn = document.querySelector('#superAdminSidebar .toggle-btn');
        if (toggleBtn) {
            toggleBtn.addEventListener('click', toggleSidebar);
        }

        const mobileBtn = document.getElementById('mobileMenuBtn');
        if (mobileBtn) {
            mobileBtn.addEventListener('click', toggleMobileSidebar);
        }

        const overlay = document.getElementById('mobileOverlay');
        if (overlay) {
            overlay.addEventListener('click', closeMobileSidebar);
        }

        document.querySelectorAll('#superAdminSidebar .nav-group-header').forEach(function (btn) {
            btn.addEventListener('click', function () {
                toggleNavGroup(btn);
            });
        });

        document.querySelectorAll('#superAdminSidebar .nav-item').forEach(function (item) {
            item.addEventListener('click', function () {
                if (window.innerWidth <= 991) {
                    closeMobileSidebar();
                }
            });
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        applyInitialSidebarState();
        restoreNavGroups();
        wireSidebarHandlers();
    });

    window.addEventListener('resize', function () {
        if (window.innerWidth > 991) {
            closeMobileSidebar();
        }
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') {
            closeMobileSidebar();
        }
    });
})();
