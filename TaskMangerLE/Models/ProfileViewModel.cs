using System.ComponentModel.DataAnnotations;

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

    [Required]
    [EmailAddress]
    [MaxLength(50)]
    [Display(Name = "Email")]
    public string NewEmail { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "The new passwords do not match.")]
    [Display(Name = "Confirm new password")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}