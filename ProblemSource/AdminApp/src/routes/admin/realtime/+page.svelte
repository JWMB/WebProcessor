<script lang="ts">
	import type { TrainingUpdateMessage } from 'src/types.js';
	import { trainingUpdateStore } from '../../../globalStore.js';
	import { DateUtils } from 'src/utilities/DateUtils.js';
	import { onMount } from 'svelte';

    const flatHistory: { [key:number]: {time: Date, message: any, trainingId: number}[]} = {};
    const trainingInfos: { id: number, username: string}[] = [];

    const cutoff = 5 * 60 * 1000;

    const messageToHistory = (m: TrainingUpdateMessage) => {
        if (flatHistory[m.TrainingId] == null) {
            flatHistory[m.TrainingId] = [];
            trainingInfos.push({ id: m.TrainingId, username: m.Username });
        }

        m.Data.forEach(d => {
            flatHistory[m.TrainingId].push({ time: new Date(d["time"] || Date.now()), message: d, trainingId: m.TrainingId });
        });
    };

    {
        const now = Date.now().valueOf();

        const xx: TrainingUpdateMessage[] = [
            { TrainingId: 1, Username : "asd qwe", Data: [
                {offset: 0, class: "AnswerLogItem", correct: true, answer: "-3" },
                {offset: 0.5, class: "AnswerLogItem", correct: false, answer: "3" },
                {offset: 1, class: "AnswerLogItem", correct: false, answer: "3" },
                {offset: 2, class: "AnswerLogItem", correct: false, answer: "3" },
                {offset: 3, class: "NewProblemLogItem", problem_type: "NPALS", level: 3, problem_string: "9 - 12" },
                {offset: 4, class: "NewProblemLogItem", problem_type: "NPALS", level: 3, problem_string: "9 - 12" }
            ].map(o => ({ time: new Date(now - o.offset * 60 * 1000), ...o}))
            },
            { TrainingId: 2, Username : "lke qqq", Data: [
                {offset: 0.1, class: "hello" },
                {offset: 4.9, class: "hello" }
            ].map(o => ({ time: new Date(now - o.offset * 60 * 1000), ...o}))},
        ]

        xx.forEach(o => messageToHistory(o));
    }

    trainingUpdateStore.subscribe(msgs => { 
        console.log("incoming", msgs);
        msgs.forEach(m => messageToHistory(m));
    });

    const timeToFract = (time: Date) => {
        const diffMs = Date.now().valueOf() - time.valueOf();
        const normalized = Math.min(1, Math.max(0, diffMs / cutoff));

        const val = Math.pow(normalized, 0.4);
        return 1.0 - val;
    };

    const getColor = (item: any) =>{
        const msg = item.message;
        if (msg != null) {
            const className = msg["class"];
            if (className == "AnswerLogItem") {
                return msg["correct"] ? "#00BB00" : "#EE0000";
            } else if (className == "NewProblemLogItem") {
                return "#0000FF";
            }
        }
        return "#000000";
    }
    const getSimpleTitle = (item: any) => {
        if (item == null) return "";

        const msg = item.message;
        if (msg != null) {
            const className = msg["class"];
            if (className == "AnswerLogItem") {
                return `${msg["answer"] || ""}`;

            } else if (className == "NewProblemLogItem") {
                return `${msg["problem_string"]} ${msg["problem_type"] || ""} level ${msg["level"]}`;

            } else if (className == "NewPhaseLogItem") {
                return `${msg["exercise"]} (day ${msg["training_day"]})`;
            }
        }
        return JSON.stringify(msg);
    };

    const getTitle = (item: any) => {
        const arr = item.trainingId == null ? null : flatHistory[item.trainingId];
        const getPreviousOfType = (t: string) => {
            if (arr == null) return null;
            const tmp = arr
                .filter(o => (o.message || {})["class"] == t)
                .filter(o => o.time < item.time)
                .toSorted((a, b) => a.time.valueOf() - b.time.valueOf());
            return tmp.length ? tmp[tmp.length - 1] : null
        };

        const msg = item.message;
        if (msg != null) {
            const className = msg["class"];
            if (className == "AnswerLogItem") {
                return `Answer: ${getSimpleTitle(item)} (${getSimpleTitle(getPreviousOfType("NewProblemLogItem"))})`;

            } else if (className == "NewProblemLogItem") {
                return `Question: ${getSimpleTitle(item)} (${getSimpleTitle(getPreviousOfType("NewPhaseLogItem"))})`;

            } else if (className == "NewPhaseLogItem") {
                return `${getSimpleTitle(item)}`;
            }
        }
        return getSimpleTitle(item);
    };

    const getPositioning = (time: Date) => {
        return `position: absolute; left: ${5 + 95 * timeToFract(time)}%`;
        // return `position: relative; left: ${500 * timeToFract(time)}px`;
    };

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

<style>
    /* Tooltip container */
    .tooltip {
      position: relative;
      display: inline-block;
      border-bottom: 1px dotted black; /* If you want dots under the hoverable text */
    }
    
    /* Tooltip text */
    .tooltip .tooltiptext {
      visibility: hidden;
      width: 120px;
      background-color: black;
      color: #fff;
      text-align: center;
      padding: 5px 0;
      border-radius: 6px;
     
      /* Position the tooltip text - see examples below! */
      position: absolute;
      z-index: 1;
    }
    
    /* Show the tooltip text when you mouse over the tooltip container */
    .tooltip:hover .tooltiptext {
      visibility: visible;
    }
</style>

<div>
    <table>
        <tr>
            <th>User</th>
            <!-- <th style="width: 800px;"> -->
            <th>
                {#each [0, 0.5, 1, 2, 3, 4, 5] as minutes}
                    <!-- <div style="position: absolute;  left: {100 * timeToFract(new Date(Date.now().valueOf() - minutes * 60 * 1000))}%;">{minutes}</div> -->
                    <span style="{getPositioning(new Date(Date.now().valueOf() - minutes * 60 * 1000))}">{minutes}</span>
                {/each}
            </th>
        </tr>
        {#each trainingInfos as info}
        <tr>
            <td>{info.username}</td>
            <td>
                {#each flatHistory[info.id] as item}
                    <!-- <span title="{getTitle(item)}" style="color: {getColor(item)}; position: absolute; left: {100 * timeToFract(item.time)}%;">X</span> -->
                    <span class="tooltip" style="color: {getColor(item)}; {getPositioning(item.time)}">
                        X
                        <span class="tooltiptext">{getTitle(item)}</span>
                    </span>
                    <!-- <span title="{getTitle(item)}" style="color: {getColor(item)}; position: absolute; left: 0%; transform: translateX({100 * timeToFract(item.time)}%);">X</span> -->
                {/each}
            </td>
        </tr>
    {/each}
    </table>
    <!-- <div>
        {#each [0, 0.5, 1, 2, 3, 4, 5] as minutes}
            <div style="position: absolute;  left: {800 * timeToFract(new Date(Date.now().valueOf() - minutes * 60 * 1000))}px;">{minutes}</div>
        {/each}
    </div>
    <br/>
    {#each trainingInfos as info}
        <div>
            {info.username}
            {#each flatHistory[info.id] as item}
                <span title="{getTitle(item)}" style="color: {getColor(item)}; position: absolute; left: {800 * timeToFract(item.time)}px;">X</span>
            {/each}
        </div>
    {/each} -->
</div>