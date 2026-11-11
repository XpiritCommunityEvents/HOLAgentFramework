# Demo 01: Transportation agent

This sample creates an Agent Framework transportation agent with ride lookup and
booking function tools, then prints one response to the supplied concert question.
It is not the Harness example and does not implement `/plan` or `/todos` commands.

The model client uses `GetResponsesClient().AsIChatClient(model)` so supported
reasoning deployments can use function tools without the Chat Completions
restriction. The instruction to ask before booking is prompt guidance here, not
an enforced approval gate. Demo 03 demonstrates Harness and explicit tool approval.

## Configure and run

Keep the API key out of `appsettings.json`:

```powershell
dotnet user-secrets set "OpenAI:Model" "<deployment-name>" --project demos\module-04\demo01\module-04-demo-01.csproj
dotnet user-secrets set "OpenAI:Endpoint" "https://<resource>.services.ai.azure.com/" --project demos\module-04\demo01\module-04-demo-01.csproj
dotnet user-secrets set "OpenAI:ApiKey" "<api-key>" --project demos\module-04\demo01\module-04-demo-01.csproj
dotnet run --project demos\module-04\demo01\module-04-demo-01.csproj
```

The endpoint is the Azure resource root; code appends `openai/v1/`.
See the [API conventions](../../../README.md#model-api-conventions) for the
experimental SDK diagnostic and intentional provider exceptions.