# Demo 05: Delegate to a computer-use agent

A coordinator fetches Amsterdam's current temperature in Fahrenheit from
[Open-Meteo](https://open-meteo.com/), then delegates conversion to a Harness
agent exposed as an agent tool. The Harness agent uses a local
[Windows-MCP](https://github.com/CursorTouch/Windows-MCP) server to open
Calculator, inspect its controls, click buttons, and read the display.

## Requirements and safety

- Windows 10 or 11 with an interactive desktop and Windows Calculator installed.
  Codespaces and headless sessions cannot run the desktop portion.
- Python 3.14+, [uv](https://docs.astral.sh/uv/), and Windows-MCP 0.8.9.
  Install it with `uv tool install windows-mcp==0.8.9` and ensure
  `windows-mcp` is on PATH. If needed, set `WindowsMcp:Command` in user
  secrets to the full path of the installed executable.
- Close existing Calculator windows and select Standard mode before running.
- **Use an isolated desktop or VM without sensitive information.** Windows-MCP
  can launch programs, click anywhere, and inspect the entire desktop. Its
  Snapshot tool can send window titles, UI text, and potentially screenshots
  to the configured model. The instruction to use only Calculator and avoid
  screenshots is guidance, not an enforced security boundary.
- The console asks for consent before starting Windows-MCP, fetching weather,
  or performing desktop actions. The server may also request desktop-control
  consent; deny it or take control back to interrupt the agent. Its balanced
  policy gates application launches and clicks. Do not interact with the
  desktop during the calculation.
- The demo starts the server over local stdio, exposes only App, Snapshot,
  Click, WaitFor and ControlStatus, and disables Windows-MCP's PostHog
  analytics. The child process does not inherit model credentials.
- Open-Meteo reports current weather-model conditions, not a live thermometer
  reading. Internet access and a Responses-compatible model with function-tool
  support are required. No weather API key is needed.

## Configure and run

The endpoint is the Azure resource root; the sample appends `openai/v1/`.
Set credentials in user secrets rather than in the tracked settings:

```powershell
dotnet user-secrets set "OpenAI:Model" "<deployment-name>" --project demos\module-04\demo05\module-04-demo-05.csproj
dotnet user-secrets set "OpenAI:Endpoint" "https://<resource>.services.ai.azure.com/" --project demos\module-04\demo05\module-04-demo-05.csproj
dotnet user-secrets set "OpenAI:ApiKey" "<api-key>" --project demos\module-04\demo05\module-04-demo-05.csproj
dotnet run --project demos\module-04\demo05\module-04-demo-05.csproj
```

This example adapts Responses to `IChatClient` only because `AsHarnessAgent`
requires it. Windows-MCP requires Windows even though the project targets
`net10.0`.
