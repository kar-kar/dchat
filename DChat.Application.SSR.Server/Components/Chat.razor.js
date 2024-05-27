class ChatController {
    constructor() {
        this.connectionIndicator = document.querySelector(".connection-indicator");
        if (!this.connectionIndicator)
            throw new Error("Connection indicator not found");

        this.messageTemplate = document.getElementById('message-template').innerHTML;
        if (!this.messageTemplate)
            throw new Error("Message template not found");

        this.messageListContainer = document.querySelector(".message-list-container");
        if (!this.messageListContainer)
            throw new Error("Message list container not found");

        this.messageList = document.querySelector(".message-list");
        if (!this.messageList)
            throw new Error("Message list not found");

        this.messageArea = document.querySelector(".new-message-area");
        if (!this.messageArea)
            throw new Error("Message area not found");

        this.sendMessageBtn = document.querySelector(".send-message-btn");
        if (!this.sendMessageBtn)
            throw new Error("Send message button not found");

        this.messageSentinel = document.querySelector(".message-sentinel");
        if (!this.messageSentinel)
            throw new Error("Message sentinel not found");

        this.connection = new signalR.HubConnectionBuilder()
            .configureLogging(signalR.LogLevel.Information)
            .withUrl("/chathub", { transport: signalR.HttpTransportType.WebSockets })
            .withAutomaticReconnect({ nextRetryDelayInMilliseconds: _ => 5000 })
            .build();

        this.connection.on("ReceiveMessage", msg => this.receive(msg));

        this.connection.onreconnecting(err => {
            const status = `Connection lost due to error "${err}". Reconnecting...`;
            console.log(status);
            this.setConnectionStatus(false, status);
        });

        this.connection.onreconnected(() => {
            console.log("reconnected");
            this.setConnectionStatus(true);
            this.reconnectToRoom();
        });

        this.connection.onclose(err => {
            const status = `Connection permanently lost due to error "${err}". Try reloading page.`;
            console.error(status);
            this.setConnectionStatus(false, status);
        });

        this.connection
            .start()
            .then(() => {
                console.log("connected");
                this.setConnectionStatus(true);
                this.joinRoom("world");
            })
            .catch(err => {
                const status = `Error connecting to server "${err}". Try reloading page.`;
                console.error(status);
                this.setConnectionStatus(false, status);
            });

        this.observer = new IntersectionObserver(entries => {
            if (entries[0].isIntersecting)
                this.loadHistory();
        }, {
            root: this.messageListContainer
        });

        this.observer.observe(this.messageSentinel);

        this.messageArea.addEventListener('input', () => {
            const lines = this.messageArea.value.split('\n').length;
            this.messageArea.setAttribute('rows', lines);
        });

        this.sendMessageBtn.addEventListener("click", () => this.send());

        this.messageArea.addEventListener('keypress', e => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                this.send();
            }
        });
    }

    setConnectionStatus(isConnected, status) {
        this.connectionIndicator.classList.toggle("connected", isConnected);
        this.connectionIndicator.setAttribute("title", status || "Connected");
    }

    joinRoom(room) {
        if (this.currentRoom == room)
            return;

        if (this.currentRoom)
            connection.invoke("Unsubscribe", room);

        this.messageList.innerHTML = "";
        this.minMsgId = null;
        this.maxMsgId = null;
        this.currentRoom = room;
        this.isHistoryLoaded = false;
        this.connection.invoke("Subscribe", room);
        this.loadHistory();
    }

    reconnectToRoom() {
        if (!this.currentRoom)
            return;

        this.connection.invoke("Subscribe", this.currentRoom);
        this.loadMissingMessages();
    }

    loadHistory() {
        if (!this.currentRoom || this.isHistoryLoaded || this.connection.state != signalR.HubConnectionState.Connected)
            return;

        console.log("loading history");
        let cnt = 0;

        this.connection
            .stream("GetMessagesBeforeId", this.currentRoom, this.minMsgId, 100)
            .subscribe({
                next: item => {
                    cnt++;
                    this.receive(item);
                },
                complete: () => {
                    if (cnt < 100) {
                        console.log("entire history is loaded");
                        this.isHistoryLoaded = true;
                    }
                },
                error: (err) => {
                    console.error(err.toString());
                },
            });
    }

    loadMissingMessages() {
        if (!this.currentRoom || !this.maxMsgId || this.connection.state != signalR.HubConnectionState.Connected)
            return;

        console.log("loading missing messages after reconnecting");

        this.connection
            .stream("GetMessagesAfterId", this.currentRoom, this.maxMsgId)
            .subscribe({
                next: item => {
                    this.receive(item);
                },
                error: (err) => {
                    console.error(err.toString());
                },
            });
    }

    send() {
        console.log("sending message");
        const text = this.messageArea.value;

        if (!text)
            return;

        this.connection.invoke("SendMessage", {
            room: this.currentRoom,
            text
        });

        this.messageArea.value = "";
        this.messageArea.setAttribute('rows', 1);
    }

    receive(msg) {
        console.log("message received");

        if (msg.id >= (this.minMsgId || Infinity) && msg.id <= (this.maxMsgId || -Infinity))
            return;

        const prevElementId = this.maxMsgId && msg.id > this.maxMsgId ? "msg-" + this.maxMsgId : null;
        const nextElementId = this.minMsgId && msg.id < this.minMsgId ? "msg-" + this.minMsgId : null;
        this.maxMsgId = Math.max(this.maxMsgId || -Infinity, msg.id);
        this.minMsgId = Math.min(this.minMsgId || Infinity, msg.id);

        const newElement = document.createElement("div");
        newElement.setAttribute("ssr-chat-scope", "");
        newElement.id = "msg-" + msg.id;
        newElement.className = "message";
        newElement.innerHTML = Sqrl.render(this.messageTemplate, msg);
        let added = false;

        if (prevElementId) {
            const prevElement = document.getElementById(prevElementId);

            if (prevElement) {
                prevElement.after(newElement);
                added = true;
            }
        }

        if (!added && nextElementId) {
            const nextElement = document.getElementById(nextElementId);

            if (nextElement) {
                nextElement.before(newElement);
                return;
            }
        }

        if (!added) {
            this.messageList.appendChild(newElement);
        }
    }
}
