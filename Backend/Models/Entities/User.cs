using System.ComponentModel.DataAnnotations;
namespace Backend.Models.Entities;

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(32)]
    public string Username { get; set; } = null!;

    [Required]
    public string PasswordHash { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}