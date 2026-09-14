using System.ComponentModel;
using System.Diagnostics;
using Module04.Demo05.Tools;

namespace Module04.Demo05.Tools;

public static class ItineraryDocumentFunctions
{
    [Description("Creates a PDF itinerary from a JSON itinerary object and returns its local path.")]
    public static async Task<string> GenerateItineraryPdf(
        [Description("A JSON object containing ticket, hotel, and ride details.")] string itineraryJson,
        [Description("The output PDF file name.")] string fileName,
        CancellationToken cancellationToken = default)
    {
        string safeName = Path.GetFileName(fileName);
        string inputPath = Path.Combine(PdfSkillSupport.WorkingDirectory, $"{Guid.NewGuid():N}.json");
        string outputPath = Path.Combine(PdfSkillSupport.WorkingDirectory, safeName);
        string scriptPath = Path.Combine(AppContext.BaseDirectory, "skills", "itinerary", "scripts", "generate_itinerary.py");
        await File.WriteAllTextAsync(inputPath, itineraryJson, cancellationToken);

        if (!File.Exists(scriptPath))
        {
            return $"Error: itinerary script was not found at {scriptPath}.";
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "python",
            WorkingDirectory = PdfSkillSupport.WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.ArgumentList.Add(inputPath);
        startInfo.ArgumentList.Add(outputPath);

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return "Error: could not start Python for itinerary generation.";
        }

        string output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        string error = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode == 0 ? output.Trim() : $"Error generating itinerary PDF: {error}";
    }
}
