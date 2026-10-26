using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace TaskMangerLE.Models;

public static class ItemConstraints
{
    public const int NameMaxLength = 70;
    public const int DescriptionMaxLength = 700;
    public const int UserIdMaxLength = 450;
}

// Not EF entity type
public abstract class ItemBase
{
    public int Id { get; set; }
    [Required, MaxLength(ItemConstraints.NameMaxLength)]
    public string? Name { get; set; }
    [Required, MaxLength(ItemConstraints.DescriptionMaxLength)]
    public string? Description { get; set; }
    public DateTime UpdatedAttUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    [MaxLength(ItemConstraints.UserIdMaxLength)]
    public string? DeletedByUserId { get; set; }
}