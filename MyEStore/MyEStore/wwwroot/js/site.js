// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Hàm cập nhật số lượng sản phẩm trong giỏ hàng
function updateCartCount() {
    $.get("/Cart/GetCartCount", function (data) {
        $("#cart-count").text(data); // Cập nhật số trên badge
    });
}


// JavaScript cải tiến cho menu responsive
document.addEventListener('DOMContentLoaded', function () {
    // Tạo các phần tử cần thiết cho menu mobile đẹp hơn
    const header = document.querySelector('header');
    const nav = document.querySelector('header nav');

    // Tạo overlay cho menu
    const mobileMenuOverlay = document.createElement('div');
    mobileMenuOverlay.className = 'mobile-menu-overlay';
    document.body.appendChild(mobileMenuOverlay);

    // Tạo nút hamburger menu đẹp hơn
    const mobileMenuToggle = document.createElement('button');
    mobileMenuToggle.className = 'mobile-menu-toggle';
    mobileMenuToggle.innerHTML = '<i class="bi bi-list"></i>';
    mobileMenuToggle.setAttribute('aria-label', 'Menu chính');
    mobileMenuToggle.style.display = 'none'; // Ẩn mặc định
    header.appendChild(mobileMenuToggle);

    // Tạo nút đóng riêng cho menu mobile
    const mobileCloseBtn = document.createElement('button');
    mobileCloseBtn.className = 'mobile-close-btn';
    mobileCloseBtn.innerHTML = '<i class="bi bi-x-lg"></i>';
    mobileCloseBtn.setAttribute('aria-label', 'Đóng menu');
    mobileCloseBtn.style.display = 'none'; // Ẩn mặc định

    // Hàm kiểm tra kích thước màn hình và áp dụng menu mobile nếu cần
    function checkScreenSize() {
        if (window.innerWidth <= 768) {
            // Ẩn menu khi ở mobile
            if (!nav.classList.contains('mobile-nav-hidden') && !nav.classList.contains('mobile-nav-visible')) {
                nav.classList.add('mobile-nav-hidden');
            }
            mobileMenuToggle.style.display = 'flex';
        } else {
            // Hiển thị menu bình thường trên desktop
            nav.classList.remove('mobile-nav-hidden', 'mobile-nav-visible');
            mobileMenuToggle.style.display = 'none';
            mobileCloseBtn.style.display = 'none';
            mobileMenuOverlay.classList.remove('show');
            document.body.style.overflow = '';
        }
    }

    // Thêm sự kiện cho nút menu hamburger
    mobileMenuToggle.addEventListener('click', function () {
        openMobileMenu();
    });

    // Thêm sự kiện cho nút đóng menu
    mobileCloseBtn.addEventListener('click', function () {
        closeMobileMenu();
    });

    // Thêm sự kiện cho overlay
    mobileMenuOverlay.addEventListener('click', function () {
        closeMobileMenu();
    });

    // Hàm mở menu mobile với animation
    function openMobileMenu() {
        nav.classList.remove('mobile-nav-hidden');
        nav.classList.add('mobile-nav-visible');

        // Thêm nút đóng vào nav nếu chưa có
        if (!nav.contains(mobileCloseBtn)) {
            nav.appendChild(mobileCloseBtn);
        }
        mobileCloseBtn.style.display = 'flex';

        // Hiển thị overlay và cố định body
        mobileMenuOverlay.classList.add('show');
        document.body.style.overflow = 'hidden';

        // Hiệu ứng cho nút menu
        mobileMenuToggle.classList.add('active');

        // Bật animation cho các menu item
        setTimeout(() => {
            const navItems = nav.querySelectorAll('.nav-item');
            navItems.forEach((item, index) => {
                item.style.animationDelay = (0.1 + index * 0.1) + 's';
            });
        }, 100);
    }

    // Hàm đóng menu mobile với animation
    function closeMobileMenu() {
        // Thêm hiệu ứng đóng menu
        nav.style.animation = 'fadeSlideOut 0.3s ease forwards';

        setTimeout(() => {
            nav.classList.remove('mobile-nav-visible');
            nav.classList.add('mobile-nav-hidden');
            nav.style.animation = '';

            // Ẩn overlay và cho phép cuộn lại
            mobileMenuOverlay.classList.remove('show');
            document.body.style.overflow = '';

            // Đặt lại trạng thái nút menu
            mobileMenuToggle.classList.remove('active');
            mobileCloseBtn.style.display = 'none';
        }, 280);
    }

    // Xử lý dropdown menu trên mobile - cải tiến
    const dropdownToggles = document.querySelectorAll('.dropdown-toggle');

    dropdownToggles.forEach(function (toggle) {
        toggle.addEventListener('click', function (e) {
            if (window.innerWidth <= 768) {
                e.preventDefault();
                e.stopPropagation();

                const dropdown = this.closest('.dropdown');
                const dropdownMenu = this.nextElementSibling;

                // Toggle dropdown menu với hiệu ứng slide
                if (dropdownMenu.classList.contains('show')) {
                    // Đóng dropdown này
                    this.setAttribute('aria-expanded', 'false');
                    dropdownMenu.style.maxHeight = '0px';

                    setTimeout(() => {
                        dropdownMenu.classList.remove('show');
                    }, 300);

                    dropdown.classList.remove('active');
                } else {
                    // Đóng tất cả các dropdown khác trước
                    document.querySelectorAll('.dropdown-menu.show').forEach(function (menu) {
                        const parentToggle = menu.previousElementSibling;
                        const parentDropdown = menu.closest('.dropdown');

                        parentToggle.setAttribute('aria-expanded', 'false');
                        menu.style.maxHeight = '0px';

                        setTimeout(() => {
                            menu.classList.remove('show');
                        }, 300);

                        parentDropdown.classList.remove('active');
                    });

                    // Mở dropdown này
                    this.setAttribute('aria-expanded', 'true');
                    dropdownMenu.classList.add('show');
                    dropdownMenu.style.maxHeight = dropdownMenu.scrollHeight + 'px';
                    dropdown.classList.add('active');
                }
            }
        });
    });

    // Xử lý đóng dropdown khi click bên ngoài
    document.addEventListener('click', function (e) {
        if (window.innerWidth > 768) {
            if (!e.target.closest('.dropdown')) {
                document.querySelectorAll('.dropdown-menu.show').forEach(function (menu) {
                    menu.classList.remove('show');
                });
            }
        }
    });

    // Xử lý đóng menu mobile khi click vào item
    const navLinks = document.querySelectorAll('.nav-link:not(.dropdown-toggle)');

    navLinks.forEach(function (link) {
        link.addEventListener('click', function () {
            if (window.innerWidth <= 768 && nav.classList.contains('mobile-nav-visible')) {
                closeMobileMenu();
            }
        });
    });

    // Đảm bảo dropdown item cũng đóng menu khi được click
    const dropdownItems = document.querySelectorAll('.dropdown-item');

    dropdownItems.forEach(function (item) {
        item.addEventListener('click', function () {
            if (window.innerWidth <= 768 && nav.classList.contains('mobile-nav-visible')) {
                closeMobileMenu();
            }
        });
    });

    // Thêm animation keyframes cho menu đóng
    const style = document.createElement('style');
    style.innerHTML = `
        @keyframes fadeSlideOut {
            from {
                opacity: 1;
                transform: translateY(0);
            }
            to {
                opacity: 0;
                transform: translateY(-20px);
            }
        }
    `;
    document.head.appendChild(style);

    // Kiểm tra kích thước màn hình khi trang tải và khi thay đổi kích thước
    checkScreenSize();
    window.addEventListener('resize', function () {
        checkScreenSize();

        // Đặt lại các dropdown khi thay đổi kích thước
        if (window.innerWidth > 768) {
            document.querySelectorAll('.dropdown-menu').forEach(function (menu) {
                menu.style.maxHeight = '';
            });
        }
    });

    // Xử lý swipe để đóng menu trên thiết bị cảm ứng
    let touchStartX = 0;
    let touchEndX = 0;

    if (nav) {
        nav.addEventListener('touchstart', function (e) {
            touchStartX = e.changedTouches[0].screenX;
        }, { passive: true });

        nav.addEventListener('touchend', function (e) {
            touchEndX = e.changedTouches[0].screenX;
            handleSwipe();
        }, { passive: true });
    }

    function handleSwipe() {
        // Swipe phải để đóng menu (khoảng cách > 100px)
        if (touchEndX - touchStartX > 100 && nav.classList.contains('mobile-nav-visible')) {
            closeMobileMenu();
        }
    }
});
// Gọi hàm này khi trang được tải
$(document).ready(function () {
    updateCartCount();
});
