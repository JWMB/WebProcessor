<script lang="ts">
	import { DateUtils } from "src/utilities/DateUtils";
    import AllTrainingsOverTimeChart from "../../../components/allTrainingsOverTimeChart.svelte";
	import { getApi } from '../../../globalStore';
	import DateInput from "src/components/DateInput.svelte";
	import type { TrainingSummaryDto } from "src/apiClient";

    let stackLeft = false;
    let startDate = DateUtils.addDays(Date.now(), -30 * 6);

    let summaries: TrainingSummaryDto[] | undefined = undefined;
    async function loadData() {
        summaries = undefined;
        summaries = await getApi()?.trainings.getAllSummaries();
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
