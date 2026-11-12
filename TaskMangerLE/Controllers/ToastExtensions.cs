using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using TaskMangerLE.Models;

namespace TaskMangerLE.Controllers;

public static class ToastExtensions
{
    // _Layout reads this key and hands the JSON to site.js.
    public const string TempDataKey = "Toast";

    // camelCase names and enum-as-string, so site.js gets { "kind": "success", "title": "..." }.
    // The default encoder also escapes < and >, so the JSON is safe to place inside a <script> tag.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    // TempData only stores simple values (strings, numbers), so the toast goes in as JSON.
    public static void ShowToast(this Controller controller, ToastMessage toast) =>
        controller.TempData[TempDataKey] = JsonSerializer.Serialize(toast, JsonOptions);
}
