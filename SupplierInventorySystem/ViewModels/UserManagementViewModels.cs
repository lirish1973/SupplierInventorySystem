using System.ComponentModel.DataAnnotations;
using SupplierInventorySystem.Models;

namespace SupplierInventorySystem.ViewModels
{
    // ViewModel for user list display
    public class UserListViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? RoleName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsLocked { get; set; }
    }

    // ViewModel for creating a new user
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "שם משתמש הוא שדה חובה")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "שם משתמש חייב להיות בין 3 ל-100 תווים")]
        [Display(Name = "שם משתמש")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "דוא״ל הוא שדה חובה")]
        [EmailAddress(ErrorMessage = "כתובת דוא״ל לא תקינה")]
        [Display(Name = "דוא״ל")]
        public string Email { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "שם מלא")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "סיסמה היא שדה חובה")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "סיסמה חייבת להיות לפחות 6 תווים")]
        [DataType(DataType.Password)]
        [Display(Name = "סיסמה")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "אימות סיסמה הוא שדה חובה")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "הסיסמאות אינן תואמות")]
        [Display(Name = "אימות סיסמה")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "תפקיד")]
        public int? RoleId { get; set; }

        [Display(Name = "פעיל")]
        public bool IsActive { get; set; } = true;
    }

    // ViewModel for editing a user
    public class EditUserViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "שם משתמש הוא שדה חובה")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "שם משתמש חייב להיות בין 3 ל-100 תווים")]
        [Display(Name = "שם משתמש")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "דוא״ל הוא שדה חובה")]
        [EmailAddress(ErrorMessage = "כתובת דוא״ל לא תקינה")]
        [Display(Name = "דוא״ל")]
        public string Email { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "שם מלא")]
        public string? FullName { get; set; }

        [Display(Name = "תפקיד")]
        public int? RoleId { get; set; }

        [Display(Name = "פעיל")]
        public bool IsActive { get; set; }

        // For display purposes
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
    }

    // ViewModel for resetting user password (by admin)
    public class AdminResetPasswordViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "סיסמה חדשה היא שדה חובה")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "סיסמה חייבת להיות לפחות 6 תווים")]
        [DataType(DataType.Password)]
        [Display(Name = "סיסמה חדשה")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "אימות סיסמה הוא שדה חובה")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "הסיסמאות אינן תואמות")]
        [Display(Name = "אימות סיסמה")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // ViewModel for role management
    public class RoleViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "שם התפקיד הוא שדה חובה")]
        [StringLength(100)]
        [Display(Name = "שם תפקיד")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "תיאור")]
        public string? Description { get; set; }

        public int UserCount { get; set; }
    }

    // ViewModel for user details
    public class UserDetailsViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public Role? Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public bool IsLocked => LockoutEnd.HasValue && LockoutEnd.Value > DateTime.Now;
    }
}
