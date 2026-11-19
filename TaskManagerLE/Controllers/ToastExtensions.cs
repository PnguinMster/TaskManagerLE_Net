using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using TaskManagerLE.Models;

namespace TaskManagerLE.Controllers;

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

    // Same limit as MAX_TOASTS in site.js.
    private const int MaxToasts = 3;

    // TempData only stores simple values (strings, numbers), so the toasts go in as one JSON list.
    // Calling this more than once before redirecting queues several toasts; past 3, the oldest is dropped.
    public static void ShowToast(this Controller controller, ToastMessage toast)
    {
        var toasts = controller.TempData[TempDataKey] is string json
            ? JsonSerializer.Deserialize<List<ToastMessage>>(json, JsonOptions) ?? []
            : [];

        toasts.Add(toast);
        if (toasts.Count > MaxToasts)
            toasts.RemoveAt(0);

        controller.TempData[TempDataKey] = JsonSerializer.Serialize(toasts, JsonOptions);
    }
}
