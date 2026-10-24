using System.ComponentModel.DataAnnotations;

namespace TaskMangerLE.Models;

public abstract class ItemBase
{
    public int Id { get; set; }
    [MaxLength(70)] public string Name { get; set; } = "Untitled";
    [MaxLength(700)] public string Description { get; set; } = "No Description";
    public DateTime LastUpdated { get; set; }
    public DateTime CreatedDate { get; set; }


    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedByUserId { get; set; }
}