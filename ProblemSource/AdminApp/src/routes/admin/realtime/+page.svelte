<script lang="ts">
	import type { TrainingUpdateMessage } from 'src/types.js';
	import { trainingUpdateStore } from '../../../globalStore.js';
	import { DateUtils } from 'src/utilities/DateUtils.js';
	import { onMount } from 'svelte';
	import Realtimeline from 'src/components/realtimeline.svelte';

    const flatHistory: { [key:number]: {time: Date, message: any, trainingId: number}[]} = {};
    const trainingInfos: { id: number, username: string}[] = [];

    const cutoff = 5 * 60 * 1000;

    const timeToFract = (time: Date) => {
        const diffMs = Date.now().valueOf() - time.valueOf();
        const normalized = Math.min(1, Math.max(0, diffMs / cutoff));

        const val = Math.pow(normalized, 0.4);
        return 1.0 - val;
    };
    const getPositioning = (time: Date) => {
        return `position: absolute; left: ${5 + 95 * timeToFract(time)}%`;
        // return `position: relative; left: ${500 * timeToFract(time)}px`;
    };

    const messageToHistory = (m: TrainingUpdateMessage) => {
        if (flatHistory[m.TrainingId] == null) {
            flatHistory[m.TrainingId] = [];
            trainingInfos.push({ id: m.TrainingId, username: m.Username });
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
            flatHistory[m.TrainingId].push({ time: timestamp, message: d, trainingId: m.TrainingId });
        });
    };

    // { // for testing:
    //     const now = Date.now() - 10 * 1000;
    //     const xx: TrainingUpdateMessage[] = [
    //         { TrainingId: 1, Username : "asd qwe", ReceivedTimestamp: new Date(Date.now()), ClientTimestamp: new Date(Date.now()), Data: [
    //             {offset: 0, className: "AnswerLogItem", correct: true, answer: "-3" },
    //             {offset: 0.5, className: "AnswerLogItem", correct: false, answer: "3" },
    //             {offset: 1, className: "AnswerLogItem", correct: false, answer: "3" },
    //             {offset: 2, className: "AnswerLogItem", correct: false, answer: "3" },
    //             {offset: 3, className: "NewProblemLogItem", problem_type: "NPALS", level: 3, problem_string: "9 - 12" },
    //             {offset: 4, className: "NewProblemLogItem", problem_type: "NPALS", level: 3, problem_string: "9 - 12" }
    //         ].map(o => ({ time: new Date(now - o.offset * 60 * 1000), ...o}))
    //         },
    //         { TrainingId: 2, Username : "lke qqq", ReceivedTimestamp: new Date(now), ClientTimestamp: new Date(Date.now()), Data: [
    //             {offset: 0.1, className: "hello" },
    //             {offset: 4.9, className: "hello" }
    //         ].map(o => ({ time: new Date(now - o.offset * 60 * 1000), ...o}))},
    //     ]
    //     xx.forEach(o => messageToHistory(o));
    // }

    trainingUpdateStore.subscribe(msgs => { 
        // console.log("incoming", msgs);
        msgs.forEach(m => messageToHistory(m));
    });


    onMount(() => {
        const interval = setInterval(() => { 
            Object.keys(flatHistory).forEach(key => {
                const arr = flatHistory[parseInt(key, 10)];
                const indicesToRemove = arr
                    .map((o, i) => ({ index: i, remove: DateUtils.getMsBetween(o.time, Date.now()) > cutoff}))
                    .filter(o => o.remove)
                    .map(o => o.index);
                indicesToRemove.reverse();
                indicesToRemove.forEach(o => arr.splice(o, 1));
            });
            flatHistory[0] = [];
        }, 100);
        return () => clearInterval(interval);
    })
</script>

<div>
    <table>
        <tr>
            <th>User</th>
            <!-- <th style="width: 800px;"> -->
            <th>
                {#each [0, 0.5, 1, 2, 3, 4, 5] as minutes}
                    <span style="{getPositioning(new Date(Date.now().valueOf() - minutes * 60 * 1000))}">{minutes}</span>
                {/each}
            </th>
        </tr>
        {#each trainingInfos as info}
        <tr>
            <td>{info.username}</td>
            <td>
                <Realtimeline history={flatHistory[info.id]} getPositioning={getPositioning}></Realtimeline>
            </td>
        </tr>
    {/each}
    </table>
</div>