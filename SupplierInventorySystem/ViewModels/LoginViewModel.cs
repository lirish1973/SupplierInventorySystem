using System.ComponentModel.DataAnnotations;

namespace SupplierInventorySystem.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "שם משתמש הוא שדה חובה")]
        [Display(Name = "שם משתמש")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "סיסמה היא שדה חובה")]
        [DataType(DataType.Password)]
        [Display(Name = "סיסמה")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "זכור אותי")]
        public bool RememberMe { get; set; }
    }
}