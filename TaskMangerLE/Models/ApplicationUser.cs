using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace TaskMangerLE.Models;

public class ApplicationUser : IdentityUser
{
    [MaxLength(70)]
    public string? DisplayName { get; set; }
}