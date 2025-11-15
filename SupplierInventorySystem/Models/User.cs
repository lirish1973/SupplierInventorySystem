using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupplierInventorySystem.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("username")]
        [Required(ErrorMessage = "שם משתמש הוא שדה חובה")]
        [StringLength(100)]
        [Display(Name = "שם משתמש")]
        public string Username { get; set; } = string.Empty;

        [Column("email")]
        [Required(ErrorMessage = "דוא״ל הוא שדה חובה")]
        [EmailAddress(ErrorMessage = "כתובת דוא״ל לא תקינה")]
        [StringLength(255)]
        [Display(Name = "דוא״ל")]
        public string Email { get; set; } = string.Empty;

        [Column("password_hash")]
        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("full_name")]
        [StringLength(255)]
        [Display(Name = "שם מלא")]
        public string? FullName { get; set; }

        [Column("role_id")]
        [Display(Name = "תפקיד")]
        public int? RoleId { get; set; }

        [Column("is_active")]
        [Display(Name = "פעיל")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        [Display(Name = "תאריך יצירה")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("last_login")]
        [Display(Name = "התחברות אחרונה")]
        public DateTime? LastLogin { get; set; }

        // ⭐ השדות החדשים להתחברות ואבטחה
        [Column("reset_token")]
        [StringLength(255)]
        public string? ResetToken { get; set; }

        [Column("reset_token_expiry")]
        public DateTime? ResetTokenExpiry { get; set; }

        [Column("failed_login_attempts")]
        public int FailedLoginAttempts { get; set; } = 0;

        [Column("lockout_end")]
        public DateTime? LockoutEnd { get; set; }

        // Navigation properties
        [ForeignKey("RoleId")]
        public Role? Role { get; set; }

        // Not mapped - for forms only
        [NotMapped]
        [DataType(DataType.Password)]
        [Display(Name = "סיסמה")]
        public string? Password { get; set; }

        [NotMapped]
        [DataType(DataType.Password)]
        [Display(Name = "אימות סיסמה")]
        [Compare("Password", ErrorMessage = "הסיסמאות אינן תואמות")]
        public string? ConfirmPassword { get; set; }
    }
}