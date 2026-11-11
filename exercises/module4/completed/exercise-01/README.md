# Module 4 exercise 01: Booking loop

This reference implementation wraps the transportation agent in a `LoopAgent`.
The booking evaluator requests a structured verdict and either stops the loop
or supplies feedback. User questions are routed through `AskForUserInput`.
This is not the Harness planning/todo example.

Both the transportation agent and evaluator use
`GetResponsesClient().AsIChatClient(model)`. The tool interfaces, session,
structured verdict, and stop/continue behavior are unchanged. Booking approval
in this exercise is prompt/tool behavior; the explicit Harness approval-response
workflow is in completed exercise 02.

## Configure and run

Keep the API key out of `appsettings.json`:

```powershell
dotnet user-secrets set "OpenAI:Model" "<deployment-name>" --project exercises\module4\completed\exercise-01\module-04-agentic.csproj
dotnet user-secrets set "OpenAI:Endpoint" "https://<resource>.services.ai.azure.com/" --project exercises\module4\completed\exercise-01\module-04-agentic.csproj
dotnet user-secrets set "OpenAI:ApiKey" "<api-key>" --project exercises\module4\completed\exercise-01\module-04-agentic.csproj
dotnet run --project exercises\module4\completed\exercise-01\module-04-agentic.csproj
```

The endpoint is the Azure resource root; code appends `openai/v1/`.
See the [API conventions](../../../../README.md#model-api-conventions).