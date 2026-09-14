using System.ComponentModel;
using System.Diagnostics;
using Module04.Demo05.Tools;

namespace Module04.Demo05.Tools;

public static class TicketReadingFunctions
{
    [Description("Downloads a public PDF ticket URL into the agent work directory.")]
    public static Task<string> DownloadFile(
        [Description("The public http or https URL of the PDF ticket.")] string url,
        [Description("The local file name, including the .pdf extension.")] string fileName) =>
        PdfSkillSupport.DownloadFile(url, fileName);

    [Description("Extracts text from a local PDF ticket. Never infer missing ticket details.")]
    public static async Task<string> ExtractPdfText(
        [Description("The local PDF path returned by download_file.")] string filePath,
        CancellationToken cancellationToken = default)
    {
        string scriptPath = Path.Combine(AppContext.BaseDirectory, "skills", "pdf", "scripts", "extract_text.py");
        if (!File.Exists(scriptPath))
        {
            return $"Error: PDF extraction script was not found at {scriptPath}.";
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
        startInfo.ArgumentList.Add(filePath);

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return "Error: could not start Python for PDF extraction.";
        }

        string output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        string error = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode == 0 ? output : $"Error extracting PDF text: {error}";
    }
}
