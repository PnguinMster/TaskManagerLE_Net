using System.Text.RegularExpressions;

namespace TaskManagerLE.IntegrationTests;

// POST actions have [ValidateAntiForgeryToken], so a test POST needs what a real browser sends:
// the antiforgery cookie plus the matching hidden form field. The client keeps cookies between
// requests, so fetching a page first sets the cookie and gives us the field's value.
public static partial class AntiforgeryHelper
{
    private const string FieldName = "__RequestVerificationToken";

    // Every page has a token, because _Layout.cshtml renders one in the toast form.
    public static async Task<HttpResponseMessage> PostFormAsync(
        this HttpClient client, string url, Dictionary<string, string> fields)
    {
        string html = await client.GetStringAsync("/Project");
        Match match = TokenInputRegex().Match(html);
        if (!match.Success)
            throw new InvalidOperationException($"No {FieldName} found on /Project.");

        fields[FieldName] = match.Groups[1].Value;
        return await client.PostAsync(url, new FormUrlEncodedContent(fields));
    }

    // Matches the input the Razor tag helper renders: <input name="__RequestVerificationToken" type="hidden" value="...">
    [GeneratedRegex(@"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""")]
    private static partial Regex TokenInputRegex();
}
