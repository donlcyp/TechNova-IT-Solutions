(function () {
    if (window.__logoutModalInitialized) return;
    window.__logoutModalInitialized = true;

    function getModalElements() {
        const overlay = document.getElementById('logoutModalOverlay');
        const cancel = document.getElementById('logoutCancelBtn');
        const confirm = document.getElementById('logoutConfirmBtn');
        return { overlay, cancel, confirm };
    }

    function openLogoutModal() {
        const { overlay } = getModalElements();
        if (!overlay) return;
        overlay.hidden = false;
        document.body.style.overflow = 'hidden';
    }

    function closeLogoutModal() {
        const { overlay } = getModalElements();
        if (!overlay) return;
        overlay.hidden = true;
        document.body.style.overflow = '';
    }

    function wireLogoutModal() {
        const { overlay } = getModalElements();
        if (overlay) {
            overlay.hidden = true;
        }

        document.addEventListener('click', function (event) {
            const target = event.target;
            if (!(target instanceof Element)) return;

            if (target.closest('.js-logout-trigger')) {
                event.preventDefault();
                openLogoutModal();
                return;
            }

            const { overlay: currentOverlay, cancel } = getModalElements();
            if (!currentOverlay) return;

            if (target === currentOverlay || target === cancel) {
                event.preventDefault();
                closeLogoutModal();
            }
        });

        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') {
                closeLogoutModal();
            }
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', wireLogoutModal);
    } else {
        wireLogoutModal();
    }
})();
