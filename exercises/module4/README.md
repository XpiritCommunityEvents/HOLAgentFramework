# Module 4: Orchestration of AI Agents

All exercises and labs are described in detail on the [Wiki page](https://github.com/XpiritCommunityEvents/HOLAgentFramework/wiki) that comes with this workshop.

The starter, completed transportation/Harness agents, and booking evaluators
use `GetResponsesClient().AsIChatClient(model)`. Configure the Azure resource
root; the code appends `openai/v1/`. Keep reasoning supported rather than
disabling it to work around Chat Completions tool restrictions. Preserve the
existing session and approval-response loop; evaluator verdicts still use
structured output. Hosted web search remains disabled in the Harness example.
See the [API conventions](../../README.md#model-api-conventions).