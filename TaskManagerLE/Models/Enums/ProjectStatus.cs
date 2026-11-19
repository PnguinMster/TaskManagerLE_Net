using System.ComponentModel.DataAnnotations;

namespace TaskManagerLE.Models.Enums;

// Listed in workflow order because dropdowns (GetEnumSelectList) follow declaration order.
// The numbers are what the database stores: never renumber them, only move lines.
// [Display] only changes the label people see; the stored value stays the same.
public enum ProjectStatus
{
    Planning = 0,
    [Display(Name = "In Progress")]
    Active = 2,
    [Display(Name = "On Hold")]
    OnHold = 3,
    Completed = 4,
    Cancelled = 5,
    Archived = 1
}
