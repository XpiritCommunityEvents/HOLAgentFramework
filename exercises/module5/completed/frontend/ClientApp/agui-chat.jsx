import React from "react";
import { createRoot } from "react-dom/client";
import { HttpAgent } from "@ag-ui/client";
import { CopilotChat, CopilotKit } from "@copilotkit/react-core/v2";
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

function AgUiChat() {
    return (
        <CopilotKit
            agents__unsafe_dev_only={{ [agentId]: agent }}
            enableInspector={true}
        >
            <CopilotChat
                agentId={agentId}
                threadId={threadId}
                labels={{
                    chatInputPlaceholder: "Ask the GloboTicket agent anything…",
                    welcomeMessageText: "I can help you discover concerts, dates, venues, and ticket options.",
                    chatDisclaimerText: "AG-UI streams messages, lifecycle events, and tool calls over SSE."
                }}
            />
        </CopilotKit>
    );
}

const root = document.getElementById("agUiChatRoot");
if (root) {
    createRoot(root).render(<AgUiChat />);
}
