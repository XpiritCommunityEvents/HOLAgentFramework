# Module 1: Behind the Curtain of AI Models

All exercises and labs are described in detail on the [Wiki page](https://github.com/XpiritCommunityEvents/HOLAgentFramework/wiki) that comes with this workshop.

Exercise 01 intentionally uses Azure AI Inference; exercise 02 intentionally
uses raw HTTP Chat Completions. In exercise 03, build the multimodal Agent
Framework client with `GetResponsesClient().AsIChatClient(model)`, as in the
completed version. Keep its endpoint at the Azure resource root; the client
appends `openai/v1/`. See the [API conventions](../../README.md#model-api-conventions)
for endpoint shapes and provider limitations.