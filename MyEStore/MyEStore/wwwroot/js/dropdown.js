// Tìm tất cả các dropdown-toggle
const dropdownToggles = document.querySelectorAll('.dropdown-toggle');

// Thêm event listener cho mỗi dropdown toggle
dropdownToggles.forEach(toggle => {
    toggle.addEventListener('click', function (e) {
        e.preventDefault();
        e.stopPropagation();

        // Tìm dropdown cha
        const dropdown = this.closest('.dropdown');

        // Toggle class "show"
        dropdown.classList.toggle('show');

        // Tìm dropdown-menu và toggle class "show"
        const menu = dropdown.querySelector('.dropdown-menu');
        if (menu) {
            menu.classList.toggle('show');
        }
    });
});

// Đóng dropdown khi click bên ngoài
document.addEventListener('click', function (e) {
    const dropdowns = document.querySelectorAll('.dropdown.show');
    dropdowns.forEach(dropdown => {
        if (!dropdown.contains(e.target)) {
            dropdown.classList.remove('show');
            const menu = dropdown.querySelector('.dropdown-menu');
            if (menu) {
                menu.classList.remove('show');
            }
        }
    });
});