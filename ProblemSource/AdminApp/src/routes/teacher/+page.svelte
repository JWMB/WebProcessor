<script lang="ts">
    import { apiFacade as apiFacadeStore } from '../../globalStore';
    import { get } from 'svelte/store';
	import { onMount } from 'svelte';
	import type { TrainingSummaryWithDaysDto, TrainingSummaryDto } from 'src/apiClient';
	import TrainingsTable from '../../components/trainingsTable.svelte';
	import TrainingGroupsTable from '../../components/trainingGroupsTable.svelte';
	import { goto } from '$app/navigation';
	import { base } from '$app/paths';

    // let trainingSummaries: TrainingSummary[] = [];

    const apiFacade = get(apiFacadeStore);

    let trainingsPromise: Promise<TrainingSummaryWithDaysDto[]>;
    // let trainingGroupsPromise: Promise<{[key: string]: TrainingSummaryDto[] }>;
    let trainingGroupsPromise2: Promise<{ group: string, summaries: TrainingSummaryDto[]}[]>;

    let trainingGroups: { group: string, summaries: TrainingSummaryDto[]}[] = [];

    const clickedGroupRow = (e: CustomEvent<any>) => {
        trainingsPromise = apiFacade.trainings.getSummaries(e.detail.group);
    };
    const clickedTrainingRow = (e: CustomEvent<any>) => {
        console.log("training", e.detail.id);
        goto(`${base}/training?id=${e.detail.id}`);
    };

    async function getTrainings() {
        if (apiFacade == null) {
            console.error("apiFacade null");
            return;
        }
        //trainingsPromise = apiFacade.trainings.getSummaries();

        trainingGroupsPromise2 = new Promise(res => {
            apiFacade.trainings.getGroups().then(r => {
                const asList = Object.entries(r).map(o => ({ group: o[0], summaries: o[1]}));
                trainingGroups = asList;
                res(asList);
            });
        });
        // trainingGroupsPromise = apiFacade.trainings.getGroups();
        //trainingSummaries = await apiFacade.trainings.getSummaries();
        //console.log("OK", trainingSummaries.length); // why is TrainingsTable not always updated?
    }

    onMount(() => getTrainings())
</script>

<div>
    <h2>Classes</h2>
    <!-- {#await trainingGroupsPromise}
        <div>Loading...</div>
    {:then grouped}
        {JSON.stringify(grouped)}
        {#each Object.entries(grouped) as [grp, summaries]}
            {grp}:
            {#each val as v}
                {v}
            {/each}
        {/each}
    {:catch error}
        {error}
    {/await} -->
    <!-- {#await trainingGroupsPromise2}
    <div>Loading...</div>
    {:then groups}
        {#each groups as item}
            {item.summaries.length}
            {item.group}
        {/each}
    {:catch error}
        {error}
    {/await} -->

    <!-- {#await trainingGroups}
    aaa
    {:then groups}
        {#each groups as item}
        {item.group}:
            {#each item.summaries as s}
                {s.id}
            {/each}
        {/each}
    {:catch error}
        {error}
    {/await} -->

    {#await trainingGroupsPromise2}
    <div>Loading...</div>
    {:then trainings}
        <TrainingGroupsTable trainingSummaries={trainings} on:clickedRow={clickedGroupRow}></TrainingGroupsTable>
    {:catch error}
        {error}
    {/await}
    

    <h2>Trainings</h2>
    <!-- <TrainingsTable trainingSummaries={trainingSummaries} numDays={5}></TrainingsTable> -->
    {#await trainingsPromise}
        <div>Loading...</div>
    {:then trainings}
        <TrainingsTable trainingSummaries={trainings} numDays={14} on:clickedRow={clickedTrainingRow}></TrainingsTable>
    {:catch error}
        {error}
    {/await}
</div>