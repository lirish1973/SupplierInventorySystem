using System.ComponentModel.DataAnnotations;

namespace SupplierInventorySystem.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "דוא״ל הוא שדה חובה")]
        [EmailAddress(ErrorMessage = "כתובת דוא״ל לא תקינה")]
        [Display(Name = "דוא״ל")]
        public string Email { get; set; } = string.Empty;
    }
}
