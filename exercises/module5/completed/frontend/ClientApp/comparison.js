"use strict";

const transportStorageKey = "globoticket.chat.transport";
const transportButtons = [...document.querySelectorAll("[data-transport]")];
const workspaces = [...document.querySelectorAll("[data-workspace]")];
let agUiClientPromise = null;

function loadAgUiClient() {
    if (agUiClientPromise) {
        return agUiClientPromise;
    }

    agUiClientPromise = new Promise((resolve, reject) => {
        const stylesheet = document.createElement("link");
        stylesheet.rel = "stylesheet";
        stylesheet.href = "/js/agui-chat.css";
        document.head.appendChild(stylesheet);

        const script = document.createElement("script");
        script.src = "/js/agui-chat.js";
        script.onload = resolve;
        script.onerror = () => reject(new Error("The AG-UI client bundle could not be loaded."));
        document.body.appendChild(script);
    });

    agUiClientPromise.catch(error => {
        const root = document.getElementById("agUiChatRoot");
        if (root) {
            root.textContent = error.message;
            root.classList.add("is-error");
        }
    });

    return agUiClientPromise;
}

function selectTransport(transport) {
    sessionStorage.setItem(transportStorageKey, transport);

    transportButtons.forEach(button => {
        const selected = button.dataset.transport === transport;
        button.classList.toggle("is-active", selected);
        button.setAttribute("aria-selected", selected.toString());
        button.tabIndex = selected ? 0 : -1;
    });

    workspaces.forEach(workspace => {
        workspace.hidden = workspace.dataset.workspace !== transport;
    });

    if (transport === "agui") {
        void loadAgUiClient();
    }

    document.dispatchEvent(new CustomEvent("chat:transport-selected", {
        detail: { transport }
    }));
}

transportButtons.forEach((button, index) => {
    button.addEventListener("click", () => selectTransport(button.dataset.transport));
    button.addEventListener("keydown", event => {
        if (event.key !== "ArrowLeft" && event.key !== "ArrowRight") {
            return;
        }

        event.preventDefault();
        const offset = event.key === "ArrowRight" ? 1 : -1;
        const nextIndex = (index + offset + transportButtons.length) % transportButtons.length;
        const nextButton = transportButtons[nextIndex];
        selectTransport(nextButton.dataset.transport);
        nextButton.focus();
    });
});

const initialTransport = sessionStorage.getItem(transportStorageKey) === "agui"
    ? "agui"
    : "signalr";

selectTransport(initialTransport);
