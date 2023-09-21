import type { TrainingUpdateMessage } from "src/types";
import { DateUtils } from "src/utilities/DateUtils";
import { realtimeTrainingListener, trainingUpdateStore } from '../globalStore.js';

export class RealtimelineTools {
    public static createPositioningFunction(timespan: number = 5 * 60 * 1000) {
        return (time: Date) => {
            const timeToFract = (time: Date) => {
                const diffMs = Date.now().valueOf() - time.valueOf();
                return diffMs / timespan;
            }
            const timeCurve = (fract: number) => {
                const normalized = Math.min(1, Math.max(0, fract));
                const val = Math.pow(normalized, 0.4);
                return 1.0 - val;
            };
    
            const fract = timeToFract(time);
            if (fract > 1)
                return null;
            return `position: absolute; left: ${5 + 95 * timeCurve(fract)}%`;
        };
    }

    constructor(private keepAfterExpired: number = 7 * 60 * 1000) {
        trainingUpdateStore.subscribe(msgs => { 
            msgs.forEach(m => this.append(m));
        });
    }

    public get connectionSignal() {
        return realtimeTrainingListener.connectionSignal;
    }
    public get isConnected() {
        return realtimeTrainingListener.connected();
    }
    public async connect() {
        await realtimeTrainingListener.connect();
    }
    public async disconnect() {
        realtimeTrainingListener.disconnect();
    }
    public async toggleConnect() {
        if (realtimeTrainingListener.connected()) {
            realtimeTrainingListener.disconnect();
        } else {
            await realtimeTrainingListener.connect();
        }
            
    }

    public static testData(ids: number[] = []) {
        const now = Date.now() - 10 * 1000;
        const xx: TrainingUpdateMessage[] = [
            { TrainingId: 1, Username : "asd qwe", ReceivedTimestamp: new Date(Date.now()), ClientTimestamp: new Date(Date.now()), Data: [
                {offset: 0, className: "AnswerLogItem", correct: true, answer: "-3" },
                {offset: 0.5, className: "AnswerLogItem", correct: false, answer: "3" },
                {offset: 1, className: "AnswerLogItem", correct: false, answer: "3" },
                {offset: 2, className: "AnswerLogItem", correct: false, answer: "3" },
                {offset: 3, className: "NewProblemLogItem", problem_type: "NPALS", level: 3, problem_string: "9 - 12" },
                {offset: 4, className: "NewProblemLogItem", problem_type: "NPALS", level: 3, problem_string: "9 - 12" }
            ].map(o => ({ time: new Date(now - o.offset * 60 * 1000), ...o}))
            },
            { TrainingId: 2, Username : "lke qqq", ReceivedTimestamp: new Date(now), ClientTimestamp: new Date(Date.now()), Data: [
                {offset: 0.1, className: "NewPhaseLogItem", exercise: "rotation", training_day: 2 },
                {offset: 1.1, className: "NewPhaseLogItem", exercise: "grid", training_day: 2 },
                {offset: 4.9, className: "NewPhaseLogItem", exercise: "grid", training_day: 2 }
            ].map(o => ({ time: new Date(now - o.offset * 60 * 1000), ...o}))},
        ]
        if (ids.length) {
            return ids.map((o, i) => ({ ...xx[i % 2], TrainingId: o }))
        }
        return xx;
    }

    public onMount() {
        const interval = setInterval(() => {
            this.clearExpired();
        }, 100);
        return { interval: interval };
    }

    public clearExpired() {
        Object.keys(this.flatHistory).forEach(key => {
            const arr = this.flatHistory[parseInt(key, 10)];
            const indicesToRemove = arr
                .map((o, i) => ({ index: i, remove: DateUtils.getMsBetween(o.time, Date.now()) > this.keepAfterExpired }))
                .filter(o => o.remove)
                .map(o => o.index);
            indicesToRemove.reverse();
            indicesToRemove.forEach(o => arr.splice(o, 1));
        });
        // this.flatHistory[0] = [];
    }


    private flatHistory: { [key: number]: { time: Date, message: any, trainingId: number }[] } = {};
    private trainingInfos: { id: number, username: string }[] = [];
    
    public getData(filterIds?: number[] | null) {
        const filtered = filterIds != null
            ? this.trainingInfos.filter(o => filterIds.indexOf(o.id) >= 0)
            : this.trainingInfos;
        return filtered.map(o => ({ events: this.flatHistory[o.id], ...o }));
    }

    public append(m: TrainingUpdateMessage) {
        let forTraining = this.flatHistory[m.TrainingId];
        if (forTraining == null) {
            forTraining = [];
            this.flatHistory[m.TrainingId] = forTraining;
            this.trainingInfos.push({ id: m.TrainingId, username: m.Username });
        }

        const now = Date.now();
        // the "ping" messages are send instantly, so the diff should only be network/service latency
        const syncNetworkLatency = 30; //TODO: can we get more "real" values? Time from training client to server, server to teacher client?
        // e.g. client had 12:00:00 when it sent the message, 
        // server had 12:00:10 (actual time) when it received the massage
        // - events should be offset with 10 seconds.
        const timeDiffSync = DateUtils.getMsBetween(m.ClientTimestamp, m.ReceivedTimestamp) - syncNetworkLatency;
        // TODO: this client (teacher) might also have an incorrect time setting, we should consider that as well:
        const timeDiffListener = 0;

        // console.log("timeDiffSync", timeDiffSync, m.ClientTimestamp, m.ReceivedTimestamp);
        // // Because we can't trust client timestamps, adjust them for now
        // const clientTimestamps = m.Data.map(d => new Date(d["time"] || now).valueOf());
        // clientTimestamps.sort().reverse();
        // const latestClientTimestamp = clientTimestamps[0];
        // let timeDiffClientServer = latestClientTimestamp - now;
        // if (!!m.ReceivedTimestamp) {
        //     timeDiffClientServer = latestClientTimestamp - DateUtils.toDate(m.ReceivedTimestamp).valueOf();
        // }

        const times = forTraining.map(o => (o.message || {})["time"]).filter(o => o != null).map(o => <number>o);
        m.Data.forEach(d => {
            const unixTimestamp = d["time"] || now;
            if (times.indexOf(unixTimestamp) > 0) {
                // console.log("Identical", unixTimestamp);
                return; // if we already have items with exact same client timestamp, skip them
            }
            let timestamp = new Date(unixTimestamp - timeDiffSync);
            // console.log("pushing event", timestamp, d);
            forTraining.push({ time: timestamp, message: d, trainingId: m.TrainingId });
        });
    };
}