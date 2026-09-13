using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Agents.AI;

namespace ModuleAgent;

/// <summary>Download + Python execution plumbing that the file-based "pdf" agent skill needs to actually run.</summary>
internal static class PdfSkillSupport
{
    private const int MaxDownloadBytes = 25 * 1024 * 1024;
    private const int MaxCapturedChars = 20_000;
    private static readonly TimeSpan ScriptTimeout = TimeSpan.FromMinutes(2);

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };

    /// <summary>Sandbox root: downloads land here and it is the working directory for every skill script.</summary>
    public static string WorkingDirectory { get; } =
        Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "work")).FullName;

    public static string SkillsDirectory { get; } = Path.Combine(AppContext.BaseDirectory, "skills");

    /// <summary>Resolved once so a PATH with several Pythons cannot silently switch interpreters between runs.</summary>
    private static readonly string PythonExecutable = ResolvePython();

    [Description("Downloads a file from a public http(s) URL into the agent working directory and returns the local file path.")]
    public static async Task<string> DownloadFile(
        [Description("The absolute http or https URL to download.")] string url,
        [Description("File name to save as, including extension, for example tickets.pdf")] string fileName,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return $"Error: '{url}' is not an absolute http(s) URL.";
        }

        uri = NormalizeGitHubBlobUrl(uri);

        // Strip any directory component so the model cannot escape the sandbox via the file name.
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName))
        {
            return "Error: fileName must be a non-empty file name.";
        }

        var destination = Path.Combine(WorkingDirectory, safeName);

        try
        {
            using var response = await Http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return $"Error: download failed with HTTP {(int)response.StatusCode} {response.ReasonPhrase}.";
            }

            if (response.Content.Headers.ContentLength > MaxDownloadBytes)
            {
                return $"Error: file is larger than the {MaxDownloadBytes / (1024 * 1024)} MB limit.";
            }

            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var target = File.Create(destination);
            await CopyWithLimitAsync(source, target, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException)
        {
            return $"Error: download failed - {ex.Message}";
        }

        var size = new FileInfo(destination).Length;
        return $"Downloaded {size} bytes to {destination}";
    }

    /// <summary>
    /// Runs a script that the skills source discovered on disk. Wire this into
    /// <see cref="AgentFileSkillsSource"/> so the <c>run_skill_script</c> tool has an execution strategy.
    /// </summary>
    public static async Task<object> RunPythonScriptAsync(
        AgentFileSkill skill,
        AgentFileSkillScript script,
        JsonElement? arguments,
        IServiceProvider? serviceProvider,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(Path.GetExtension(script.FullPath), ".py", StringComparison.OrdinalIgnoreCase))
        {
            return $"Error: only .py skill scripts are supported, got '{script.FullPath}'.";
        }

        if (PythonExecutable.Length == 0)
        {
            return "Error: no Python interpreter found on PATH. Install Python 3 and the PDF dependencies with 'python -m pip install pypdf pdfplumber'.";
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = PythonExecutable,
            WorkingDirectory = WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        startInfo.ArgumentList.Add(script.FullPath);
        foreach (var argument in ParseArguments(arguments))
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        process.OutputDataReceived += (_, e) => Append(stdout, e.Data);
        process.ErrorDataReceived += (_, e) => Append(stderr, e.Data);

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(ScriptTimeout);
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            return $"Error: script timed out after {ScriptTimeout.TotalSeconds:N0}s.";
        }
        catch (Exception ex)
        {
            return $"Error: failed to run script - {ex.Message}";
        }

        var result = $"""
            exit code: {process.ExitCode}
            stdout:
            {Truncate(stdout.ToString())}
            stderr:
            {Truncate(stderr.ToString())}
            """;

        if (process.ExitCode != 0 && stderr.ToString().Contains("ModuleNotFoundError", StringComparison.Ordinal))
        {
            result += $"\nPython executable: {PythonExecutable}\nInstall the missing package with '{PythonExecutable} -m pip install pypdf pdfplumber'.";
        }

        return result;
    }

    private static IEnumerable<string> ParseArguments(JsonElement? arguments)
    {
        // AgentFileSkillScript advertises {"type":"array","items":{"type":"string"}}, but tolerate a bare string.
        if (arguments is not { } value)
        {
            yield break;
        }

        switch (value.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in value.EnumerateArray())
                {
                    var text = item.ValueKind == JsonValueKind.String ? item.GetString() : item.ToString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        yield return text;
                    }
                }

                break;

            case JsonValueKind.String:
                var single = value.GetString();
                if (!string.IsNullOrEmpty(single))
                {
                    yield return single;
                }

                break;
        }
    }

    private static Uri NormalizeGitHubBlobUrl(Uri uri)
    {
        // github.com/<o>/<r>/blob/<ref>/<path> serves HTML; raw.githubusercontent.com serves the bytes.
        if (!uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
        {
            return uri;
        }

        var segments = uri.AbsolutePath.Trim('/').Split('/');
        if (segments.Length < 5 || segments[2] != "blob")
        {
            return uri;
        }

        var rest = string.Join('/', segments.Skip(3));
        return new Uri($"https://raw.githubusercontent.com/{segments[0]}/{segments[1]}/{rest}");
    }

    private static async Task CopyWithLimitAsync(Stream source, Stream target, CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            total += read;
            if (total > MaxDownloadBytes)
            {
                throw new IOException($"Response exceeded the {MaxDownloadBytes / (1024 * 1024)} MB limit.");
            }

            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
    }

    private static string ResolvePython()
    {
        foreach (var candidate in new[] { "python", "py", "python3" })
        {
            try
            {
                using var probe = Process.Start(new ProcessStartInfo(candidate, "--version")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                });

                if (probe is null)
                {
                    continue;
                }

                probe.WaitForExit(5000);
                if (probe.ExitCode == 0)
                {
                    return candidate;
                }
            }
            catch
            {
                // Not on PATH; try the next candidate.
            }
        }

        return string.Empty;
    }

    private static void Append(StringBuilder builder, string? line)
    {
        if (line is not null && builder.Length < MaxCapturedChars)
        {
            builder.AppendLine(line);
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Process already gone.
        }
    }

    private static string Truncate(string text) =>
        text.Length <= MaxCapturedChars ? text : text[..MaxCapturedChars] + "\n...[truncated]";
}
