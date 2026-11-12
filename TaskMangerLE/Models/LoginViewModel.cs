using System.ComponentModel.DataAnnotations;

namespace TaskMangerLE.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Enter your username or email.")]
    [Display(Name = "Username or Email")]
    public string UsernameOrEmail { get; set; } = string.Empty;
    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    [Display(Name = "Remember me")] public bool RememberMe { get; set; }
}