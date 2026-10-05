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
                return DateUtils.getTimeDiffCategory(now - new Date(created).valueOf()).msRounded;
            });
            starts = [...grouped]
                .sort((a, b) => a[0] - b[0])
                .map(o => ({ startPeriod: DateUtils.getTimeDiffCategory(o[0]).name, count: o[1].length }));
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