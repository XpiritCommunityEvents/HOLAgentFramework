"use strict";

const conversationStorageKey = "globoticket.chat.signalr.conversation";
const chatForm = document.getElementById("signalrChatForm");
const sendButton = document.getElementById("signalrSendButton");
const messageInput = document.getElementById("signalrMessageInput");
const messagesList = document.getElementById("signalrMessagesList");
const emptyState = document.getElementById("signalrEmptyState");
const chatThread = document.getElementById("signalrChatThread");
const connectionStatus = document.getElementById("signalrConnectionStatus");
const statusContainer = connectionStatus.closest(".chat-status");

const state = {
    ready: false,
    busy: false,
    error: null
};

function getOrCreateConversationId() {
    let id = sessionStorage.getItem(conversationStorageKey);
    if (!id) {
        id = crypto.randomUUID();
        sessionStorage.setItem(conversationStorageKey, id);
    }
    return id;
}

const conversationId = getOrCreateConversationId();
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .withAutomaticReconnect()
    .build();

function scrollToBottom() {
    chatThread.scrollTop = chatThread.scrollHeight;
}

function addMessage(role, content, label) {
    const item = document.createElement("li");
    item.className = `chat-message is-${role}`;

    const messageLabel = document.createElement("span");
    messageLabel.className = "chat-message-label";
    messageLabel.textContent = label ?? (role === "user" ? "You" : "SignalR");

    const messageContent = document.createElement("div");
    messageContent.className = "chat-message-content";
    messageContent.textContent = content;

    item.append(messageLabel, messageContent);
    messagesList.appendChild(item);
    emptyState.hidden = true;
    scrollToBottom();
    return messageContent;
}

function setStatus(kind, text) {
    statusContainer.classList.toggle("is-ready", kind === "ready");
    statusContainer.classList.toggle("is-error", kind === "error");
    connectionStatus.textContent = text;
}

function updateControls() {
    const enabled = state.ready && !state.busy;
    sendButton.disabled = !enabled;
    messageInput.disabled = !enabled;

    if (state.error) {
        setStatus("error", state.error);
    } else if (state.busy) {
        setStatus("busy", "Agent is responding");
    } else if (state.ready) {
        setStatus("ready", "WebSocket hub ready");
    } else {
        setStatus("busy", "Connecting to hub");
    }
}

let assistantMessageContent = null;

connection.on("NewResponse", () => {
    assistantMessageContent = addMessage("assistant", "", "SignalR");
});

connection.on("ReceiveMessagePart", messagePart => {
    assistantMessageContent ??= addMessage("assistant", "", "SignalR");
    assistantMessageContent.textContent += messagePart;
    scrollToBottom();
});

connection.on("ResponseDone", () => {
    assistantMessageContent = null;
    state.busy = false;
    updateControls();
    messageInput.focus();
});

connection.onreconnecting(() => {
    state.ready = false;
    state.error = null;
    updateControls();
});

connection.onreconnected(() => {
    state.ready = true;
    state.error = null;
    updateControls();
});

connection.onclose(() => {
    state.ready = false;
    state.error = "Connection closed";
    updateControls();
});

chatForm.addEventListener("submit", async event => {
    event.preventDefault();
    const message = messageInput.value.trim();
    if (!message || sendButton.disabled) {
        return;
    }

    messageInput.value = "";
    state.busy = true;
    addMessage("user", message);
    updateControls();

    try {
        await connection.invoke("SendMessage", conversationId, message);
    } catch (error) {
        addMessage("error", `Message could not be sent: ${error}`, "SignalR error");
        state.busy = false;
        updateControls();
    }
});

updateControls();
connection.start()
    .then(() => {
        state.ready = true;
        state.error = null;
        updateControls();
        messageInput.focus();
    })
    .catch(error => {
        state.error = "Connection failed";
        addMessage("error", `Chat connection failed: ${error}`, "SignalR error");
        updateControls();
    });
