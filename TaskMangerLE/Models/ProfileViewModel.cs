using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace TaskMangerLE.Models;

public class ProfileViewModel
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime MemberSinceUtc { get; set; }

    [Required]
    [StringLength(30, MinimumLength = 3)]
    [Display(Name = "Username")]
    public string NewUserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [MaxLength(256, ErrorMessage = "Email can be at most 256 characters.")]
    [Display(Name = "Email")]
    public string NewEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your current password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

    // Lets the page render the change-password card from this one model. [ValidateNever] stops the
    // details form (which doesn't post these fields) from failing on the nested [Required] rules.
    [ValidateNever]
    public ChangePasswordViewModel ChangePassword { get; set; } = new();
}