import React, { useState } from "react";
import { createRoot } from "react-dom/client";
import { initialVariant, PrototypeFrame, PrototypeSwitcher } from "./host.jsx";

// Fixtures only. The real /Chat route uses results from the .NET MCP servers.
const events = [
    { eventId: "ee272f8b-6096-4cb6-8625-bb4bb2d89e8b", artist: "John Egbert", name: "John Egbert Live", date: "2027-05-15", price: 64.99, venue: "Madison Square Garden", city: "New York", venueCapacity: 20000 },
    { eventId: "3448d5a4-0f72-4dd7-bf15-c14a46b26c00", artist: "Michael Johnson", name: "The State of Affairs", date: "2027-08-15", price: 84.99, venue: "Staples Center", city: "Los Angeles", venueCapacity: 19000 },
    { eventId: "62787623-4c52-43fe-b0c9-b7044fb5929b", artist: "Manuel Santinonisi", name: "Spanish guitar hits", date: "2027-03-15", price: 24.99, venue: "TD Garden", city: "Boston", venueCapacity: 19580 }
];
const quote = {
    quoteId: "a2222222-2222-4222-8222-222222222222",
    expiresAt: new Date(Date.now() + 15 * 60000).toISOString(),
    lines: [{ eventId: events[0].eventId, eventName: events[0].name, ticketCount: 2, unitPrice: 64.99 }],
    currency: "USD", promoCode: "SAVE10", subtotal: 129.98, discount: 13, administrationFee: 5.85, total: 122.83
};
const data = {
    concerts: { events, notice: "FIXTURE DATA, NOT A LIVE CATALOG. Capacity is not ticket availability." },
    quote,
    expired: { ...quote, expiresAt: new Date(Date.now() - 60000).toISOString() },
    receipt: { orderId: "b3333333-3333-4333-8333-333333333333", quote, status: "FIXTURE RECEIPT. No order was submitted; no payment, email, seat reservation or ticket issuance." }
};
function Preview() {
    const [variant, setVariant] = useState(initialVariant);
    const [scenario, setScenario] = useState("concerts");
    const [message, setMessage] = useState("");
    const result = { content: [{ type: "text", text: JSON.stringify(data[scenario]) }], structuredContent: data[scenario] };
    const uri = scenario === "concerts" ? "ui://globoticket/concerts" : "ui://globoticket/checkout";
    return <>
        <header className="preview-header"><strong>GloboTicket</strong><span>Concerts / Chat</span></header>
        <main className="preview-main">
            <div className="prototype-banner">THROWAWAY / Fixture preview. Real integration: AppHost, then /Chat?mcpUiPrototype=1.</div>
            <h1>GloboTicket assistant</h1>
            <div className="preview-toolbar">{Object.keys(data).map(key =>
                <button key={key} onClick={() => { setScenario(key); setMessage(""); }}>{key}</button>)}</div>
            <p className="preview-message">Assistant / {scenario === "concerts" ? "Here are some concerts to explore. Pick your favorites." :
                scenario === "quote" ? "Here is your full quote, including fees." :
                scenario === "expired" ? "This quote has expired. Request a new quote in chat." : "Here is the simulated receipt."}</p>
            <PrototypeFrame key={`${scenario}-${variant}`} name={scenario} args={{}} {...{ result, resourceUri: uri, variant }}
                onMessage={async params => { setMessage(params.content.map(item => item.text).join("\n")); return {}; }} />
            {message && <div className="prototype-banner"><strong>Widget request captured, not executed:</strong><p>{message}</p></div>}
            <p>Stock DevUI is a text/debugging host, not an MCP Apps renderer. This view exercises the actual SDK and sandbox without model or database dependencies.</p>
        </main>
        <PrototypeSwitcher variant={variant} onChange={setVariant} />
    </>;
}
createRoot(document.getElementById("preview-root")).render(<Preview />);
