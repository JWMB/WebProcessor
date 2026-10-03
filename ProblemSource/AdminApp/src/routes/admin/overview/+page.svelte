<script lang="ts">
	import { DateUtils } from "../../../utilities/DateUtils";
    import AllTrainingsOverTimeChart from "../../../components/allTrainingsOverTimeChart.svelte";
	import { getApi } from '../../../globalStore';
	import DateInput from "src/components/DateInput.svelte";
	import type { TrainingSummaryDto } from "../../../apiClient";

    let stackLeft = false;
    let startDate = DateUtils.addDays(Date.now(), -30 * 6);

    let summaries: TrainingSummaryDto[] | undefined = undefined;
    let starts: {startPeriod: string, count: number}[] | undefined;

    async function loadData() {
        summaries = undefined;
        starts = undefined;
        summaries = await getApi()?.trainings.getAllSummaries();

        const now = Date.now();
        if (summaries?.length) {
            const grouped = Map.groupBy(summaries, ({ created }) => {
            //const grouped = Object.groupBy(summaries, ({ created }) => {
                const date = new Date(created);
                const days = (now - date.valueOf()) / 1000 / 60 / 60 / 24;
                if (days > 30) return "DOlder";
                if (days > 7) return "CLast week";
                if (days > 1) return `B${date.getDate()}/${date.getMonth() + 1}`;
                return `AToday ${date.getHours()}`
            });
            starts = [...grouped]
                .sort((a, b) => a[0].localeCompare(b[0]))
                .map(o => ({ startPeriod: o[0].substring(1), count: o[1].length }));
        }
    } 
</script>

<div>Stack left: <input type="checkbox" bind:checked={stackLeft}></div>
<DateInput bind:date={startDate}></DateInput>
<button on:click={loadData}>Load</button>
stackLeft? {stackLeft}
{#if summaries}
total count: {summaries?.length}
<AllTrainingsOverTimeChart data={summaries} stackToLeft={stackLeft} startDate={startDate} ></AllTrainingsOverTimeChart>
{/if}
{#if starts}
{#each starts as start}
<div>{start.startPeriod}: {start.count}</div>
{/each}
{/if}