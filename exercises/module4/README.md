# Module 4: Orchestration of AI Agents

All exercises and labs are described in detail on the [Wiki page](https://github.com/XpiritCommunityEvents/HOLAgentFramework/wiki) that comes with this workshop.

The starter, loop agent, and booking evaluators use the Responses client directly
with `AsAIAgent`. The Harness agent uses `AsIChatClient(model)` because
`AsHarnessAgent` requires an `IChatClient`. Configure the Azure resource root;
the code appends `openai/v1/`. The evaluator requests a structured verdict, and
the Harness example preserves the approval-response loop with hosted web search disabled.
See the [API conventions](../../README.md#model-api-conventions).