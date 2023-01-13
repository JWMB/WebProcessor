import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";
import { createEventDispatcher } from "svelte";

export interface Message {
    trainingId: number;
    message: string;
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
        this.connection = new HubConnectionBuilder()
            .withUrl(url)
            .configureLogging(LogLevel.Information)
            .withAutomaticReconnect()
            .build();

        console.log("connecting...", url);
        this.connection.on("ReceiveMessage", (trn, msg: string) => {
            console.log(`got msg`, trn, msg);
            if (this.onReceived != null)
                this.onReceived( <Message>{ trainingId: trn, message: msg });
            // this.dispatch("received", <Message>{ trainingId: trn, message: msg })
        });

        this.connection.onreconnected(id => console.log("reconnected"));
        this.connection.onreconnecting(id => console.log("reconnecting"));
        this.connection.onclose(err => { if (!!this.onDisconnected) { this.onDisconnected(err); }});

        await this.connection.start();
        if (!!this.onConnected) { this.onConnected(); }
    }

    get state() { return this.connection?.state || HubConnectionState.Disconnected };

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