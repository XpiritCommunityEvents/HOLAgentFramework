import React, { useEffect, useMemo, useState } from "react";
import { AppRenderer } from "@mcp-ui/client";
import "./host.css";

const variants = ["A", "B", "C"];
const labels = { A: "Concert cards", B: "Evening planner", C: "Comparison desk" };

export function PrototypeSwitcher({ variant, onChange }) {
    function cycle(delta) {
        const next = variants[(variants.indexOf(variant) + delta + variants.length) % variants.length];
        const url = new URL(window.location.href);
        url.searchParams.set("variant", next);
        history.replaceState(null, "", url);
        onChange(next);
    }
    useEffect(() => {
        const keydown = event => {
            if (event.target.closest("input,textarea,select,[contenteditable]") ||
                event.altKey || event.ctrlKey || event.metaKey) return;
            if (event.key === "ArrowLeft" || event.key === "ArrowRight") {
                event.preventDefault();
                cycle(event.key === "ArrowLeft" ? -1 : 1);
            }
        };
        window.addEventListener("keydown", keydown);
        return () => window.removeEventListener("keydown", keydown);
    }, [variant]);
    if (process.env.NODE_ENV === "production") return null;
    return <nav className="prototype-switcher" aria-label="Prototype variants">
        <button aria-label="Previous variant" onClick={() => cycle(-1)}>&larr;</button>
        <span>{variant} / {labels[variant]}</span>
        <button aria-label="Next variant" onClick={() => cycle(1)}>&rarr;</button>
    </nav>;
}

export function initialVariant() {
    const value = new URLSearchParams(location.search).get("variant");
    return variants.includes(value) ? value : "A";
}

export async function readPrototypeResource({ uri }) {
    const response = await fetch(`/mcp-ui-prototype/resource?uri=${encodeURIComponent(uri)}`);
    if (!response.ok) throw new Error(`MCP resource read failed (${response.status}).`);
    return response.json();
}

export function PrototypeFrame({ name, args, result, resourceUri, variant, onMessage, busy = false,
    readResource = readPrototypeResource }) {
    const [error, setError] = useState("");
    const sandbox = useMemo(() => ({
        url: new URL("/mcp-ui-prototype/sandbox", location.origin), permissions: "allow-scripts"
    }), []);
    const toolInput = useMemo(() => ({
        ...args, __prototypeVariant: variant, __prototypeBusy: busy
    }), [args, variant, busy]);
    return <section className="prototype-tool" aria-label={`${name} interactive result`}>
        <div className="prototype-tool-label">MCP APP / {name}</div>
        {error ? <p role="alert">{error} Text result remains available below.</p> :
            <AppRenderer key={`${name}-${variant}`}
                toolName={name} toolResourceUri={resourceUri}
                sandbox={sandbox}
                onReadResource={readResource}
                toolInput={toolInput} toolResult={result}
                onMessage={onMessage}
                onCallTool={async () => { throw new Error("Direct widget tool calls are disabled. Continue in chat; submission requires approval."); }}
                onOpenLink={async () => { throw new Error("External navigation is disabled in this prototype."); }}
                onError={failure => { console.error(failure); setError(failure.message); }}
            />}
        <details className="prototype-state"><summary>Host state / text fallback</summary>
            <pre>{JSON.stringify({ variant, resourceUri, args, result }, null, 2)}</pre>
        </details>
    </section>;
}

