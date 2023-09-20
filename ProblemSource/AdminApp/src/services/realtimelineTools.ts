import type { TrainingUpdateMessage } from "src/types";
import { DateUtils } from "src/utilities/DateUtils";
import { trainingUpdateStore } from '../globalStore.js';

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
        if (this.flatHistory[m.TrainingId] == null) {
            this.flatHistory[m.TrainingId] = [];
            this.trainingInfos.push({ id: m.TrainingId, username: m.Username });
        }

        const now = Date.now();
        // Because we can't trust client timestamps, adjust them for now
        const clientTimestamps = m.Data.map(d => new Date(d["time"] || now).valueOf());
        clientTimestamps.sort().reverse();
        const latestClientTimestamp = clientTimestamps[0];
        let offset = latestClientTimestamp - now;
        if (!!m.ReceivedTimestamp) {
            offset = latestClientTimestamp - DateUtils.toDate(m.ReceivedTimestamp).valueOf();
        }

        m.Data.forEach(d => {
            const itemTimestamp = new Date(d["time"] || now);
            let timestamp = new Date(itemTimestamp.valueOf() - offset);
            this.flatHistory[m.TrainingId].push({ time: timestamp, message: d, trainingId: m.TrainingId });
        });
    };
}