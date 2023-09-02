<script lang="ts">
	import { DateUtils } from "src/utilities/DateUtils";
    import AllTrainingsOverTimeChart from "../../../components/allTrainingsOverTimeChart.svelte";
	import { getApi } from '../../../globalStore';
	import DateInput from "src/components/DateInput.svelte";

    let stackLeft = true;
    let startDate = DateUtils.addDays(Date.now(), -30 * 6);
    async function lazyLoading() {
        return await getApi()?.trainings.getAllSummaries();
	}
</script>

{#await lazyLoading() then data}
    total count: {data?.length}
    <!--TODO: chart is not updated..?-->
    <div>Stack left: <input type="checkbox" checked={stackLeft}></div>
    <DateInput bind:date={startDate}></DateInput>

    <AllTrainingsOverTimeChart data={data} stackToLeft={stackLeft} startDate={startDate} ></AllTrainingsOverTimeChart>
{/await}
