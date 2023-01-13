import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";
import { createEventDispatcher } from "svelte";

export interface Message {
    trainingId: number;
    username: string;
    events: any[];
    message?: string;
}

export class Realtime {

    private connection: HubConnection | null = null;

    // TODO: proper event system
    onReceived?: (msg: Message) => void;
    onConnected?: () => void;
    onDisconnected?: (err: Error | undefined) => void;

    // dispatch: any; // TODO: typing..= (type: EventKey, message: Message);
    constructor() {
        // this.dispatch = createEventDispatcher<Message>();
    }

    async connect(hostOrigin: string) {
        if (this.connection != null && this.hasDisconnectLikeState() === false)
            return;
        
        const url = `${hostOrigin}/realtime`;
        console.log("connecting...", url, this.hasDisconnectLikeState(), this.connection);

        this.connection = new HubConnectionBuilder()
            .withUrl(url)
            .configureLogging(LogLevel.Information)
            .withAutomaticReconnect()
            .build();

        this.connection.on("ReceiveMessage", (msg: Message | string) => {
            // console.log(`got msg`, msg);
            if (typeof msg === "string") msg = <Message>JSON.parse(msg);
            if (this.onReceived != null)
                this.onReceived(msg);
            // this.dispatch("received", <Message>{ trainingId: trn, message: msg })
        });

        this.connection.onreconnected(id => console.log("reconnected"));
        this.connection.onreconnecting(id => console.log("reconnecting"));
        this.connection.onclose(err => { if (!!this.onDisconnected) { this.onDisconnected(err); }});

        try { await this.connection.start(); }
        catch (err) { this.connection = null; throw err; }
        if (!!this.onConnected) { this.onConnected(); }
    }

    get state() { return this.connection?.state || HubConnectionState.Disconnected };

    get isConnected() { return this.connection == null ? false : this.connection.state === HubConnectionState.Connected; }

    private hasDisconnectLikeState() {
        return this.connection != null 
            && (this.connection.state === HubConnectionState.Disconnecting
            || this.connection.state === HubConnectionState.Disconnected);
    }

    disconnect() {
        if (this.connection != null && !this.hasDisconnectLikeState())
            this.connection.stop()
    }
}