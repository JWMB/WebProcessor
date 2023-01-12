import { HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";

export class Realtime {
    connect(hostOrigin: string) {
        const url = `${hostOrigin}/realtime`;
        const connection = new HubConnectionBuilder()
            .withUrl(url)
            .configureLogging(LogLevel.Information)
            .withAutomaticReconnect()
            .build();

        console.log("connecting...", url);
        connection.on("ReceiveMessage", (trn, msg) => {
            console.log(`got msg`, trn, msg);
            // dispatch('participants-changed', participants);
        });
        connection.start();
    }
}