import React, { createContext, useContext, useMemo, useState } from "react";
import { useAgent, useCopilotKit } from "@copilotkit/react-core/v2";
import { PrototypeFrame } from "./host.jsx";

export const PrototypeContext = createContext({ uiTools: [], variant: "A" });

export const prototypeRenderers = ["browse_concerts", "quote_tickets", "submit_order", "get_order"]
    .map(name => ({ name, render: PrototypeToolCard }));

function PrototypeToolCard({ name, args, result, toolCallId }) {
    const { uiTools, variant } = useContext(PrototypeContext);
    const resourceUri = uiTools.find(tool => tool.name === name)?._meta?.ui?.resourceUri;
    const { agent } = useAgent({ agentId: "GloboTicketAssistant" });
    const { copilotkit } = useCopilotKit();
    const [error, setError] = useState("");
    const decoded = useMemo(() => {
        try {
            return { result: result === undefined ? null : JSON.parse(result) };
        } catch {
            return { error: `Cannot decode MCP result: ${result}` };
        }
    }, [result]);
    const parsed = decoded.result;
    if (decoded.error) return <pre role="alert">{decoded.error}</pre>;
    if (!parsed) return <p role="status">{name}: waiting for the tool result...</p>;
    if (agent.isRunning) return <p role="status">{name}: finishing the chat turn...</p>;
    if (parsed.isError) return <pre role="alert">{JSON.stringify(parsed, null, 2)}</pre>;
    if (!resourceUri) return <pre>{JSON.stringify(parsed, null, 2)}</pre>;
    async function message(params) {
        try {
            if (agent.isRunning) throw new Error("Wait for the current chat turn before selecting tickets.");
            const text = params.content?.filter(item => item.type === "text").map(item => item.text).join("\n");
            if (!text || text.length > 4000) throw new Error("Widget message is missing or too long.");
            agent.addMessage({ id: crypto.randomUUID(), role: "user", content: text });
            await copilotkit.runAgent({ agent });
            return {};
        } catch (failure) {
            setError(failure.message);
            throw failure;
        }
    }
    return <>
        {error && <p role="alert">{error}</p>}
        <PrototypeFrame key={`${toolCallId}-${variant}`} {...{ name, args, resourceUri, variant }}
            result={parsed} onMessage={message} busy={agent.isRunning} />
    </>;
}
