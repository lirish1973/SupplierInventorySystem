using System.ComponentModel.DataAnnotations;

namespace SupplierInventorySystem.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "סיסמה ישנה היא שדה חובה")]
        [DataType(DataType.Password)]
        [Display(Name = "סיסמה ישנה")]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "סיסמה חדשה היא שדה חובה")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "הסיסמה חייבת להכיל לפחות 6 תווים")]
        [DataType(DataType.Password)]
        [Display(Name = "סיסמה חדשה")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "אימות סיסמה הוא שדה חובה")]
        [DataType(DataType.Password)]
        [Display(Name = "אימות סיסמה חדשה")]
        [Compare("NewPassword", ErrorMessage = "הסיסמאות אינן תואמות")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
