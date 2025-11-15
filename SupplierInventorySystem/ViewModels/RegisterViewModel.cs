using System.ComponentModel.DataAnnotations;

namespace SupplierInventorySystem.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "שם משתמש הוא שדה חובה")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "שם משתמש חייב להיות בין 3 ל-100 תווים")]
        [Display(Name = "שם משתמש")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "דוא״ל הוא שדה חובה")]
        [EmailAddress(ErrorMessage = "כתובת דוא״ל לא תקינה")]
        [Display(Name = "דוא״ל")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "שם מלא הוא שדה חובה")]
        [StringLength(255, MinimumLength = 2, ErrorMessage = "שם מלא חייב להיות בין 2 ל-255 תווים")]
        [Display(Name = "שם מלא")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "סיסמה היא שדה חובה")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "הסיסמה חייבת להכיל לפחות 6 תווים")]
        [DataType(DataType.Password)]
        [Display(Name = "סיסמה")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "אימות סיסמה הוא שדה חובה")]
        [DataType(DataType.Password)]
        [Display(Name = "אימות סיסמה")]
        [Compare("Password", ErrorMessage = "הסיסמאות אינן תואמות")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
