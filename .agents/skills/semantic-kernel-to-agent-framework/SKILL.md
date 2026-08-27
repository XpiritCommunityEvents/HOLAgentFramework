---
name: semantic-kernel-to-agent-framework
description: Migrate Semantic Kernel Agents usage to Microsoft Agent Framework in .NET projects. This skill is designed to work across Claude Code, Copilot CLI, and OpenCode without tool-specific syntax.
---

# Semantic Kernel to Agent Framework migration

Use this skill when a .NET project references `Microsoft.SemanticKernel.Agents` or related Semantic Kernel agent packages and needs to migrate to `Microsoft.Agents.AI` and the Agent Framework API surface.

This file is intentionally tool-neutral so it can be reused across Claude Code, Copilot CLI, and OpenCode interchangeably.

## Activation

Use this skill when you see any of the following:

- `Microsoft.SemanticKernel.Agents` package references
- `ChatCompletionAgent`, `OpenAIAssistantAgent`, `AzureAIAgent`, `OpenAIResponseAgent`, `A2AAgent`, or `BedrockAgent`
- `InvokeAsync` / `InvokeStreamingAsync` patterns tied to Semantic Kernel agents
- `KernelFunction`, `KernelPlugin`, or `KernelArguments` used for agent tool registration
- `InnerContent` access on agent messages

## Core rules

- Work at the smallest possible scope: single project first, then solution if explicitly requested.
- Preserve business logic and comments. Do not add placeholder logic.
- Prefer actual code and API usage over text heuristics.
- Keep migration reversible if a provider-specific API does not map cleanly.
- Never declare the migration complete until the modified code builds successfully.
- If a scenario is unsupported, record the remaining work and document it clearly in the final report.

## Cross-tool compatibility rules

- Treat this as a plain-language operating guide, not a tool-specific script.
- Avoid relying on environment-specific commands or proprietary command wrappers.
- Use normal discovery, file editing, and build steps that work in Claude Code, Copilot CLI, and OpenCode.
- Keep output concise, direct, and implementation-focused.

## Required workflow

1. Identify affected projects
   - Search for Semantic Kernel agent package references and direct API usage.
   - If a single project is named, migrate only that project.
   - If a solution is named, migrate all projects in scope that directly use the agent APIs.
   - Use central package management updates when applicable.

2. Update package references
   - Remove Semantic Kernel agent packages that are no longer needed.
   - If the project uses non-agent Semantic Kernel APIs (for example, `Kernel`, `KernelMemory`, or non-agent plugins), retain the corresponding SK packages and namespaces. Only remove packages and namespaces exclusively related to the agent surface.
   - Add the appropriate Agent Framework packages for the provider in use.
   - If a provider is unsupported, record the limitation and do not claim a supported migration.

3. Migrate the code
   - Replace `using Microsoft.SemanticKernel.Agents` and related Semantic Kernel agent namespaces with the proper Agent Framework namespaces.
   - Replace agent creation patterns with provider-specific Agent Framework equivalents.
   - Replace `InvokeAsync` / `InvokeStreamingAsync` with `RunAsync` / `RunStreamingAsync`.
   - Replace thread creation patterns with `agent.GetNewThread()`.
   - Replace `KernelFunction`-based tool registration with `AIFunctionFactory.Create(...)`.
   - Replace `InnerContent` access with `RawRepresentation` when raw provider data is required.

4. Re-scan for leftovers
   - Search again for remaining Semantic Kernel agent references.
   - Keep iterating until the affected code paths no longer depend on Semantic Kernel agent APIs.

5. Build and fix errors
   - Run the relevant build step for each modified project.
   - Fix compilation issues one by one.
   - Do not stop early when a migration is only partially converted.

6. Validate the migration
   - Confirm the checklist below passes.
   - Confirm no direct Semantic Kernel agent usage remains in the affected code paths.

7. Write the report
   - Create a markdown report at `<solution-root>/.github/SemanticKernelToAgentFrameworkReport.md`.
   - Include package changes, modified files, unresolved gaps, provider-specific notes, and runtime verification steps.

## Type mapping

| Semantic Kernel type | Agent Framework equivalent |
| --- | --- |
| `ChatCompletionAgent` | `ChatClientAgent` |
| `OpenAIAssistantAgent` | `assistantsClient.CreateAIAgent()` or `GetAIAgent(agentId)` |
| `AzureAIAgent` | `persistentAgentsClient.CreateAIAgent()` or `GetAIAgent(agentId)` |
| `OpenAIResponseAgent` | `responsesClient.CreateAIAgent()` |
| `A2AAgent` | `AIAgent` via the A2A card resolver |
| `BedrockAgent` | Unsupported; requires custom implementation |
| `IChatCompletionService` | `IChatClient` |

