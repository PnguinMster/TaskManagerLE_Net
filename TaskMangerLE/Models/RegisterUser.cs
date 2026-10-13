using System.ComponentModel.DataAnnotations;

namespace TaskMangerLE.Models;

public class RegisterUser
{
    [Required, MaxLength(30), MinLength(3)]
    public string UserName { get; set; } = string.Empty;

    [Required, EmailAddress] public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}