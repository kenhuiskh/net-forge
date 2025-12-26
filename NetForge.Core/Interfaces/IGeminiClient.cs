using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NetForge.Core.Interfaces;

public interface IGeminiClient
{
    /// <summary>
    /// Sends a prompt and associated images to Gemini and generated text content
    /// </summary>

    Task<string?> GenerateContentAsync(
        string prompt,
        IEnumerable<(string MimeType, string Base64Data)> images,
        CancellationToken cancellationToken = default);
}