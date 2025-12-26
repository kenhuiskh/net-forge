using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using ImageMagick;
using NetForge.Core.Interfaces;
using NetForge.Core.Utils;

namespace NetForge.Cli.Commands;

public class ExpenseTracker : AsyncCommand<ExpenseTracker.Settings>
{
    private readonly IGeminiClient _geminiClient;
    private static readonly HashSet<string> ValidExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".heic"
    };

    public ExpenseTracker(IGeminiClient geminiClient)
    {
        _geminiClient = geminiClient;
    }

    public sealed class Settings : CommandSettings
    {
        [CommandOption("-p|--path <PATH>")]
        [Description("The path to the image file")]
        public string? Path { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        string path = Path.GetFullPath(settings.Path ?? string.Empty);

        try
        {
            if (!ValidateFilePath(path))
            {
                throw new ArgumentException($"Invalid file path: {path}");
            }

            List<(string MimeType, string Base64Data)> images;

            if (CheckPathIsAFolder(path))
            {
                images = GetImagesFromFolder(path, "image/jpeg");
            }
            else
            {
                var base64Image = ConvertHeicToBase64(path);

                images = new List<(string MimeType, string Base64Data)>
                {
                    ("image/jpeg", base64Image)
                };
            }

            if (images.Count == 0)
            {
                throw new ArgumentException($"No valid images found in the path: {path}");
            }            

            var result = await _geminiClient.GenerateContentAsync(
                PromptTemplates.BuildReceiptExtractionPrompt(),
                images);

            AnsiConsole.MarkupLine($"[bold green]Gemini Response:[/]");
            AnsiConsole.WriteLine(result ?? "No response from Gemini.");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    private string ConvertHeicToBase64(string filePath, uint quality = 80, uint maxSize = 1024)
    {
        using var image = new MagickImage(filePath);
        image.Format = MagickFormat.Jpeg;
        image.Grayscale();
        image.Normalize();
        image.Quality = quality;
        image.Resize(maxSize, maxSize);

        // write to specific path
        // var debugPath = Path.Combine($"debug_q{quality}_maxSize{maxSize}.jpg");
        // image.Write(debugPath);

        byte[] imageBytes = image.ToByteArray();
        return Convert.ToBase64String(imageBytes);
    }

    private bool ValidateFilePath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        if (!Directory.Exists(filePath) && !File.Exists(filePath))
        {
            return false;
        }

        return true;
    }

    private bool CheckPathIsAFolder(string path)
    {
        return Directory.Exists(path);
    }

    private List<(string, string)> GetImagesFromFolder(string path, string mimeType)
    {
        var images = new List<(string, string)>();
        var files = Directory.GetFiles(path);

        foreach (var file in files)
        {
            // check file extension is a valid image foramt
            if (!ValidExtensions.Contains(Path.GetExtension(file).ToLower()))
            {
                continue;
            }
            
            images.Add((mimeType, ConvertHeicToBase64(file)));
        }
        return images;
    }
}