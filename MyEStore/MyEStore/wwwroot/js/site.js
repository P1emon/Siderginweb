// Hàm cập nhật số lượng sản phẩm trong giỏ hàng
function updateCartCount() {
    $.get("/Cart/GetCartCount", function (data) {
        $("#cart-count").text(data);
    });
}

document.addEventListener('DOMContentLoaded', function () {
    const header = document.querySelector('header');
    const nav = document.querySelector('header nav');

    // Tạo overlay cho menu
    const mobileMenuOverlay = document.createElement('div');
    mobileMenuOverlay.className = 'mobile-menu-overlay';
    document.body.appendChild(mobileMenuOverlay);

    // Tạo nút hamburger menu
    const mobileMenuToggle = document.createElement('button');
    mobileMenuToggle.className = 'mobile-menu-toggle';
    mobileMenuToggle.innerHTML = '<i class="bi bi-list"></i>';
    mobileMenuToggle.setAttribute('aria-label', 'Menu chính');
    mobileMenuToggle.style.display = 'none';
    header.appendChild(mobileMenuToggle);

    // Tạo nút đóng menu mobile
    const mobileCloseBtn = document.createElement('button');
    mobileCloseBtn.className = 'mobile-close-btn';
    mobileCloseBtn.innerHTML = '<i class="bi bi-x-lg"></i>';
    mobileCloseBtn.setAttribute('aria-label', 'Đóng menu');
    mobileCloseBtn.style.display = 'none';

    // Hàm kiểm tra kích thước màn hình
    function checkScreenSize() {
        if (window.innerWidth <= 768) {
            if (!nav.classList.contains('mobile-nav-hidden') && !nav.classList.contains('mobile-nav-visible')) {
                nav.classList.add('mobile-nav-hidden');
            }
            mobileMenuToggle.style.display = 'flex';
        } else {
            nav.classList.remove('mobile-nav-hidden', 'mobile-nav-visible');
            mobileMenuToggle.style.display = 'none';
            mobileCloseBtn.style.display = 'none';
            mobileMenuOverlay.classList.remove('show');
            document.body.style.overflow = '';
            resetAllDropdowns();
        }
    }

    // Hàm reset tất cả dropdown
    function resetAllDropdowns() {
        document.querySelectorAll('.dropdown-menu').forEach(function (menu) {
            menu.classList.remove('show');
            menu.style.maxHeight = '';
        });
        document.querySelectorAll('.dropdown').forEach(function (dropdown) {
            dropdown.classList.remove('show');
        });
        document.querySelectorAll('.dropdown-toggle').forEach(function (toggle) {
            toggle.setAttribute('aria-expanded', 'false');
        });
    }

    // Toggle menu khi nhấn hamburger
    mobileMenuToggle.addEventListener('click', function () {
        if (nav.classList.contains('mobile-nav-visible')) {
            closeMobileMenu();
        } else {
            openMobileMenu();
        }
    });

    // Nút đóng menu mobile
    mobileCloseBtn.addEventListener('click', function () {
        closeMobileMenu();
    });

    // Click vào overlay thì đóng menu
    mobileMenuOverlay.addEventListener('click', function () {
        closeMobileMenu();
    });

    // Mở menu mobile
    function openMobileMenu() {
        nav.classList.remove('mobile-nav-hidden');
        nav.classList.add('mobile-nav-visible');

        if (!nav.contains(mobileCloseBtn)) {
            nav.appendChild(mobileCloseBtn);
        }
        mobileCloseBtn.style.display = 'flex';

        mobileMenuOverlay.classList.add('show');
        document.body.style.overflow = 'hidden';
        mobileMenuToggle.classList.add('active');
        resetAllDropdowns();
    }

    // Đóng menu mobile
    function closeMobileMenu() {
        nav.classList.remove('mobile-nav-visible');
        nav.classList.add('mobile-nav-hidden');

        mobileMenuOverlay.classList.remove('show');
        document.body.style.overflow = '';
        mobileMenuToggle.classList.remove('active');
        mobileCloseBtn.style.display = 'none';
        resetAllDropdowns();
    }

    // Khởi tạo dropdown menu
    function initializeDropdowns() {
        const dropdownToggles = document.querySelectorAll('.dropdown-toggle');

        dropdownToggles.forEach(function (toggle) {
            toggle.removeEventListener('click', handleDropdownClick);
            toggle.addEventListener('click', handleDropdownClick);
        });
    }

    function handleDropdownClick(e) {
        e.preventDefault();
        e.stopPropagation();

        const dropdown = this.closest('.dropdown');
        const dropdownMenu = this.nextElementSibling;

        if (dropdownMenu && dropdownMenu.classList.contains('dropdown-menu')) {
            const isMobile = window.innerWidth <= 768;
            const isCurrentlyOpen = dropdownMenu.classList.contains('show');

            document.querySelectorAll('.dropdown').forEach(function (otherDropdown) {
                if (otherDropdown !== dropdown) {
                    const otherMenu = otherDropdown.querySelector('.dropdown-menu');
                    const otherToggle = otherDropdown.querySelector('.dropdown-toggle');
                    if (otherMenu && otherToggle) {
                        otherMenu.classList.remove('show');
                        otherToggle.setAttribute('aria-expanded', 'false');
                        if (isMobile) {
                            otherMenu.style.maxHeight = '0px';
                        }
                    }
                }
            });

            if (isCurrentlyOpen) {
                dropdownMenu.classList.remove('show');
                this.setAttribute('aria-expanded', 'false');
                if (isMobile) {
                    dropdownMenu.style.maxHeight = '0px';
                }
            } else {
                dropdownMenu.classList.add('show');
                this.setAttribute('aria-expanded', 'true');
                if (isMobile) {
                    requestAnimationFrame(() => {
                        dropdownMenu.style.maxHeight = dropdownMenu.scrollHeight + 'px';
                    });
                }
            }
        }
    }

    // Đóng dropdown nếu click bên ngoài (trừ mobile)
    document.addEventListener('click', function (e) {
        if (window.innerWidth > 768 && !e.target.closest('.dropdown')) {
            resetAllDropdowns();
        }
    });

    // Đóng dropdown nếu nhấn ESC
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            resetAllDropdowns();
        }
    });

    // Khởi tạo khi load
    checkScreenSize();
    initializeDropdowns();

    window.addEventListener('resize', function () {
        checkScreenSize();
        initializeDropdowns();
    });
});

// Gọi khi trang được tải
$(document).ready(function () {
    updateCartCount();
});
