using System.ComponentModel.DataAnnotations;

namespace DouyinContentGenerator.Core.Models;

public class Role
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty; // Admin, Operator
    
    // Navigation property
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
