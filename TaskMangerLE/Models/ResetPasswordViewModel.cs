using System.ComponentModel.DataAnnotations;

namespace TaskMangerLE.Models;

// not final, logic needs to be implemented
// this is temporary
public class ResetPasswordViewModel
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    [Required]
    [Display(Name = "Confirm password")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}