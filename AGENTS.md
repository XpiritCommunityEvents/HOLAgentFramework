# Agent instructions

## Repository layout and architecture

- This repository is a hands-on Microsoft Agent Framework workshop. `demos/` contains runnable examples organized by module; `exercises/` contains the corresponding exercises, usually with `start/` and `completed/` versions. Treat each sample as an independently buildable project and make changes in the intended version rather than copying changes between variants.
- Module README files point to the workshop Wiki for exercise instructions. Check the relevant module and sample before changing behavior; there is no single application spanning all modules.
- Most projects are standalone .NET 10 console applications. Module 5's completed GloboTicket sample is the distributed-app exception: its `AppHost` composes `catalog`, `ordering`, and `frontend`, with `ServiceDefaults` providing shared Aspire service configuration. The catalog persists event data with EF Core/SQL Server and exposes tools over MCP; the frontend hosts the agent and web/chat experience and consumes catalog and ordering APIs. The AppHost also configures the OpenAI model and Agent Framework DevUI.
- Module 5's `unittests` project tests catalog-tool and frontend-agent contracts. Package versions for .NET projects are centrally managed in the root `Directory.Packages.props`; keep project package references versionless and update central versions when needed.

## Build, test, and lint

Run commands from the repository root. Build or run an individual sample by targeting its project:

```powershell
dotnet build demos/module-01/demo-03/module-01-demo-03.csproj
dotnet run --project demos/module-01/demo-03/module-01-demo-03.csproj
```

For the Module 5 distributed sample and its tests:

```powershell
dotnet build exercises/module5/completed/GloboTicket.slnx
dotnet test exercises/module5/completed/GloboTicket.slnx
dotnet test exercises/module5/completed/unittests/unittests.csproj --filter "FullyQualifiedName~CatalogToolContractTests.Artist_lookup_trims_input_bounds_results_and_propagates_cancellation"
dotnet run --project exercises/module5/completed/AppHost/AppHost.csproj
```

The completed Module 5 frontend has JavaScript bundles built with esbuild; from `exercises/module5/completed/`, run `npm run build:chat` after editing its chat client sources or dependencies. Keep the tracked bundles and lockfile consistent with the manifest. No repository-wide lint command is configured.

## Repository-specific conventions

- Keep each lesson sample self-contained and use its existing project/solution. Do not treat `start/` and `completed/` as interchangeable: `start/` is the learner scaffold and `completed/` is the reference implementation.
- Follow the nearby sample's style. Standalone demos commonly use top-level `Program.cs`; Module 5 uses conventional ASP.NET Core service projects wired by its Aspire AppHost.
- Keep credentials out of tracked settings. The demos commonly load configuration from `appsettings.json`, .NET user secrets, and environment variables; preserve required-setting validation and do not add real keys to sample configuration.
- In Module 5, configure service endpoints and dependencies in `AppHost/AppHost.cs` and use the shared `ServiceDefaults` integration in services. Avoid hard-coding inter-service URLs that the AppHost supplies through configuration.
- Use the existing MSTest contract tests in `exercises/module5/completed/unittests` for changes to catalog tools or frontend-agent behavior. Keep MCP tool safety annotations and validation consistent with the neighboring tools and tests.

## Dependency maintenance

- Keep all projects targeting `net10.0`. The root `global.json` selects SDK 10.0.404, rolls forward only within .NET 10, and disables prerelease SDKs. Do not select or install .NET 11 for this workshop.
- Most participants use Codespaces. Keep the .NET feature's SDK version in `.devcontainer/devcontainer.json` aligned with `global.json`; `global.json` selects an SDK but does not install it. Use the stable .NET 10 image family, regenerate `devcontainer-lock.json` with the Dev Container CLI after feature changes, and verify SDK resolution during container creation. Existing Codespaces need a rebuild to pick up these changes.
- Prefer compatible stable packages. Existing preview-only Agent Framework hosting/DevUI integrations, Azure AI Inference, and EF Core instrumentation stay pinned until a suitable stable replacement is available. Keep EF Core and Microsoft.Extensions on their .NET 10 release lines; package major numbers alone do not establish runtime requirements.
- Central transitive pinning keeps OpenAI on 2.14.0 and `System.Security.Cryptography.Pkcs` on its patched .NET 10 version. Preserve these constraints when updating upstream dependencies.
- OpenAI 2.15.0 supports .NET 10, but changes `ResponsesClient.CreateResponseStreamingAsync(CreateResponseOptions, CancellationToken)` from `AsyncCollectionResult<StreamingResponseUpdate>` to `Task<AsyncStreamingResult<StreamingResponseUpdate>>`. `Microsoft.Extensions.AI.OpenAI` 10.11.0 calls the former signature and throws `MissingMethodException` during streaming with 2.15.0. Do not upgrade OpenAI alone; run the Responses contract tests, including streaming/session history, when changing this integration.
- Run NuGet and npm vulnerability audits after dependency changes. The frontend still has upstream Zod 3 and math-rendering advisories; do not force the suggested CopilotKit downgrade or incompatible transitive major versions just to clear the audit.

