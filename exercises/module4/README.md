# Module 4: Orchestration of AI Agents

All exercises and labs are described in detail on the [Wiki page](https://github.com/XpiritCommunityEvents/HOLAgentFramework/wiki) that comes with this workshop.

The starter, loop agent, and booking evaluators use the Responses client directly
with `AsAIAgent`. The Harness agent uses `AsIChatClient(model)` because
`AsHarnessAgent` requires an `IChatClient`. Configure the Azure resource root;
the code appends `openai/v1/`. The evaluator requests a structured verdict, and
the Harness example preserves the approval-response loop with hosted web search disabled.
See the [API conventions](../../README.md#model-api-conventions).

All Module 4 console samples stream responses through an `AgentResponsePrinter`
so text, reasoning summaries (when supplied by the model), tool calls and results,
approval requests, errors, and token usage are visible as updates arrive.
Demo 04 also labels agents and workflow executor start/completion events for its
orchestration patterns; concurrent agent updates are printed atomically, and the
final itinerary is not printed a second time. Agent-as-tool calls expose the
delegation and returned result, not a separate stream of the specialist's internals.
This is local console diagnostics, not exported telemetry. Tool arguments and
results may contain sensitive data; do not share console logs without reviewing them.