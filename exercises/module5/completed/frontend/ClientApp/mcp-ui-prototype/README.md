# GloboTicket MCP Apps prototype

**Throwaway branch only.** Question: can the catalog and ordering MCPs supply interactive
concert selection, quote breakdowns and receipts to GloboTicket chat, without replacing
the agent or bypassing order approval?

**Verdict:** the MCP Apps resource/metadata pattern works with the .NET MCP SDK and
`@mcp-ui/client`. Three different layouts are available; no design winner has been chosen.
Stock Agent Framework DevUI is not an MCP Apps host. Keep its text results and use the
Aspire dashboard's **MCP Apps prototype (DevUI companion)** link for interactive rendering.
This is not a DevUI plugin or a production integration.

## Run

From `exercises/module5/completed`, after `npm ci`:

```bash
npm run prototype:mcp-ui
```

Open <http://localhost:4173/Chat?variant=A>. This standalone preview uses clearly labeled,
in-memory fixtures; clicking a widget captures its chat request but does not execute it.
The preview uses the real MCP Apps guest SDK, AppRenderer and sandbox handshake.

For the existing distributed app, build bundles first and opt in:

```bash
npm run build:chat
McpUiPrototype__Enabled=true dotnet run --project AppHost/AppHost.csproj
```

In PowerShell, set `$env:McpUiPrototype__Enabled = "true"` before running the AppHost.
Use the dashboard link or append `/Chat?mcpUiPrototype=1&variant=A` to the frontend URL.
Normal `/Chat` remains unchanged. The feature and its HTTP proxy endpoints are restricted
to Development, and AppHost enables it only in run mode.

**Existing prerequisites still apply:** the AppHost OpenAI parameter, a Responses-compatible
model deployment and SQL Server. Startup of the existing Development catalog recreates its
database. Use only the workshop's disposable databases, not a shared catalog with data to keep.
An actual live model/AppHost checkout was not run during this prototype's verification.

## Explore

| Variant | Discovery | Quote / receipt |
| --- | --- | --- |
| A | Poster-like concert cards | Large all-in price beside selected tickets |
| B | Date-based evening planner | Checkout steps and a separate price panel |
| C | Dense comparison table | Invoice-style cost ledger |

Use the floating arrows or keyboard left/right to update `?variant=A|B|C`.
Arrow keys remain available inside editable controls. Expand **Widget state** or
**Host state / text fallback** to see the complete data and selection.

In real chat, try “Show me John Egbert concerts,” select concerts and ticket counts,
then **Ask for a quote**. The widget sends a user message into the existing AG-UI chat;
the agent calls `quote_tickets`. **Continue in chat** requests the normal checkout flow,
including fictitious customer details and the existing separate approval card.
After approval, `submit_order` / `get_order` can display a simulated receipt.
No widget calls `submit_order` directly, grants approval, reserves seats or takes payment.
Expired quotes cannot continue through the widget. Existing approval, expiry and
idempotency rules remain authoritative.

## How it fits

1. The catalog adds a bounded, read-only `browse_concerts` prototype tool with structured
   concert data. Existing catalog tools retain their original behavior.
2. `browse_concerts` links to `ui://globoticket/concerts` through tool `_meta.ui.resourceUri`.
   Ordering's existing quote/submission/receipt tools link to `ui://globoticket/checkout`;
   their schemas, structured results and safety annotations are preserved.
3. Both servers expose self-contained HTML as `text/html;profile=mcp-app`. They embed
   the same generated widget, avoiding separate HTML implementations.
4. The frontend discovers tool metadata and exposes only those UI resource reads through
   its existing server-side MCP connections. There is no arbitrary URI or tool-call proxy,
   and no browser-held model credentials.
5. CopilotKit v2 tool renderers pass completed MCP result envelopes to `AppRenderer`.
   The widget uses MCP Apps `ui/message` to request the next chat turn.

The sandbox response enforces **`sandbox allow-scripts` through a CSP HTTP header**.
This matters: the installed MCP-UI renderer hardcodes a more permissive iframe attribute;
passing `sandbox.permissions` alone does not enforce opaque-origin isolation.
Both frames are opaque-origin, messages are source/origin-checked, network access,
forms and external links are blocked, and the proxy refuses to run if it can access
its parent document. This intentionally supports only these self-contained widgets.
Do not relax these restrictions to render arbitrary remote HTML.

Verification covered the existing .NET contracts, actual .NET MCP metadata/resource reads
and catalog/quote/receipt calls against an in-memory fixture database. A fresh Chrome
session also exercised the actual frontend resource proxy and tool renderer, concert selection,
widget-to-AG-UI follow-up, and quote rendering using captured MCP payloads and a stubbed model
transport. Fixture preview checks covered all layouts, expired quotes and a narrow viewport.
It does not claim a live model-driven end-to-end checkout.
The npm audit still reports upstream Zod 3 / math-rendering advisories, including Zod
propagating through the MCP-UI client; no incompatible transitive major upgrade was forced.

## Sources and promotion

- [MCP-UI introduction](https://mcpui.dev/guide/introduction): MCP-UI now implements MCP Apps.
- [AppRenderer reference](https://mcpui.dev/guide/client/app-renderer): backend resource-read
  callbacks and guest message handlers.
- [MCP Apps](https://github.com/modelcontextprotocol/ext-apps): guest/host protocol.
- [DevUI frontend dependencies](https://github.com/microsoft/agent-framework/blob/main/python/packages/devui/frontend/package.json)
  and [content renderer](https://github.com/microsoft/agent-framework/blob/main/python/packages/devui/frontend/src/components/features/agent/message-renderers/OpenAIContentRenderer.tsx):
  text/data/approval rendering, not an MCP Apps host.

`build.mjs` regenerates the embedded HTML and tracked browser bundles. Keep them synchronized
with the sources and lockfile. The prototype has no database or persistent selection of its own.
Retain this experiment on its throwaway branch. After a layout is chosen, implement the
validated design deliberately; do not merge all variants, the switcher or fixture preview into main.
