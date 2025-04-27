using System.ComponentModel.DataAnnotations;

namespace MyEStore.Models
{
    public class ProfileVM
    {
        public string? UserName { get; set; }

        [Required(ErrorMessage = "Họ và Tên là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Họ và Tên không được vượt quá 100 ký tự.")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(256, ErrorMessage = "Email không được vượt quá 256 ký tự.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [RegularExpression(@"^(0|\+84)(3|5|7|8|9)[0-9]{8}$", ErrorMessage = "Số điện thoại không hợp lệ. Vui lòng nhập số điện thoại Việt Nam hợp lệ (ví dụ: 0901234567).")]
        public string PhoneNumber { get; set; } = null!;

        [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự và không vượt quá 100 ký tự.")]
        public string? Password { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự và không vượt quá 100 ký tự.")]
        public string? NewPassword { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu xác nhận phải có ít nhất 6 ký tự và không vượt quá 100 ký tự.")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp với mật khẩu mới.")]
        public string? ConfirmPassword { get; set; }

        public string DiaChi { get; set; } // Thêm để lưu địa chỉ
        public string DiaChiPhu { get; set; } // Thêm để lưu địa chỉ phụ
        public List<Ranks> Ranks { get; set; } = new List<Ranks>();
    }
    public class ProfileUpdateVM
    {
        [Required(ErrorMessage = "Họ và Tên là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Họ và Tên không được vượt quá 100 ký tự.")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(256, ErrorMessage = "Email không được vượt quá 256 ký tự.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [RegularExpression(@"^(0|\+84)(3|5|7|8|9)[0-9]{8}$", ErrorMessage = "Số điện thoại không hợp lệ. Vui lòng nhập số điện thoại Việt Nam hợp lệ (ví dụ: 0901234567).")]
        public string PhoneNumber { get; set; } = null!;
    }
    public class ChangePasswordVM
    {
        [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự và không vượt quá 100 ký tự.")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự và không vượt quá 100 ký tự.")]
        public string NewPassword { get; set; } = null!;

        [Required(ErrorMessage = "Mật khẩu xác nhận là bắt buộc.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu xác nhận phải có ít nhất 6 ký tự và không vượt quá 100 ký tự.")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp với mật khẩu mới.")]
        public string ConfirmPassword { get; set; } = null!;
    }
    public class Ranks
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public List<string> Benefits { get; set; }
    }
}
