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
    public IActionResult HelloWorld()
    {
        return Ok(new {
            Message = "Hello World from ExpenseTrackerController!"
        });
    }

    [HttpGet("byebyeworld")]
    public IActionResult ByeByeWorld()
    {
        return Ok(new {
            Message = "Bye Bye World from ExpenseTrackerController!",
            Timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("hello/{name}")]
    public IActionResult HelloName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Name cannot be empty.");
        }

        return Ok(new {
            Message = $"Hello {name}!",
            Timestamp = DateTime.UtcNow
        });
    }

    private string GetUploadDirectory()
    {
        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        Directory.CreateDirectory(uploadDir);
        return uploadDir;
    }

    private string GetChunkDirectory(string fileHash)
    {
        var uploadDir = GetUploadDirectory();
        var chunkDir = Path.Combine(uploadDir, "chunks", fileHash);
        Directory.CreateDirectory(chunkDir);
        return chunkDir;
    }

    [HttpPost("upload/receipt")]
    public async Task<IActionResult> UploadReceipt(
        [FromForm] IFormFile receipts,
        [FromForm] string? fileHash,
        [FromForm] int? chunkIndex,
        [FromForm] int? totalChunks,
        [FromForm] string? fileName)
    {
        if (receipts == null || receipts.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        try
        {
            Console.WriteLine($"Uploading file: {fileName}, Hash: {fileHash}, Chunk: {chunkIndex}/{totalChunks}");
            // Check if this is a chunked upload
            if (!string.IsNullOrWhiteSpace(fileHash) && chunkIndex.HasValue && totalChunks.HasValue)
            {
                return await HandleChunkedUpload(receipts, fileHash, chunkIndex.Value, totalChunks.Value, fileName);
            }

            // Regular single file upload
            return await HandleSingleFileUpload(receipts);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"XX Error processing upload: {ex.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Message = "Error processing upload",
                Error = ex.Message
            });
        }
    }

    [HttpPost("analyze/receipt")]
    public async Task<IActionResult> AnalyzeReceipt()
    {
        if (string.IsNullOrWhiteSpace(_geminiApiKey))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Gemini API key is not configured.");
        }

        var jpgPaths = new List<string>();
        var imagesForGemini = new List<(string MimeType, string Base64Data)>();

        // how should I read the uploaded files ?
        // or should the upload and analysis be a single endpoint ?
        // foreach (var receipt in receipts)
        // {
        //     await using var buffer = new MemoryStream();
        //     await receipt.CopyToAsync(buffer);
        //     // var imageBytes = buffer.ToArray();
        //     // var base64Image = Convert.ToBase64String(imageBytes);
        //     buffer.Position = 0;

        //     // write the receipt to uploads folder
        //     // no matter what it is
        //     // var originalPath = Path.Combine(uploadDir, Path.GetRandomFileName() + Path.GetExtension(receipt.FileName));
        //     // await using (var target = System.IO.File.Create(originalPath))
        //     // {
        //     //     await receipt.CopyToAsync(target, HttpContext.RequestAborted);
        //     // }

        //     using var image = new MagickImage(buffer);
        //     image.Format = MagickFormat.Jpeg;

        //     image.Resize(new MagickGeometry(image.Width / 2, image.Height / 2) { IgnoreAspectRatio = false });
        //     image.FilterType = FilterType.Lanczos;
        //     image.Quality = 95;
        //     var resizedBytes = image.ToByteArray(MagickFormat.Jpeg);
        //     var base64Image = Convert.ToBase64String(resizedBytes);

        //     // var jpgPath = Path.Combine(uploadDir, Path.ChangeExtension(Path.GetRandomFileName(), ".jpg"));
        //     // image.Write(jpgPath);
        //     // jpgPaths.Add(jpgPath);

        //     var mimeType = string.IsNullOrWhiteSpace(receipt.ContentType) ? "image/heic" : receipt.ContentType;
        //     imagesForGemini.Add((mimeType, base64Image));
        // }

        // string? geminiSummary;
        // string? csvPath = null;

        // try
        // {
        //     var prompt = _geminiClient.BuildReceiptExtractionPrompt();
        //     geminiSummary = await _geminiClient.GenerateContentAsync(
        //         prompt,
        //         imagesForGemini,
        //         HttpContext.RequestAborted);
        // }
        // catch (InvalidOperationException ex)
        // {
        //     geminiSummary = $"Configuration error: {ex.Message}";
        // }
        // catch (HttpRequestException ex)
        // {
        //     geminiSummary = $"Gemini request failed: {ex.Message}";
        // }

        // if (!string.IsNullOrWhiteSpace(geminiSummary))
        // {
        //     var csvContent = ExtractCsvFromResponse(geminiSummary);
        //     if (!string.IsNullOrWhiteSpace(csvContent))
        //     {
        //         var csvFileName = $"gemini-summary-{DateTime.UtcNow:yyyyMMddHHmmssfff}.csv";
        //         csvPath = Path.Combine(uploadDir, csvFileName);
        //         await System.IO.File.WriteAllTextAsync(csvPath, csvContent);
        //     }
        // }

        // return Ok(new
        // {
        //     Message = "Receipts uploaded successfully!",
        //     FilePaths = jpgPaths,
        //     GeminiSummary = geminiSummary,
        //     GeminiCsvPath = csvPath
        // });
        return Ok(new
        {
            Message = "AnalyzeReceipt endpoint is under construction."
        });
    }

    private async Task<IActionResult> HandleChunkedUpload(
        IFormFile chunk,
        string fileHash,
        int chunkIndex,
        int totalChunks,
        string? fileName)
    {
        try
        {
            var chunkDir = GetChunkDirectory(fileHash);
            var chunkFilePath = Path.Combine(chunkDir, $"chunk_{chunkIndex:D4}");

            // Save the chunk
            await using (var fileStream = System.IO.File.Create(chunkFilePath))
            {
                await chunk.CopyToAsync(fileStream);
            }

            // Create a metadata file to track upload progress
            var metadataPath = Path.Combine(chunkDir, "metadata.json");
            var metadata = new
            {
                FileName = fileName,
                FileHash = fileHash,
                TotalChunks = totalChunks,
                CurrentChunks = Directory.GetFiles(chunkDir, "chunk_*").Length,
                CreatedAt = DateTime.UtcNow
            };

            await System.IO.File.WriteAllTextAsync(
                metadataPath,
                System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })
            );

            var uploadedChunks = Directory.GetFiles(chunkDir, "chunk_*").Length;

            // Check if all chunks have been received
            if (uploadedChunks == totalChunks)
            {
                // All chunks received - assemble the file
                return await AssembleChunks(fileHash, fileName);
            }

            return Ok(new
            {
                Message = "Chunk uploaded successfully",
                FileHash = fileHash,
                ChunkIndex = chunkIndex,
                UploadedChunks = uploadedChunks,
                TotalChunks = totalChunks,
                Progress = (uploadedChunks / (double)totalChunks) * 100
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in HandleChunkedUpload: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Message = "Error processing chunk upload",
                Error = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }

    private async Task<IActionResult> AssembleChunks(string fileHash, string? fileName)
    {
        var chunkDir = GetChunkDirectory(fileHash);
        var uploadDir = GetUploadDirectory();

        try
        {
            // Get all chunk files in order
            var chunkFiles = Directory.GetFiles(chunkDir, "chunk_*")
                .OrderBy(f => f)
                .ToList();

            if (chunkFiles.Count == 0)
            {
                return BadRequest("No chunks found to assemble.");
            }

            // Create final file path
            var finalFileName = !string.IsNullOrWhiteSpace(fileName)
                ? fileName
                : $"receipt_{fileHash}_{DateTime.UtcNow:yyyyMMddHHmmssfff}";

            var finalFilePath = Path.Combine(uploadDir, finalFileName);

            // Assemble chunks into final file
            await using (var finalFile = System.IO.File.Create(finalFilePath))
            {
                foreach (var chunkFile in chunkFiles)
                {
                    var chunkData = await System.IO.File.ReadAllBytesAsync(chunkFile);
                    await finalFile.WriteAsync(chunkData, 0, chunkData.Length);
                }
            }

            // Clean up chunk directory
            foreach (var chunkFile in chunkFiles)
            {
                System.IO.File.Delete(chunkFile);
            }
            // check if the  folder is empty before deleting
            if (Directory.GetFiles(chunkDir).Length == 0)
            {
                Directory.Delete(chunkDir);
            }

            return Ok(new
            {
                Message = "File assembled successfully",
                FileName = Path.GetFileName(finalFilePath),
                FilePath = finalFilePath,
                FileSize = new FileInfo(finalFilePath).Length
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Message = "Error assembling chunks",
                Error = ex.Message
            });
        }
    }

    private async Task<IActionResult> HandleSingleFileUpload(IFormFile receipt)
    {
        if (string.IsNullOrWhiteSpace(_geminiApiKey))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Gemini API key is not configured.");
        }

        var uploadDir = GetUploadDirectory();
        var fileName = $"receipt_{Path.GetRandomFileName()}{Path.GetExtension(receipt.FileName)}";
        var filePath = Path.Combine(uploadDir, fileName);

        // Save the file
        await using (var fileStream = System.IO.File.Create(filePath))
        {
            await receipt.CopyToAsync(fileStream);
        }

        return Ok(new
        {
            Message = "Receipt uploaded successfully!",
            FileName = fileName,
            FilePath = filePath,
            FileSize = receipt.Length
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
