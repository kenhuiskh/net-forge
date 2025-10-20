using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ImageMagick;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NetForge.Configuration;
using NetForge.Services;

namespace NetForge.Controllers;

[ApiController]
[Route("api/expenses")]
public class ExpenseTrackerController : ControllerBase
{
    private readonly string _geminiApiKey;
    private readonly GeminiClient _geminiClient;

    public ExpenseTrackerController(IOptions<GeminiSettings> geminiOptions, GeminiClient geminiClient)
    {
        _geminiApiKey = geminiOptions.Value.ApiKey;
        _geminiClient = geminiClient;
    }

    [HttpGet("helloworld")]
    public string HelloWorld()
    {
        return "Hello World Endpoint Testing";
    }

    [HttpPost("upload/receipt")]
    public async Task<IActionResult> UploadReceipt([FromForm] List<IFormFile> receipts)
    {
        if (string.IsNullOrWhiteSpace(_geminiApiKey))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Gemini API key is not configured.");
        }

        if (receipts == null || receipts.Count == 0)
        {
            return BadRequest("No files uploaded.");
        }

        // check if the uploads folder exists, if not, create it
        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        Directory.CreateDirectory(uploadDir);
        var jpgPaths = new List<string>();
        var imagesForGemini = new List<(string MimeType, string Base64Data)>();

        foreach (var receipt in receipts)
        {
            await using var buffer = new MemoryStream();
            await receipt.CopyToAsync(buffer);
            // var imageBytes = buffer.ToArray();
            // var base64Image = Convert.ToBase64String(imageBytes);
            buffer.Position = 0;

            // write the receipt to uploads folder
            // no matter what it is
            // var originalPath = Path.Combine(uploadDir, Path.GetRandomFileName() + Path.GetExtension(receipt.FileName));
            // await using (var target = System.IO.File.Create(originalPath))
            // {
            //     await receipt.CopyToAsync(target, HttpContext.RequestAborted);
            // }

            using var image = new MagickImage(buffer);
            image.Format = MagickFormat.Jpeg;

            image.Resize(new MagickGeometry(image.Width / 2, image.Height / 2) { IgnoreAspectRatio = false });
            image.FilterType = FilterType.Lanczos;
            image.Quality = 95;
            var resizedBytes = image.ToByteArray(MagickFormat.Jpeg);
            var base64Image = Convert.ToBase64String(resizedBytes);

            // var jpgPath = Path.Combine(uploadDir, Path.ChangeExtension(Path.GetRandomFileName(), ".jpg"));
            // image.Write(jpgPath);
            // jpgPaths.Add(jpgPath);

            var mimeType = string.IsNullOrWhiteSpace(receipt.ContentType) ? "image/heic" : receipt.ContentType;
            imagesForGemini.Add((mimeType, base64Image));
        }

        string? geminiSummary;
        string? csvPath = null;

        try
        {
            var prompt = _geminiClient.BuildReceiptExtractionPrompt();
            geminiSummary = await _geminiClient.GenerateContentAsync(
                prompt,
                imagesForGemini,
                HttpContext.RequestAborted);
        }
        catch (InvalidOperationException ex)
        {
            geminiSummary = $"Configuration error: {ex.Message}";
        }
        catch (HttpRequestException ex)
        {
            geminiSummary = $"Gemini request failed: {ex.Message}";
        }

        if (!string.IsNullOrWhiteSpace(geminiSummary))
        {
            var csvContent = ExtractCsvFromResponse(geminiSummary);
            if (!string.IsNullOrWhiteSpace(csvContent))
            {
                var csvFileName = $"gemini-summary-{DateTime.UtcNow:yyyyMMddHHmmssfff}.csv";
                csvPath = Path.Combine(uploadDir, csvFileName);
                await System.IO.File.WriteAllTextAsync(csvPath, csvContent);
            }
        }

        return Ok(new
        {
            Message = "Receipts uploaded successfully!",
            FilePaths = jpgPaths,
            GeminiSummary = geminiSummary,
            GeminiCsvPath = csvPath
        });
    }

    private static string? ExtractCsvFromResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return null;
        }

        var trimmed = response.Trim();

        const string fence = "```";
        if (trimmed.StartsWith(fence, StringComparison.Ordinal))
        {
            var firstNewLine = trimmed.IndexOf('\n');
            if (firstNewLine >= 0)
            {
                trimmed = trimmed[(firstNewLine + 1)..];
            }
        }

        if (trimmed.EndsWith(fence, StringComparison.Ordinal))
        {
            trimmed = trimmed[..^fence.Length];
        }

        trimmed = trimmed.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();

        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
