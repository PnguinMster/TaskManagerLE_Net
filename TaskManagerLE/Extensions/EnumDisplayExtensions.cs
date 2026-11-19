using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace TaskManagerLE.Extensions;

public static class EnumDisplayExtensions
{
    // The label from [Display(Name = "...")] on an enum value (e.g. ProjectStatus.Active -> "In Progress"),
    // or the value's own name when it has none. Html.GetEnumSelectList reads the same attribute for the
    // dropdowns, so each label is written once, on the enum.
    public static string GetDisplayName(this Enum value) =>
        value.GetType().GetField(value.ToString())?.GetCustomAttribute<DisplayAttribute>()?.GetName()
        ?? value.ToString();
}
