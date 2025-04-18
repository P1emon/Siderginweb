
using System.ComponentModel.DataAnnotations;

namespace MyEStore.Models
{
    public class RegisterVM
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "Tên đăng nhập phải từ 5 đến 20 ký tự")]
        public string UserName { get; set; } = null!;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 100 ký tự")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Họ và tên không được để trống")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(15, MinimumLength = 9, ErrorMessage = "Số điện thoại phải từ 9 đến 15 chữ số")]
        public string PhoneNumber { get; set; } = null!;

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        [StringLength(100, ErrorMessage = "Địa chỉ tối đa 50 ký tự")]
        public string Address { get; set; } = null!;
    }
}