## Model API conventions

Use the Responses API for Agent Framework examples, including multimodal agents, function tools, streaming, RAG, booking evaluators, and the Module 5 frontend. Prefer direct provider agent creation in both runnable examples and starter scaffolds:

```csharp
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;

#pragma warning disable OPENAI001 // The compatible SDK marks ResponsesClient and GetResponsesClient experimental.
var responsesClient = new OpenAIClient(
    new ApiKeyCredential(apiKey),
    new OpenAIClientOptions { Endpoint = new Uri(endpoint) })
    .GetResponsesClient();
AIAgent agent = responsesClient.AsAIAgent(
    model: model,
    instructions: "You are a helpful assistant.");
#pragma warning restore OPENAI001
```

- Reference `Microsoft.Agents.AI.OpenAI` with its centrally managed version. The direct extension is in `OpenAI.Responses`.
- The SDK marks both `OpenAIClient.GetResponsesClient()` and explicit `ResponsesClient` type references experimental. Direct `AsAIAgent` does not eliminate `OPENAI001`, even with OpenAI 2.15.0. Keep necessary suppressions in affected source files, not project-wide.
- Module 4 demo 03 and completed exercise 02 retain `responsesClient.AsIChatClient(model).AsHarnessAgent(...)` because `AsHarnessAgent` requires `IChatClient` and has no `ResponsesClient` overload. Their evaluators still use direct `AsAIAgent`. These abstractions do not imply Chat Completions on the wire.
- Preserve `AgentSession`, streaming, tools and approvals, structured output, DI, logging, telemetry, and orchestration. Responses is a model transport, not a replacement for these workflows. Keep Harness hosted web search disabled where the lesson does not require it.
- Keep supported model reasoning enabled. The workshop's `gpt-6-luna` deployment supports function tools plus reasoning through Responses, not Chat Completions. Do not silently disable reasoning or retry another protocol after failure. Sampling options are model-specific; token limits include reasoning tokens.
- Never export sensitive prompts or tool results outside explicitly enabled local development diagnostics.

### Endpoint and secret configuration

| Samples | `OpenAI:Endpoint` |
| --- | --- |
| Module 1 multimodal agents and Modules 3-4 | Azure resource root, e.g. `https://<resource>.services.ai.azure.com/`; code appends `openai/v1/` |
| Module 2 Agent Framework examples and Module 5 frontend | Full API base, e.g. `https://<resource>.services.ai.azure.com/openai/v1/`; code uses it directly |

Do not pass `/chat/completions` or `/responses` operation URLs as SDK bases. For another provider, verify Responses/model support and adapt the sample explicitly; Azure path-appending code is not a universal endpoint normalizer. Direct OpenAI uses `https://api.openai.com/v1/`.

Standalone samples intentionally share a user-secrets ID. Module 5's AppHost has its own ID and uses `Parameters:openai-api-key`; configuring a standalone sample does not configure the AppHost.

### Intentional API exceptions

| Examples | API retained and reason |
| --- | --- |
| Module 1 demo 03 and exercise 01, including scaffolds | Azure AI Inference `/models`; provider-specific SDK lesson |
| Module 1 demo 04 and exercise 02, including scaffolds; Module 2 demo 01 | Raw HTTP Chat Completions; request JSON and function-tool schema lessons |
| Module 3 demo 01's optional `GetVenuePolicyFileContentsChatClient` helper | Raw Chat Completions structured-output comparison |
| Module 3 embedding generators | Embeddings API, not conversational responses |
| Module 1 demo 06 image editing | MAI image-editing REST API, not OpenAI Responses |

Do not migrate these exceptions to Responses. For reasoning with function tools in the raw Chat Completions lesson, choose a compatible deployment or use `reasoning_effort: "none"` only for that compatibility demonstration, never for the agents.

## Agent skills

### Issue tracker

Issues and specs are tracked in this repository's GitHub Issues using the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Triage labels

Use the default labels `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, and `wontfix`. See `docs/agents/triage-labels.md`.

### Domain docs

Use the single-context layout with a root `GLOSSARY.md` and `docs/adr/`. See `docs/agents/domain.md`.
