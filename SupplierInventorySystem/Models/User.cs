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
        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Column("email")]
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Column("password_hash")]
        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("full_name")]
        [StringLength(255)]
        public string? FullName { get; set; }

        [Column("role_id")]
        public int? RoleId { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("last_login")]
        public DateTime? LastLogin { get; set; }

        // Navigation properties
        [ForeignKey("RoleId")]
        public Role? Role { get; set; }
    }
}