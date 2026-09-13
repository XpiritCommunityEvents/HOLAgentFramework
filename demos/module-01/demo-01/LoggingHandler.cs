using System.Text.Json;

/// <summary>
/// Intercepts HTTP requests and responses to log details to the console.
/// </summary>
public class LoggingHandler : DelegatingHandler
{
    public LoggingHandler(HttpMessageHandler innerHandler) : base(innerHandler)
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        // --- LOG REQUEST ---
        Console.WriteLine("\n============================ HTTP REQUEST ============================");
        Console.WriteLine($"{request.Method} {request.RequestUri}");
    
        Console.ForegroundColor = ConsoleColor.Green;
    
        if (request.Content != null)
        {
            // Read and log request body
            string requestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(requestBody))
            {
                Console.WriteLine("\n[Body]:");
                Console.WriteLine(requestBody);
            }
        }

        // --- EXECUTE REQUEST ---
        // Pass the request down the pipeline to execute the actual network call
        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

        // --- LOG RESPONSE ---
        Console.WriteLine("\n============================ HTTP RESPONSE ============================");
        Console.WriteLine($"HTTP/{response.Version} {(int)response.StatusCode} {response.StatusCode}");

        Console.ForegroundColor = ConsoleColor.Yellow;
        if (response.Content != null)
        {
            // Read and log response body
            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                Console.WriteLine("\n[Body]:");
                var doc = JsonDocument.Parse(responseBody);
                Console.WriteLine(JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true }) );
            }
        }
        Console.WriteLine("======================================================================\n");

        return response;
    }
}