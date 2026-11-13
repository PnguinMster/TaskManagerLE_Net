using System.ComponentModel.DataAnnotations;

namespace TaskMangerLE.Models;

public class ResetPasswordViewModel
{
    // Email and Code come from the link in the email and travel through the form as hidden fields.
    [Required] public string Email { get; set; } = string.Empty;
    [Required] public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter a new password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    [Required(ErrorMessage = "Enter your new password again.")]
    [Display(Name = "Confirm password")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}