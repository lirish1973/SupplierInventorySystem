using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupplierInventorySystem.Models
{
    [Table("role_permissions")]
    public class RolePermission
    {
        [Column("role_id")]
        public int RoleId { get; set; }

        [Column("permission_id")]
        public int PermissionId { get; set; }

        // Navigation properties
        [ForeignKey("RoleId")]
        public Role? Role { get; set; }

        [ForeignKey("PermissionId")]
        public Permission? Permission { get; set; }
    }
}