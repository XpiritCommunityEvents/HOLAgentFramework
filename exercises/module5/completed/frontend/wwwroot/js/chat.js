"use strict";

const conversationStorageKey = "globoticket.chat.conversation";
let conversationId = sessionStorage.getItem(conversationStorageKey);
if (!conversationId) {
    conversationId = crypto.randomUUID();
    sessionStorage.setItem(conversationStorageKey, conversationId);
}

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .build();

const sendButton = document.getElementById("sendButton");
const messageInput = document.getElementById("messageInput");
const messagesList = document.getElementById("messagesList");
let assistantMessage = null;

sendButton.disabled = true;
messageInput.focus();

function scrollToBottom() {
    const messages = document.querySelector(".chat-messages");
    messages.scrollTop = messages.scrollHeight;
    window.scrollTo(0, document.body.scrollHeight);
}

function addMessage(text, className) {
    const item = document.createElement("li");
    item.className = className || "";
    item.appendChild(document.createTextNode(text));
    messagesList.appendChild(item);
    scrollToBottom();
    return item;
}

function setReady() {
    const connected = connection.state === signalR.HubConnectionState.Connected;
    sendButton.disabled = !connected;
    messageInput.disabled = !connected;
}

connection.on("NewResponse", function () {
    assistantMessage = addMessage("", "assistant-message");
});

connection.on("ReceiveMessagePart", function (message) {
    assistantMessage ||= addMessage("", "assistant-message");
    assistantMessage.appendChild(document.createTextNode(message));
    scrollToBottom();
});

connection.on("ResponseDone", function () {
    assistantMessage = null;
    setReady();
    scrollToBottom();
    messageInput.focus();
});

connection.start()
    .then(setReady)
    .catch(error => addMessage(`Chat connection failed: ${error}`, "chat-error"));

sendButton.addEventListener("click", async function (event) {
    event.preventDefault();
    const message = messageInput.value.trim();
    if (!message || sendButton.disabled) {
        return;
    }

    addMessage(`You: ${message}`, "user-message");
    messageInput.value = "";
    sendButton.disabled = true;
    messageInput.disabled = true;

    try {
        await connection.invoke("SendMessage", conversationId, message);
    } catch (error) {
        addMessage(`Message could not be sent: ${error}`, "chat-error");
        setReady();
    }
});

messageInput.addEventListener("keydown", function (event) {
    if (event.key === "Enter") {
        event.preventDefault();
        sendButton.click();
    }
});
