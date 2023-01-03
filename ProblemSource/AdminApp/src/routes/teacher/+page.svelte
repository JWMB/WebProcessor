<script lang="ts">
    import { apiFacade as apiFacadeStore } from '../../globalStore';
    import { get } from 'svelte/store';
	import { onMount } from 'svelte';
	import type { TrainingSummary } from 'src/apiClient';
	import TrainingsTable from '../../components/trainingsTable.svelte';

    let trainingSummaries: TrainingSummary[] = [];

    const apiFacade = get(apiFacadeStore);

    async function getTrainings() {
        if (apiFacade == null) {
            console.error("apiFacade null");
            return;
        }
        console.log("Cmon");
        // apiFacade.trainings.getSummaries().then(r => {
        //      trainingSummaries = r;
        //      console.log("OK", trainingSummaries.length);
        //  });
        trainingSummaries = await apiFacade.trainings.getSummaries();
        console.log("OK", trainingSummaries.length); // why is TrainingsTable not always updated?
    }

    onMount(() => getTrainings())
</script>

<div>
    <h1>Trainings</h1>

    <TrainingsTable trainingSummaries={trainingSummaries} numDays={5}></TrainingsTable>

    <!-- {#await trainingsPromise then number}
	    <p>the number is {number}</p>
    {/await} -->
</div>