using System.ComponentModel.DataAnnotations;

namespace TaskMangerLE.Models;

public class LoginViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    [Display(Name = "Remember me")] public bool RememberMe { get; set; } = default;
    public string? ReturnUrl { get; set; }
}