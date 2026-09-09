# Agent Framework assessment: module 04 start exercise

This starter is intentionally minimal and uses Agent Framework 1.20.0. It introduces a plain agent before later exercises add tools, approvals, and workflows.

## Learner path

- Configure the OpenAI-compatible model, endpoint, and API key.
- Complete TODO 1 by adapting the injected `IChatClient` with `AsAIAgent(...)` and supplying clear transportation-agent instructions.
- Complete TODO 2 by sending the supplied request with `RunAsync(...)`, preserving cancellation, and displaying the returned response.

The starter builds before either TODO is implemented, but intentionally throws `NotImplementedException` when run. Remove that placeholder as part of completing the exercise, then run the application with configured credentials.
