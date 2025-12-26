using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NetForge.Core.Configuration;
using NetForge.Core.Interfaces;
using NetForge.Core.Utils;

namespace NetForge.Api.Services;

public class GeminiClient : IGeminiClient
{
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";
    private const string DefaultModel = "gemini-2.5-flash-lite";

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
            new { text = PromptTemplates.BuildReceiptExtractionPrompt() + "\n" + prompt }
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
}
