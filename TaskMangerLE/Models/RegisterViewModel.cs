using System.ComponentModel.DataAnnotations;

namespace TaskMangerLE.Models;

public class RegisterViewModel
{
    [Required]
    [Display(Name = "Username")]
    [StringLength(30, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    [Required]
    [Display(Name = "Confirm password")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}