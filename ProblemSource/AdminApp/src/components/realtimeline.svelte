<script lang="ts">
    export let history: {time: Date, message: any }[] = [];
    export let getPositioning: (date: Date) => string;


    const getColor = (item: any) =>{
        const msg = item.message;
        if (msg != null) {
            const className = msg["className"];
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
            const className = msg["className"];
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
        //const arr = item.trainingId == null ? null : flatHistory[item.trainingId];
        const getPreviousOfType = (t: string) => {
            if (history == null) return null;
            const tmp = history
                .filter(o => (o.message || {})["className"] == t)
                .filter(o => o.time < item.time)
                .toSorted((a, b) => a.time.valueOf() - b.time.valueOf());
            return tmp.length ? tmp[tmp.length - 1] : null
        };

        const msg = item.message;
        if (msg != null) {
            const className = msg["className"];
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
</script>

<style>
    .tooltip {
      position: relative;
      display: inline-block;
      border-bottom: 1px dotted black; /* If you want dots under the hoverable text */
    }
    
    .tooltip .tooltiptext {
      visibility: hidden;
      width: 120px;
      background-color: black;
      color: #fff;
      text-align: center;
      padding: 5px 0;
      border-radius: 6px;
     
      position: absolute;
      z-index: 1;
    }
    
    .tooltip:hover .tooltiptext {
      visibility: visible;
    }
</style>

{#each history as item}
<span class="tooltip" style="color: {getColor(item)}; {getPositioning(item.time)}">
    X
    <span class="tooltiptext">{getTitle(item)}</span>
</span>
{/each}
