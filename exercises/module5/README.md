# Module 5: Agent Framework in Concert

All exercises and labs are described in detail on the [Wiki page](https://github.com/XpiritCommunityEvents/HOLAgentFramework/wiki) that comes with this workshop.

The completed frontend's outbound model client uses
`GetResponsesClient().AsIChatClient(model)`, including MCP function tools and
streaming. Aspire still supplies the full `/openai/v1/` API base, model, and key;
do not hard-code service URLs. The frontend's own Responses/Conversations and
AG-UI endpoints are separate from this outbound model transport and are unchanged.
Configure `Parameters:openai-api-key` in the AppHost's own user secrets.
There is no Module 5 starter project in this repository. See the
[API conventions](../../README.md#model-api-conventions).
