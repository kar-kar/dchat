class ChatController {
    constructor(dotNetObj) {
        this.dotNetObj = dotNetObj;
        if (!this.dotNetObj)
            throw new Error("DotNet object is required");

        this.findElements();
        this.observer = this.createScrollObserver();
    }

    findElements() {
        this.messageList = document.querySelector(".message-list");
        if (!this.messageList)
            throw new Error("Message list not found");

        this.messageSentinel = document.querySelector(".message-sentinel");
        if (!this.messageSentinel)
            throw new Error("Message sentinel not found");
    }

    createScrollObserver() {
        const observer = new IntersectionObserver(entries => {
            if (entries[0].isIntersecting)
                this.dotNetObj.invokeMethodAsync("OnMessageSentinelVisible");
        }, {
            root: this.messageListContainer
        });

        observer.observe(this.messageSentinel);
        return observer;
    }
}