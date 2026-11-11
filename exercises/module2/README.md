# Module 2: Backstage with Agent Framework

All exercises and labs are described in detail on the [Wiki page](https://github.com/XpiritCommunityEvents/HOLAgentFramework/wiki) that comes with this workshop.

The starter and all four completed exercises use the Responses-backed
`IChatClient`: `openAIClient.GetResponsesClient().AsIChatClient(model)`.
Configure the full Azure OpenAI API base ending in `/openai/v1/`, not an
operation URL. Session history, streaming, skills, middleware, and approval
responses still use the same Agent Framework APIs. The raw HTTP demo 01 is an
intentional Chat Completions comparison. See the
[API conventions](../../README.md#model-api-conventions).

Completed exercise 01 focuses on the agent, context provider, session, and
response formats. Discount tools and anonymous-user middleware are introduced
in completed exercise 02, not registered in exercise 01.