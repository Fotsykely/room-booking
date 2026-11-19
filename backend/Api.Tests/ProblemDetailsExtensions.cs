using System.Net.Http.Json;
using System.Text.Json;

namespace Api.Tests;

public static class ProblemDetailsExtensions
{
    // Reads the per-field messages of a 400 validation response: { "Name": ["message"] }.
    public static async Task<Dictionary<string, string[]>> ReadFieldErrorsAsync(this HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        return problem.GetProperty("errors").EnumerateObject().ToDictionary(
            field => field.Name,
            field => field.Value.EnumerateArray().Select(message => message.GetString()!).ToArray());
    }
}
