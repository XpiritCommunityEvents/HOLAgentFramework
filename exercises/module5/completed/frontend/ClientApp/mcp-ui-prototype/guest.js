import { App } from "@modelcontextprotocol/ext-apps";

// Three throwaway concert/checkout layouts, selected by the host's ?variant=A|B|C.
const app = new App({ name: "GloboTicket prototype", version: "0.1.0" }, {});
const state = { variant: "A", result: null, selected: [], ticketCount: 2, busy: false, expired: false, lastAction: "Waiting", error: "" };
const main = document.getElementById("app");
const esc = value => String(value ?? "").replace(/[&<>"']/g, char =>
    ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[char]);
const usd = value => new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(value);
const date = value => new Date(`${String(value).slice(0, 10)}T12:00:00`)
    .toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" });
const normalize = value => Array.isArray(value) ? value.map(normalize) :
    value && typeof value === "object" ? Object.fromEntries(Object.entries(value)
        .map(([key, item]) => [key[0].toLowerCase() + key.slice(1), normalize(item)])) : value;
const events = () => state.result?.events ?? [];
const quote = () => state.result?.quote ?? (state.result?.quoteId ? state.result : null);
const selected = item => state.selected.includes(item.eventId);
const selectButton = item => `<button data-select="${esc(item.eventId)}" aria-pressed="${selected(item)}" ${state.busy ? "disabled" : ""}>${selected(item) ? "Selected" : "Compare / select"}</button>`;

function VariantA() {
    return `<div class="eyebrow">LIVE CATALOG / DISCOVER</div><h1>Your next great night.</h1>
        <p>Concerts first. Pick your favorites, then ask for a price including fees.</p>
        <div class="cards">${events().map((item, index) => `<article class="concert ${selected(item) ? "selected" : ""}">
            <div class="poster poster-${index % 3}"><span>LIVE<br>${esc(item.artist)}</span></div>
            <div class="card-body"><small>${esc(date(item.date))} / ${esc(item.city)}</small>
            <h2>${esc(item.name)}</h2><p>${esc(item.venue)}</p>
            <strong>${usd(item.price)} <small>per ticket</small></strong>${selectButton(item)}</div></article>`).join("")}</div>`;
}
function VariantB() {
    return `<div class="planner"><aside><div class="eyebrow">PLAN YOUR EVENING</div><h1>Make a<br>night of it.</h1>
        <p>Explore the dates. Select one or more nights to price together.</p><div class="large-number">${events().length}</div><p>catalog matches</p></aside>
        <ol class="timeline">${events().map(item => `<li class="${selected(item) ? "selected" : ""}">
            <div class="date">${esc(date(item.date))}</div><div><h2>${esc(item.artist)}</h2><p>${esc(item.venue)} / ${esc(item.city)}</p>
            <small>${esc(item.name)}</small></div><div class="timeline-action"><strong>${usd(item.price)}</strong>${selectButton(item)}</div></li>`).join("")}</ol></div>`;
}
function VariantC() {
    return `<div class="eyebrow">COMPARISON DESK</div><h1>Find the right fit.</h1>
        <p>Compare date, venue and ticket price before asking for an authoritative quote.</p>
        <div class="table-scroll"><table><thead><tr><th>Concert</th><th>Date / city</th><th>Venue capacity*</th><th>USD / ticket</th><th>Select</th></tr></thead>
        <tbody>${events().map(item => `<tr class="${selected(item) ? "selected" : ""}"><td><strong>${esc(item.artist)}</strong><br>${esc(item.name)}</td>
            <td>${esc(date(item.date))}<br>${esc(item.city)}</td><td>${esc(item.venueCapacity ?? "Unknown")}</td><td>${usd(item.price)}</td><td>${selectButton(item)}</td></tr>`).join("")}</tbody></table></div>
        <small>*Capacity is not live ticket availability.</small>`;
}
function checkout() {
    const value = quote();
    const receipt = Boolean(state.result?.orderId);
    const expired = !receipt && new Date(value.expiresAt).getTime() <= Date.now();
    const breakdown = `<dl><dt>Subtotal</dt><dd>${usd(value.subtotal)}</dd><dt>Discount ${esc(value.promoCode)}</dt><dd>-${usd(value.discount)}</dd>
        <dt>Administration fee</dt><dd>${usd(value.administrationFee)}</dd><dt class="total">Total USD</dt><dd class="total">${usd(value.total)}</dd></dl>`;
    const lines = `<ul class="quote-lines">${value.lines.map(line => `<li><strong>${esc(line.eventName)}</strong><span>${line.ticketCount} x ${usd(line.unitPrice)}</span></li>`).join("")}</ul>`;
    const heading = receipt ? "Your simulated receipt." : expired ? "This quote has expired." : "Know the whole price.";
    const status = receipt ? `<p class="receipt-status">${esc(state.result.status)}</p><p>Order reference <code>${esc(state.result.orderId)}</code></p>` :
        `<p>Quote <code>${esc(value.quoteId)}</code><br>Expires ${esc(new Date(value.expiresAt).toLocaleString())}</p>
        <button data-continue ${expired || state.busy ? "disabled" : ""}>Continue in chat / review approval</button>
        <p class="notice">This button is not approval and does not submit an order. The host's separate approval control is required.</p>`;
    if (state.variant === "B") return `<div class="eyebrow">GUIDED CHECKOUT</div><h1>${heading}</h1>
        <ol class="steps"><li>1 / Select concerts</li><li>2 / Quote total</li><li>3 / Host approval</li><li>4 / Simulated receipt</li></ol>
        <div class="checkout-columns"><section>${lines}${status}</section><section class="price-panel">${breakdown}</section></div>`;
    if (state.variant === "C") return `<div class="invoice"><div class="eyebrow">GLOBOTICKET / ${receipt ? "RECEIPT" : "QUOTE"}</div>
        <h1>${heading}</h1>${lines}${breakdown}${status}</div>`;
    return `<div class="eyebrow">${receipt ? "SIMULATION COMPLETE" : "YOUR QUOTE"}</div><h1>${heading}</h1>
        <div class="checkout-columns"><section>${lines}${status}</section><aside class="price-panel"><div class="hero-price">${usd(value.total)}</div><p>All fees included</p>${breakdown}</aside></div>`;
}
function render() {
    if (!state.result) return;
    const concertResult = Array.isArray(state.result.events);
    main.innerHTML = `${concertResult ? ({ A: VariantA, B: VariantB, C: VariantC })[state.variant]() :
        quote() ? checkout() : "<p>No supported structured data in this tool result.</p>"}
        ${concertResult ? `<div class="selection-bar"><strong>${state.selected.length} selected</strong>
            <label>Tickets per concert <input id="ticket-count" type="number" min="1" max="100" value="${state.ticketCount}"></label>
            <button data-quote ${state.selected.length && !state.busy ? "" : "disabled"}>Ask for a quote</button></div>
            ${events().length ? "" : "<p>No matching concerts. Try a different artist or city.</p>"}` : ""}
        ${state.busy ? '<p role="status">Finishing the current chat turn. Selection will be available shortly.</p>' : ""}
        <p class="notice">${esc(state.result.notice ?? "Workshop simulation: no payment, email, seat reservation or ticket issuance.")}</p>
        ${state.error ? `<p role="alert">${esc(state.error)}</p>` : ""}
        <details class="state"><summary>Widget state / ${esc(state.lastAction)}</summary><pre>${esc(JSON.stringify(state, null, 2))}</pre></details>`;
    main.querySelectorAll("[data-select]").forEach(button => button.onclick = () => {
        const id = button.dataset.select;
        state.selected = state.selected.includes(id) ? state.selected.filter(value => value !== id) : [...state.selected, id];
        state.lastAction = "Selection changed";
        render();
    });
    const count = main.querySelector("#ticket-count");
    if (count) count.onchange = () => {
        state.ticketCount = Math.max(1, Math.min(100, Math.trunc(Number(count.value) || 1)));
        state.lastAction = "Ticket count changed";
        render();
    };
    const request = main.querySelector("[data-quote]");
    if (request) request.onclick = () => send(`Please quote these catalog tickets including discounts and administration fees: ${JSON.stringify(
        state.selected.map(eventId => ({ eventId, ticketCount: state.ticketCount })))}. Do not submit an order yet.`);
    const proceed = main.querySelector("[data-continue]");
    if (proceed) proceed.onclick = () => send(`I want to continue with quote ${quote().quoteId}. Ask for any missing fictitious customer details, then use the separate host approval control. This widget message is not approval.`);
}
async function send(text) {
    state.error = "";
    state.lastAction = "Sending request to chat";
    render();
    try {
        await app.sendMessage({ role: "user", content: [{ type: "text", text }] });
        state.lastAction = "Request sent to chat (not approval)";
    } catch (error) {
        state.error = error.message;
        state.lastAction = "Request failed";
    }
    render();
}
app.ontoolinput = ({ arguments: args }) => {
    state.busy = Boolean(args.__prototypeBusy);
    state.variant = ["A", "B", "C"].includes(args.__prototypeVariant) ? args.__prototypeVariant : "A";
    render();
};
app.ontoolresult = result => {
    state.result = normalize(result.structuredContent);
    state.expired = false;
    state.lastAction = "Tool result received";
    if (!state.result) {
        main.textContent = "The MCP tool returned no structured content. Use the host's text fallback.";
        return;
    }
    render();
};
app.connect().catch(error => { main.textContent = `MCP Apps connection failed: ${error.message}`; });
setInterval(() => {
    if (quote() && !state.result?.orderId && !state.expired &&
        new Date(quote().expiresAt).getTime() <= Date.now()) {
        state.expired = true;
        state.lastAction = "Quote expired";
        render();
    }
}, 1000);
