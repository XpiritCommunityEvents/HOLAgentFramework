using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

internal sealed class WeatherTools(HttpClient httpClient)
{
    [Description("Fetches Amsterdam's current weather-model temperature in Fahrenheit and its local time from Open-Meteo.")]
    public async Task<string> GetAmsterdamTemperature(CancellationToken cancellationToken)
    {
        Console.WriteLine("\n[Weather] Fetching Amsterdam's current temperature from Open-Meteo.");
        const string url = "https://api.open-meteo.com/v1/forecast?latitude=52.3676&longitude=4.9041&current=temperature_2m&temperature_unit=fahrenheit&timezone=Europe%2FAmsterdam";
        using JsonDocument document = await httpClient.GetFromJsonAsync<JsonDocument>(url, cancellationToken)
            ?? throw new InvalidOperationException("Open-Meteo returned an empty response.");

        JsonElement current = document.RootElement.GetProperty("current");
        double temperature = current.GetProperty("temperature_2m").GetDouble();
        string time = current.GetProperty("time").GetString()
            ?? throw new InvalidOperationException("Open-Meteo did not return a weather time.");
        string unit = document.RootElement.GetProperty("current_units").GetProperty("temperature_2m").GetString()
            ?? throw new InvalidOperationException("Open-Meteo did not return a temperature unit.");
        if (unit != "°F" || !double.IsFinite(temperature))
        {
            throw new InvalidOperationException("Open-Meteo returned an invalid Fahrenheit temperature.");
        }

        return FormattableString.Invariant($"Amsterdam at {time} Europe/Amsterdam: {temperature} °F (Open-Meteo current weather model).");
    }
}
