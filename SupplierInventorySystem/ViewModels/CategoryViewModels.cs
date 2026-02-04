using System.ComponentModel.DataAnnotations;

namespace SupplierInventorySystem.ViewModels
{
    public class CategoryListViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ParentName { get; set; }
        public int? ParentId { get; set; }
        public int ProductCount { get; set; }
        public int SubCategoryCount { get; set; }
        public int Level { get; set; } // Depth in hierarchy
    }

    public class CategoryFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "שם הקטגוריה הוא שדה חובה")]
        [StringLength(255, MinimumLength = 2, ErrorMessage = "שם הקטגוריה חייב להיות בין 2 ל-255 תווים")]
        [Display(Name = "שם קטגוריה")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "קטגוריית אב")]
        public int? ParentId { get; set; }

        [Display(Name = "תיאור")]
        public string? Description { get; set; }
    }

    public class CategoryTreeViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public List<CategoryTreeViewModel> Children { get; set; } = new();
    }
}
