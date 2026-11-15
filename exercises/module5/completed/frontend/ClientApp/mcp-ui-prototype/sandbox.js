// Throwaway opaque-origin sandbox. Neither frame may acquire the host's origin.
if (window.parent === window) throw new Error("Embed this page in a sandboxed iframe.");
try {
    void window.parent.document;
    throw new Error("Sandbox isolation is missing.");
} catch (error) {
    if (error.name !== "SecurityError") throw error;
}
const hostOrigin = new URL(document.referrer).origin;
const guest = document.createElement("iframe");
guest.setAttribute("sandbox", "allow-scripts");
guest.title = "GloboTicket MCP App";
guest.style = "width:100%;height:100%;border:0;display:block";
document.body.append(guest);
window.addEventListener("message", event => {
    if (event.source === window.parent && event.origin === hostOrigin) {
        if (event.data?.method === "ui/notifications/sandbox-resource-ready") {
            guest.srcdoc = event.data.params.html;
        } else {
            guest.contentWindow.postMessage(event.data, "*");
        }
    } else if (event.source === guest.contentWindow && event.origin === "null") {
        window.parent.postMessage(event.data, hostOrigin);
    }
});
window.parent.postMessage({
    jsonrpc: "2.0", method: "ui/notifications/sandbox-proxy-ready", params: {}
}, hostOrigin);
