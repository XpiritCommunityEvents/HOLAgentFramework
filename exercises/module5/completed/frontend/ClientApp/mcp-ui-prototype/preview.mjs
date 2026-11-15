import { createServer } from "node:http";
import { readFile } from "node:fs/promises";

const root = new URL("../../../", import.meta.url);
const port = Number(process.env.PORT ?? 4173);
const server = createServer(async (request, response) => {
    const url = new URL(request.url, `http://localhost:${port}`);
    try {
        if (url.pathname === "/mcp-ui-prototype/resource") {
            if (!["ui://globoticket/concerts", "ui://globoticket/checkout"].includes(url.searchParams.get("uri"))) {
                response.writeHead(404).end(); return;
            }
            const html = await readFile(new URL("catalog/MCP/Prototype/tool-ui.html", root), "utf8");
            response.writeHead(200, { "Content-Type": "application/json" }).end(JSON.stringify({
                contents: [{ uri: url.searchParams.get("uri"), mimeType: "text/html;profile=mcp-app", text: html }]
            }));
        } else if (url.pathname === "/mcp-ui-prototype/sandbox") {
            response.writeHead(200, {
                "Content-Type": "text/html",
                "Content-Security-Policy": "sandbox allow-scripts; default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; frame-src 'self' about:; connect-src 'none'; form-action 'none'; base-uri 'none'"
            }).end(await readFile(new URL("frontend/wwwroot/js/mcp-ui-prototype-sandbox.html", root)));
        } else if (["/preview.js", "/preview.css"].includes(url.pathname)) {
            const suffix = url.pathname.endsWith(".css") ? "css" : "js";
            response.writeHead(200, { "Content-Type": suffix === "css" ? "text/css" : "text/javascript" })
                .end(await readFile(new URL(`frontend/wwwroot/js/mcp-ui-prototype-preview.${suffix}`, root)));
        } else if (url.pathname === "/Chat" || url.pathname === "/") {
            response.writeHead(200, { "Content-Type": "text/html" }).end(`<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>GloboTicket MCP Apps prototype</title><link rel="stylesheet" href="/preview.css"><style>body{margin:0;background:#f5f6f1;color:#172624;font:15px system-ui}.preview-header{background:#172e25;color:white;padding:24px 8%;display:flex;justify-content:space-between}.preview-main{max-width:1020px;margin:25px auto 100px;padding:0 20px}.preview-toolbar{display:flex;gap:10px}.preview-toolbar button{padding:8px 15px;border:1px solid #a9b5a5;border-radius:6px;background:white;cursor:pointer}.preview-message{background:#e7ecdf;padding:16px;border-radius:10px}</style><body><div id="preview-root"></div><script src="/preview.js"></script></body></html>`);
        } else if (url.pathname === "/favicon.ico") {
            response.writeHead(204).end();
        } else {
            response.writeHead(404).end();
        }
    } catch (error) {
        console.error(error);
        response.writeHead(500).end(error.message);
    }
});
server.listen(port, "127.0.0.1", () => console.log(`MCP Apps fixture preview: http://localhost:${port}/Chat?variant=A`));
