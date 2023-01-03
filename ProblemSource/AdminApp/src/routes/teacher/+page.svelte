<script lang="ts">
    import { apiFacade as apiFacadeStore } from '../../globalStore';
    import { get } from 'svelte/store';
	import { onMount } from 'svelte';
	import type { TrainingSummary } from 'src/apiClient';
	import TrainingsTable from '../../components/trainingsTable.svelte';

    let trainingSummaries: TrainingSummary[] = [];

    const apiFacade = get(apiFacadeStore);

    async function getTrainings() {
        trainingSummaries = await apiFacade.trainings.getSummaries();
    }

    onMount(() => getTrainings())
</script>

<div>
    <h1>Trainings</h1>

    <TrainingsTable trainingSummaries={trainingSummaries} numDaysBack={5}></TrainingsTable>
</div>