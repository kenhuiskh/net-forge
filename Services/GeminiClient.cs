using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NetForge.Configuration;

namespace NetForge.Services;

public class GeminiClient
{
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";
    private const string DefaultModel = "gemini-2.0-flash";

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> CategoryDefinitions =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Food"] = new[]
            {
                "Rice/Dry", "Drinks", "Tins/Frozen", "Breakfast", "Vegetable", "Fruits",
                "Meat", "Sauce", "Dairy", "Snacks", "Seafood"
            },
            ["Household"] = new[] { "Toiletries", "Kitchen", "Others" },
            ["HST"] = Array.Empty<string>(),
            ["Beauty"] = new[] { "Others", "Supplement" },
            ["Dining"] = new[] { "Lunch", "Dinner" },
            ["Pet"] = new[] { "Kibble", "Can", "Medical", "Wet Food", "Litter" },
            ["Other"] = new[] { "Clothes", "Stationary", "House", "Learning", "Entertainment" },
            ["Commute"] = new[] { "Travel", "Car Rental", "Public" }
        };

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GeminiClient(HttpClient httpClient, IOptions<GeminiSettings> options)
    {
        _httpClient = httpClient;
        _apiKey = options.Value.ApiKey;
    }

    public async Task<string?> GenerateContentAsync(
        string prompt,
        IEnumerable<(string MimeType, string Base64Data)> images,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured.");
        }

        var requestUri = $"{BaseUrl}/{DefaultModel}:generateContent?key={_apiKey}";

        var imageParts = images?.ToList() ?? new List<(string MimeType, string Base64Data)>();

        if (imageParts.Count == 0)
        {
            throw new ArgumentException("At least one image must be supplied for Gemini processing.", nameof(images));
        }

        var parts = new List<object>
        {
            new { text = BuildReceiptExtractionPrompt() + "\n" + prompt }
        };

        parts.AddRange(imageParts.Select(image => new
        {
            inline_data = new
            {
                mime_type = image.MimeType,
                data = image.Base64Data
            }
        }));

        var payload = new
        {
            contents = new object[]
            {
                new
                {
                    role = "user",
                    parts = parts.ToArray()
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json")
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Gemini API call failed with status {(int)response.StatusCode}: {responseContent}");
        }

        using var document = JsonDocument.Parse(responseContent);
        if (!document.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
        {
            return null;
        }

        var firstCandidate = candidates[0];
        if (!firstCandidate.TryGetProperty("content", out var content) ||
            !content.TryGetProperty("parts", out var contentElement) ||
            contentElement.GetArrayLength() == 0)
        {
            return null;
        }

        var textPart = contentElement[0];
        if (textPart.TryGetProperty("text", out var textElement))
        {
            return textElement.GetString();
        }

        return null;
    }

    public string BuildReceiptExtractionPrompt()
    {
        var categories = string.Join(", ", CategoryDefinitions.Keys);
        var subcategories = string.Join(", ", CategoryDefinitions
            .SelectMany(pair => pair.Value)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase));

        var builder = new StringBuilder();
        builder.AppendLine("Please give me the following information and put in csv format.");
        builder.AppendLine("1. Purchase date (The date could be in various formats. however, the year must be 2025, and please use the format MM/DD/YYYY),");
        builder.AppendLine("2. Merchant/Vendor (Please use carmel case, e.g. NoFrills, Walmart, Costco),");
        builder.AppendLine("3. Item Name,");
        builder.AppendLine("4. Item quantity (integer if there is no item unit and default is 1),");
        builder.AppendLine("5. Item unit (if any, e.g. kg, g, lb. If there is no item unit, please leave it blank),");
        builder.AppendLine("6. Item Price,");
        builder.AppendLine("7. Item Average price,");
        builder.AppendLine($"8. Item category (available categories: {categories}),");
        builder.AppendLine($"9. Item Subcategory (available subcategories: {subcategories})");
        builder.Append("The output must be in a valid CSV format with 9 columns.");

        return builder.ToString();
    }
}