## Required API conversions

- `InvokeAsync(...)` -> `RunAsync(...)`
- `InvokeStreamingAsync(...)` -> `RunStreamingAsync(...)`
- provider-specific thread creation -> `agent.GetNewThread()`
- `AgentInvokeOptions` / `KernelArguments` -> `AgentRunOptions` or `ChatClientAgentRunOptions`
- `[KernelFunction]` + `KernelPlugin` -> `AIFunctionFactory.Create(...)`
- `InnerContent` -> `RawRepresentation`
- Use `AgentResponse` for non-streaming invocations (`RunAsync`). Retain `IAsyncEnumerable<AgentResponseItem<ChatMessageContent>>` only for streaming invocations (`RunStreamingAsync`).

## Example migration patterns

### Namespace cleanup

Remove:

```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
```

Add only what is needed for the actual provider and runtime code:

```csharp
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using Azure.AI.OpenAI;
using Azure.AI.Agents.Persistent;
```

### Agent creation

Semantic Kernel:

```csharp
Kernel kernel = Kernel.CreateBuilder()
    .AddOpenAIChatClient(modelId, apiKey)
    .Build();

ChatCompletionAgent agent = new()
{
    Instructions = "You are helpful",
    Kernel = kernel
};
```

Agent Framework:

```csharp
AIAgent agent = new OpenAIClient(apiKey)
    .GetChatClient(modelId)
    .CreateAIAgent(instructions: "You are helpful");
```

### Thread creation

Semantic Kernel:

```csharp
AgentThread thread = new ChatHistoryAgentThread();
```

Agent Framework:

```csharp
AgentThread thread = agent.GetNewThread();
```

### Tool registration

Semantic Kernel:

```csharp
[KernelFunction]
[Description("Get the weather for a location")]
static string GetWeather(string location) => $"Weather in {location}";

KernelFunction function = KernelFunctionFactory.CreateFromMethod(GetWeather);
KernelPlugin plugin = KernelPluginFactory.CreateFromFunctions("Weather", [function]);
kernel.Plugins.Add(plugin);
```

Agent Framework:

```csharp
[Description("Get the weather for a location")]
static string GetWeather(string location) => $"Weather in {location}";

AIAgent agent = chatClient.CreateAIAgent(
    instructions: "You are a helpful assistant",
    tools: [AIFunctionFactory.Create(GetWeather)]);
```

### Invocation

Semantic Kernel:

```csharp
await foreach (var item in agent.InvokeAsync(input, thread, options))
{
    Console.WriteLine(item.Message);
}
```

Agent Framework:

```csharp
AgentResponse result = await agent.RunAsync(input, thread, options);
Console.WriteLine(result);
```

## Special handling

- Use `RawRepresentation` instead of `InnerContent` when inspecting provider-specific SDK objects.
- Use provider-specific extension methods when a feature requires direct access to the underlying client.
- For advanced settings not exposed by `ChatOptions`, preserve provider behavior via the raw representation path or provider-specific model settings.
- For unsupported providers such as Bedrock or CopilotStudio, document the custom-implementation work clearly.

## Validation checklist

The migration is complete only when all of the following are true:

1. The modified projects build successfully.
2. `using Microsoft.SemanticKernel.Agents` no longer appears in the affected code paths.
3. `InvokeAsync` / `InvokeStreamingAsync` are replaced with `RunAsync` / `RunStreamingAsync`.
4. `AgentResponse` is used where the old async enumerable pattern was replaced.
5. Thread creation uses `agent.GetNewThread()`.
6. `[KernelFunction]` and `KernelPlugin` patterns are removed in favor of `AIFunctionFactory.Create(...)`.
7. `AgentInvokeOptions` and `KernelArguments` patterns are replaced by `AgentRunOptions` or `ChatClientAgentRunOptions`.
8. `InnerContent` access is replaced by `RawRepresentation` where breakthrough access is still needed.

## Report requirements

At the end, document in the report:

- all package changes, including removals and additions
- all code files updated and what changed in each one
- provider-specific migration decisions
- unsupported or risky cases
- runtime behavior differences to verify
- follow-up work required after build validation

This skill is deliberately written in a neutral, plain-language format so the same migration guidance can be followed in Claude Code, Copilot CLI, and OpenCode without changing the underlying workflow.
