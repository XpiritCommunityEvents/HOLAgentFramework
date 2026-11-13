import React, { useEffect, useState } from "react";
import { createRoot } from "react-dom/client";
import { HttpAgent } from "@ag-ui/client";
import { CopilotChat, CopilotKit, useInterrupt } from "@copilotkit/react-core/v2";
import "@copilotkit/react-core/v2/styles.css";

const agentId = "GloboTicketAssistant";
const threadStorageKey = "globoticket.chat.agui.thread";

function getOrCreateThreadId() {
    let id = sessionStorage.getItem(threadStorageKey);
    if (!id) {
        id = crypto.randomUUID();
        sessionStorage.setItem(threadStorageKey, id);
    }
    return id;
}

const threadId = getOrCreateThreadId();
const agent = new HttpAgent({
    agentId,
    threadId,
    url: "/ag-ui"
});

function ApprovalCard({ interrupt, resolve }) {
    const [quote, setQuote] = useState(null);
    const [error, setError] = useState("");
    const [busy, setBusy] = useState(false);
    const [now, setNow] = useState(Date.now());
    const toolCall = agent.messages.flatMap(message => message.toolCalls ?? [])
        .find(call => call.id === interrupt?.toolCallId);
    let args;
    try {
        args = toolCall ? JSON.parse(toolCall.function.arguments) : null;
    } catch {
        args = null;
    }
    const supported = toolCall?.function.name === "submit_order" && typeof args?.quoteId === "string"
        && typeof args?.name === "string" && typeof args?.email === "string";

    useEffect(() => {
        if (!supported) return;
        const controller = new AbortController();
        setQuote(null);
        setError("");
        fetch(`/checkout/quote/${encodeURIComponent(args.quoteId)}`, { signal: controller.signal })
            .then(async response => {
                const body = await response.json();
                if (!response.ok) throw new Error(body.detail ?? "Could not load the quoted purchase.");
                if (body.quoteId !== args.quoteId || !Array.isArray(body.lines)
                    || !Number.isFinite(body.total) || !Number.isFinite(Date.parse(body.expiresAt))) {
                    throw new Error("The quoted purchase response is invalid.");
                }
                setQuote(body);
            })
            .catch(failure => {
                if (failure.name !== "AbortError") setError(failure.message);
            });
        return () => controller.abort();
    }, [args?.quoteId, supported]);

    useEffect(() => {
        const timer = setInterval(() => setNow(Date.now()), 1000);
        return () => clearInterval(timer);
    }, []);

    async function respond(approved) {
        setBusy(true);
        setError("");
        try {
            await resolve({
                approved,
                toolCall: {
                    callId: toolCall.id,
                    name: toolCall.function.name,
                    arguments: args
                }
            }, interrupt.id);
        } catch (failure) {
            setError(failure.message);
        } finally {
            setBusy(false);
        }
    }

    const expired = quote && Date.parse(quote.expiresAt) <= now;
    return (
        <section className="alert alert-info" aria-label="Approve simulated order">
            <h3>Review simulated order</h3>
            {!supported && <p role="alert">This approval request is not a supported ordering action. Do not approve it.</p>}
            {supported && !quote && !error && <p role="status">Loading the authoritative quote...</p>}
            {quote && <>
                <ul>{quote.lines.map(line => (
                    <li key={line.eventId}>{line.eventName}: {line.ticketCount} ticket(s) at USD {line.unitPrice.toFixed(2)}</li>
                ))}</ul>
                <p>Subtotal: USD {quote.subtotal.toFixed(2)}. Discount: USD {quote.discount.toFixed(2)}.
                    {" "}Administration fee: USD {quote.administrationFee.toFixed(2)}.</p>
                <p><strong>Total: USD {quote.total.toFixed(2)}</strong></p>
                <p>Demo customer: {args.name} ({args.email})</p>
                <p>Quote: {quote.quoteId}. Expires: {new Date(quote.expiresAt).toLocaleString()}.</p>
                {expired && <p role="alert">Quote expired. Reject this request and ask for a new quote.</p>}
            </>}
            <p>No payment, email, seat reservation or ticket issuance. Use fictitious details only.</p>
            {error && <p role="alert">{error}</p>}
            <button type="button" className="btn btn-primary mr-2" disabled={!supported || !quote || expired || busy}
                onClick={() => respond(true)}>Approve simulated order</button>
            <button type="button" className="btn btn-secondary" disabled={!supported || busy}
                onClick={() => respond(false)}>Reject</button>
        </section>
    );
}

function OrderingApproval() {
    useInterrupt({
        agentId,
        render: props => <ApprovalCard key={props.interrupt?.id} {...props} />
    });
    return null;
}

function AgUiChat() {
    return (
        <CopilotKit
            agents__unsafe_dev_only={{ [agentId]: agent }}
            enableInspector={true}
        >
            <OrderingApproval />
            <CopilotChat
                agentId={agentId}
                threadId={threadId}
                labels={{
                    chatInputPlaceholder: "Ask the GloboTicket agent anything…",
                    welcomeMessageText: "Find concerts within your budget and place a simulated order after reviewing the quote.",
                    chatDisclaimerText: "Workshop demo: fictitious details only. No payment or seat reservation."
                }}
            />
        </CopilotKit>
    );
}

const root = document.getElementById("agUiChatRoot");
if (root) {
    createRoot(root).render(<AgUiChat />);
}
