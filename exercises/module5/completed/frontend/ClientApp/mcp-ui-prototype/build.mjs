import { build } from "esbuild";
import { readFile, writeFile } from "node:fs/promises";

const here = new URL("./", import.meta.url);
const root = new URL("../../../", here);
const bundled = async file => (await build({
    entryPoints: [new URL(file, here).pathname], bundle: true, write: false,
    minify: true, format: "iife", target: "es2022",
    define: { "process.env.NODE_ENV": JSON.stringify("development") }
})).outputFiles[0].text.replaceAll("</script", "<\\/script");
const css = await readFile(new URL("guest.css", here), "utf8");
const html = `<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>GloboTicket MCP Apps prototype</title><style>${css}</style><body><main id="app"><p>Waiting for an MCP tool result...</p></main><script>${await bundled("guest.js")}</script></body></html>`;
await writeFile(new URL("catalog/MCP/Prototype/tool-ui.html", root), html);
await writeFile(new URL("frontend/wwwroot/js/mcp-ui-prototype-sandbox.html", root),
    `<!doctype html><html><meta charset="utf-8"><title>MCP Apps opaque sandbox</title><style>html,body{margin:0;width:100%;height:100%;overflow:hidden}</style><body><script>${await bundled("sandbox.js")}</script></body></html>`);
await build({
    entryPoints: [new URL("preview.jsx", here).pathname], bundle: true, minify: true,
    format: "iife", target: "es2022", outfile: new URL("frontend/wwwroot/js/mcp-ui-prototype-preview.js", root).pathname,
    define: { "process.env.NODE_ENV": JSON.stringify("development") }
});
