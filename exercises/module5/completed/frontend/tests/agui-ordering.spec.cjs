const { test, expect } = require("@playwright/test");
const { readFileSync } = require("node:fs");
const { join } = require("node:path");

const quoteId = "00000000-0000-0000-0000-000000000001";

async function openOrderingChat(page, { expired = false, missing = false } = {}) {
    const resumes = [];
    await page.route("http://localhost:4173/**", async route => {
        const path = new URL(route.request().url()).pathname;
        if (path === "/") {
            return route.fulfill({ contentType: "text/html", body:
                '<html><body><div id="agUiChatRoot"></div><script src="/agui-chat.js"></script></body></html>' });
        }
        if (path === "/agui-chat.js") {
            return route.fulfill({ contentType: "application/javascript",
                body: readFileSync(join(__dirname, "../wwwroot/js/agui-chat.js"), "utf8") });
        }
        if (path === `/checkout/quote/${quoteId}`) {
            return route.fulfill({ status: missing ? 404 : 200, json: missing ? { detail: "Quote not found." } : {
                quoteId, expiresAt: new Date(Date.now() + (expired ? -60000 : 60000)).toISOString(),
                lines: [{ eventId: "concert", eventName: "Demo Concert", ticketCount: 2, unitPrice: 100 }],
                currency: "USD", subtotal: 200, discount: 20, administrationFee: 9, total: 189
            } });
        }
        if (path === "/ag-ui") {
            const input = route.request().postDataJSON();
            const events = [{ type: "RUN_STARTED", threadId: input.threadId, runId: input.runId }];
            if (input.resume) {
                resumes.push(input.resume);
                events.push(
                    { type: "TOOL_CALL_RESULT", messageId: "result", toolCallId: "order-call", content: "Handled." },
                    { type: "TEXT_MESSAGE_START", messageId: "receipt", role: "assistant" },
                    { type: "TEXT_MESSAGE_CONTENT", messageId: "receipt",
                        delta: input.resume[0].payload.approved ? "Simulated order DEMO-123 recorded for USD 189.00." : "Order rejected. No order was submitted." },
                    { type: "TEXT_MESSAGE_END", messageId: "receipt" },
                    { type: "RUN_FINISHED", threadId: input.threadId, runId: input.runId, outcome: { type: "success" } });
            } else {
                events.push(
                    { type: "TOOL_CALL_START", toolCallId: "order-call", toolCallName: "submit_order", parentMessageId: "proposal" },
                    { type: "TOOL_CALL_ARGS", toolCallId: "order-call", delta: JSON.stringify({
                        quoteId, name: "Demo Customer", email: "demo@example.test"
                    }) },
                    { type: "TOOL_CALL_END", toolCallId: "order-call" },
                    { type: "RUN_FINISHED", threadId: input.threadId, runId: input.runId, outcome: {
                        type: "interrupt", interrupts: [{ id: "approval", reason: "tool_call", toolCallId: "order-call",
                            responseSchema: { type: "object", properties: { approved: { type: "boolean" } } } }]
                    } });
            }
            return route.fulfill({ contentType: "text/event-stream",
                body: events.map(event => `data: ${JSON.stringify(event)}\n\n`).join("") });
        }
        return route.abort();
    });
    await page.goto("http://localhost:4173/");
    const input = page.getByRole("textbox");
    await input.fill("Find two tickets and quote a simulated order.");
    await input.press("Enter");
    await expect(page.getByRole("heading", { name: "Review simulated order" })).toBeVisible();
    return resumes;
}

for (const approved of [true, false]) {
    test(`reviews the authoritative quote before explicit ${approved ? "approval" : "rejection"}`, async ({ page }) => {
        const resumes = await openOrderingChat(page);
        const card = page.getByRole("region", { name: "Approve simulated order" });
        await expect(card).toContainText("Demo Concert: 2 ticket(s) at USD 100.00");
        await expect(card).toContainText("Total: USD 189.00");
        await expect(card).toContainText("Administration fee: USD 9.00");
        await expect(card).toContainText("Demo Customer (demo@example.test)");
        expect(resumes).toHaveLength(0);
        await card.getByRole("button", { name: approved ? "Approve simulated order" : "Reject", exact: true }).click();
        await expect(page.getByText(approved ? "Simulated order DEMO-123 recorded for USD 189.00." :
            "Order rejected. No order was submitted.", { exact: true })).toBeVisible();
        expect(resumes).toHaveLength(1);
        expect(resumes[0][0]).toMatchObject({
            interruptId: "approval", status: "resolved", payload: {
                approved, toolCall: { callId: "order-call", name: "submit_order",
                    arguments: { quoteId, name: "Demo Customer", email: "demo@example.test" } }
            }
        });
    });
}

test("an expired quote cannot be approved but can be rejected", async ({ page }) => {
    const resumes = await openOrderingChat(page, { expired: true });
    await expect(page.getByRole("alert")).toContainText("Quote expired");
    await expect(page.getByRole("button", { name: "Approve simulated order", exact: true })).toBeDisabled();
    await expect(page.getByRole("button", { name: "Reject", exact: true })).toBeEnabled();
    expect(resumes).toHaveLength(0);
});

test("a missing authoritative quote surfaces an error instead of allowing approval", async ({ page }) => {
    const resumes = await openOrderingChat(page, { missing: true });
    await expect(page.getByRole("alert")).toContainText("Quote not found.");
    await expect(page.getByRole("button", { name: "Approve simulated order", exact: true })).toBeDisabled();
    expect(resumes).toHaveLength(0);
});
