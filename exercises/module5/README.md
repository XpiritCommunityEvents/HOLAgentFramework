# Module 5: Agent Framework in Concert

All exercises and labs are described in detail on the [Wiki page](https://github.com/XpiritCommunityEvents/HOLAgentFramework/wiki) that comes with this workshop.

The completed sample combines catalog discovery with **simulated ticket orders**.
There is no Module 5 starter project. See the [API conventions](../../README.md#model-api-conventions).

## Architecture and boundaries

The Aspire AppHost composes the catalog, ordering service, frontend and DevUI.
Catalog owns events and current prices. Ordering fetches catalog prices independently,
calculates quotes, and persists immutable quotes and receipts in its own `OrderingDb`
on the existing SQL Server. Both HTTP checkout and MCP tools use this same ordering
service. Aspire supplies service endpoints; do not hard-code interservice URLs.

The frontend discovers tools from both MCP servers and creates its agent through
`GetResponsesClient().AsAIAgent(...)`. Aspire supplies the full `/openai/v1/` API base,
model and key. This outbound model transport is separate from the frontend's inbound
AG-UI and Responses/Conversations endpoints.

This is a **trusted workshop**, not an authenticated or multi-tenant shop. Use
fictitious name/email only. No payment, email delivery, inventory reservation or
ticket issuance occurs. Venue capacity is not available ticket inventory. Order
and conversation IDs are not access control; do not expose these endpoints to
untrusted users. Receipt resources omit customer contact details.

## Run and try the journey

Use the repository's .NET 10 SDK and start Docker. Configure
`Parameters:openai-api-key` in the **AppHost's own** user secrets, not the shared
standalone-demo secrets. From the repository root:

```sh
dotnet build exercises/module5/completed/GloboTicket.slnx
aspire start --non-interactive --apphost exercises/module5/completed/AppHost/AppHost.csproj
aspire wait GloboTicketAssistant --apphost exercises/module5/completed/AppHost/AppHost.csproj
aspire describe --apphost exercises/module5/completed/AppHost/AppHost.csproj
```

Open the frontend URL shown by Aspire and select the AG-UI chat. Ask:

> Find two tickets for an artist in a city within my budget, show me the total,
> then place the simulated order after I approve.

Choose an artist/city from the running catalog, give a USD budget and fictitious
customer details, and review the quoted lines, discount, fee, total and expiry.
**Reject** first: no order is submitted. Ask to try again, then **Approve simulated
order** to receive a persisted order reference. The regular basket/checkout uses
the same quoted totals and requests no card or postal address.

DevUI points at the **same agent** through its Responses endpoint. Its built-in
tool-approval controls support this flow; it does not use the website's React card.
The website's AG-UI card adds an authoritative quote fetched from ordering beside
the approve/reject buttons, rather than relying solely on generated prose. The
framework's approval gate binds the decision to the server's pending tool call.
Named agent registration and hosted session stores are needed for continuation.
The AG-UI compatibility adapter also removes CopilotKit's synthetic approval-result
messages: a decision is not a receipt and must not suppress the server's tool execution.
Chat sessions and pending approvals are in memory: a frontend restart requires a
new conversation/approval. Persisted quotes and orders are separate and survive it.
SignalR remains for comparison; new ordering UI behavior targets AG-UI.

Stop only this AppHost when finished:

```sh
aspire stop --non-interactive --apphost exercises/module5/completed/AppHost/AppHost.csproj
```

The SQL data volume survives an ordinary stop. The catalog reseeds on development
startup; ordering retains historical event names/prices in its quote snapshots.
Do not delete the volume unless intentionally discarding the workshop data.

## Pricing and retry contract

Quotes last **15 minutes**, use USD, and accept 1-20 distinct events with 1-100
tickets per event. `SAVE10`, `SAVE15` and `SAVE25` apply to current catalog prices,
including special offers. Discounted event lines round to cents, midpoint-to-even.
The administration fee is 5% of the discounted subtotal below USD 500, rounded to
cents; at USD 500 or above it is zero. `maxTotal` bounds the final total including fees.

Submit an immutable `quoteId` with fictitious `name` and `email`. One quote creates
at most one order. Retrying with the same normalized details returns the original
receipt, even after expiry; different details conflict. An unused expired quote
requires a new quote and approval, never silent repricing. The shared HTTP surface
is `POST /order/quotes`, `GET /order/quotes/{id}`, `POST /order`, and `GET /order/{id}`.

## MCP capabilities and client support

Both services expose Streamable HTTP at their Aspire-discovered URL plus `/mcp`.

| Capability | Catalog | Ordering | Where to demonstrate |
| --- | --- | --- | --- |
| Tools | Existing event/artist/location/date discovery | `quote_tickets`, `submit_order`, `get_order` | AG-UI, DevUI, Inspector, headless clients |
| Structured results and safety annotations | Existing discovery contracts preserved | Typed quote/receipt results; write/read and retry hints | Inspector; agent tool calls |
| Resource templates | `catalog://events/{eventId}` | `ordering://orders/{orderId}` | Resource-aware supporting client |
| Static resource | - | `ordering://policy` | Resource-aware supporting client |
| Prompts | `discover_concerts` | `guided_checkout` | Prompt-aware supporting client |
| Argument completion | Artist prefix in `discover_concerts`, bounded to 20 suggestions | - | Completion-aware supporting client |
| Optional form elicitation | - | `collect_demo_customer` | Supporting client advertising form elicitation |
| Approval | - | Client policy, not mandatory server elicitation | AG-UI quote card; native DevUI controls; headless policy |

Listing tools does **not** automatically add resource browsing, prompt selection,
completion or elicitation screens to AG-UI or DevUI. The frontend deliberately
does not expose the optional elicitation tool to its agent; it collects details in
conversation instead. Ordering uses stateful HTTP transport so supporting clients
can answer server-to-client elicitation requests. Elicitation only collects data:
accepting the form never approves or submits an order. Unsupported, declined,
cancelled and invalid form responses return explicit tool errors.

### Inspector walkthrough

The completed AppHost starts the official MCP Inspector **2.15.0** container for
local development. Open the **MCP Inspector** link on the `mcp-inspector` resource
in the Aspire dashboard. The `catalog` and `ordering` servers are preconfigured
with Aspire-discovered, container-reachable `/mcp` endpoints; select a server and
connect. The Inspector keeps its built-in authentication enabled and allows the
Aspire-assigned localhost browser origin. Use its dashboard link rather than a
`dev.localhost` alias, which is not in the origin allowlist.
Do not expose it publicly or forward its port
with public visibility in Codespaces. Its server list is generated on each
startup; no separate Node.js installation or manual URL configuration is needed.
Inspector is omitted when publishing the AppHost.

Alternatively, run `npx @modelcontextprotocol/inspector`, select **Streamable HTTP**,
and connect to the catalog's discovered `/mcp` endpoint.

For catalog, list tools, prompts and resource
templates. Use discovery tools to obtain an event UUID; read its
`catalog://events/{eventId}` resource. Select `discover_concerts` with `artist`,
`city` and `budgetUsd`; enter `El` in `artist` to see the `Elton John` completion.

Disconnect from `catalog`, then connect to `ordering` (or use another client
connected to its `/mcp` endpoint). Read `ordering://policy`,
get the `guided_checkout` prompt with `eventId`, `ticketCount` and `budgetUsd`,
then call `quote_tickets` with `lines: [{ eventId, ticketCount }]`, optionally
`promoCode` and `maxTotal`. Inspect the result's full price breakdown and expiry.
If the client supports form elicitation, try `collect_demo_customer`, including
decline/cancel; otherwise supply fictitious details directly.

After explicitly reviewing/authorizing the purchase in your client, call
`submit_order` with `quoteId`, `name` and `email`. Repeat the same call and verify
the same order reference; call `get_order` and read `ordering://orders/{orderId}`.
The MCP service does not provide production authorization or force a human dialog.
A future Module 4 harness can discover, quote, apply its predefined approval
policy, submit and retrieve the receipt without a browser or elicitation callback.
No Module 4 implementation is included here.

Progress notifications, sampling and subscriptions are not simulated: this scenario
has no long-running task or changing inventory feed requiring them.

## Checks

From the repository root:

```sh
dotnet test exercises/module5/completed/GloboTicket.slnx
```

After chat-source edits, rebuild tracked bundles and run the focused browser checks:

```sh
cd exercises/module5/completed
npm run build:chat
npx playwright install chromium --only-shell
npx playwright test frontend/tests/agui-ordering.spec.cjs --workers=1
```

The browser checks use the real bundle with intercepted AG-UI/quote responses to
verify rendering, approve/reject payloads, expiry and errors without a model key.
MSTest exercises the actual MCP, AG-UI and hosted Responses HTTP protocols with
deterministic dependencies. These checks do not replace the live cross-service
journey above, which needs the AppHost model key, model access and working SQL Server.
