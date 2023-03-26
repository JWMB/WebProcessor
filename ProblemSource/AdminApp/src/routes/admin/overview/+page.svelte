<script lang="ts">
	import AllTrainingsOverTimeChart from "../../../components/allTrainingsOverTimeChart.svelte";
	import { getApi } from '../../../globalStore';

    async function lazyLoading() {
        return await getApi()?.trainings.getAllSummaries();
	}
</script>

{#await lazyLoading() then data}
    len: {data?.length}
    <AllTrainingsOverTimeChart data={data}></AllTrainingsOverTimeChart>
{/await}
