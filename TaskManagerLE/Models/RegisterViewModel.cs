using System.ComponentModel.DataAnnotations;

namespace TaskManagerLE.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Enter a username.")]
    [Display(Name = "Username")]
    [StringLength(30, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 30 characters.")]
    public string UserName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [MaxLength(256, ErrorMessage = "Email can be at most 256 characters.")]
    public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Enter a password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    [Required(ErrorMessage = "Enter your password again.")]
    [Display(Name = "Confirm password")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
