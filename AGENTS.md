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

The completed Module 5 frontend has JavaScript bundles built with esbuild; from `exercises/module5/completed/`, run `npm run build:chat` after editing its chat client sources. No repository-wide lint command is configured.

## Repository-specific conventions

- Keep each lesson sample self-contained and use its existing project/solution. Do not treat `start/` and `completed/` as interchangeable: `start/` is the learner scaffold and `completed/` is the reference implementation.
- Follow the nearby sample's style. Standalone demos commonly use top-level `Program.cs`; Module 5 uses conventional ASP.NET Core service projects wired by its Aspire AppHost.
- Keep credentials out of tracked settings. The demos commonly load configuration from `appsettings.json`, .NET user secrets, and environment variables; preserve required-setting validation and do not add real keys to sample configuration.
- In Module 5, configure service endpoints and dependencies in `AppHost/AppHost.cs` and use the shared `ServiceDefaults` integration in services. Avoid hard-coding inter-service URLs that the AppHost supplies through configuration.
- Use the existing MSTest contract tests in `exercises/module5/completed/unittests` for changes to catalog tools or frontend-agent behavior. Keep MCP tool safety annotations and validation consistent with the neighboring tools and tests.
