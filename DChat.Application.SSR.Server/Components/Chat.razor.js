class ChatController {
    constructor(currentUserId, defaultRoom) {
        this.currentUserId = currentUserId;
        this.defaultRoom = defaultRoom;
        this.findElements();
        this.connection = this.connect();
        this.observer = this.createScrollObserver();
        this.subscribeElements();
    }

    findElements() {
        this.roomBtn = document.getElementById("room-btn");
        if (!this.roomBtn)
            throw new Error("Room button not found");

        this.changeRoomForm = document.getElementById("change-room-form");
        if (!this.changeRoomForm)
            throw new Error("Change room form not found");

        this.roomNameInput = document.getElementById("room-name-input");
        if (!this.roomNameInput)
            throw new Error("Room name input not found");

        this.connectionIndicator = document.getElementById("connection-indicator");
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
    }

    subscribeElements() {
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

        this.changeRoomForm.addEventListener("submit", e => {
            e.preventDefault();
            const room = this.roomNameInput.value;

            if (room)
                this.joinRoom(room);
        });
    }

    connect() {
        const connection = new signalR.HubConnectionBuilder()
            .configureLogging(signalR.LogLevel.Information)
            .withUrl("/chathub", { transport: signalR.HttpTransportType.WebSockets })
            .withAutomaticReconnect({ nextRetryDelayInMilliseconds: _ => 5000 })
            .build();

        connection.on("ReceiveMessage", msg => this.receive(msg));

        connection.onreconnecting(err => {
            const status = `Connection lost due to error "${err}". Reconnecting...`;
            console.log(status);
            this.setConnectionStatus(false, status);
        });

        connection.onreconnected(() => {
            console.log("reconnected");
            this.setConnectionStatus(true);
            this.reconnectToRoom();
        });

        connection.onclose(err => {
            const status = `Connection permanently lost due to error "${err}". Try reloading page.`;
            console.error(status);
            this.setConnectionStatus(false, status);
        });

        connection
            .start()
            .then(() => {
                console.log("connected");
                this.setConnectionStatus(true);
                this.joinRoom(this.defaultRoom);
            })
            .catch(err => {
                const status = `Error connecting to server "${err}". Try reloading page.`;
                console.error(status);
                this.setConnectionStatus(false, status);
            });

        return connection;
    }

    createScrollObserver() {
        const observer = new IntersectionObserver(entries => {
            if (entries[0].isIntersecting)
                this.loadHistory();
        }, {
            root: this.messageListContainer
        });

        observer.observe(this.messageSentinel);
        return observer;
    }

    setConnectionStatus(isConnected, status) {
        this.connectionIndicator.classList.toggle("connected", isConnected);
        this.connectionIndicator.setAttribute("title", status || "Connected");
    }

    joinRoom(room) {
        if (this.currentRoom == room)
            return;

        if (this.currentRoom)
            this.connection.invoke("Unsubscribe", room);

        this.messageList.innerHTML = "";
        this.minMsgId = null;
        this.maxMsgId = null;
        this.currentRoom = room;
        this.roomBtn.innerHTML = this.currentRoom;
        this.isHistoryLoaded = false;

        if (this.currentRoom) {
            this.connection.invoke("SetDefaultRoom", this.currentRoom);
            this.connection.invoke("Subscribe", this.currentRoom);
            this.loadHistory();
        }
    }

    reconnectToRoom() {
        if (!this.currentRoom)
            return;

        this.connection.invoke("Subscribe", this.currentRoom);

        if (this.maxMsgId)
            this.loadMissingMessages();
        else
            this.loadHistory();
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

        if (msg.senderId == this.currentUserId)
            newElement.classList.add("own");

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
