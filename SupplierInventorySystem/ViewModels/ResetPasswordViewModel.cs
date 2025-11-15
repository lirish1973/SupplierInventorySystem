using System.ComponentModel.DataAnnotations;

namespace SupplierInventorySystem.ViewModels
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "סיסמה חדשה היא שדה חובה")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "הסיסמה חייבת להכיל לפחות 6 תווים")]
        [DataType(DataType.Password)]
        [Display(Name = "סיסמה חדשה")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "אימות סיסמה הוא שדה חובה")]
        [DataType(DataType.Password)]
        [Display(Name = "אימות סיסמה")]
        [Compare("Password", ErrorMessage = "הסיסמאות אינן תואמות")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
