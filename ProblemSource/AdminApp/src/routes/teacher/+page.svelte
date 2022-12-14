<script lang="ts">
    import { apiFacade as apiFacadeStore } from '../../globalStore';
    import { get } from 'svelte/store';
	// import type { Account } from 'src/apiClient';
	import { onMount } from 'svelte';
    import { base } from '$app/paths';
	import type { Training, TrainingSummary } from 'src/apiClient';

    const apiFacade = get(apiFacadeStore);

    let trainings: TrainingSummary[] = [];
    const getTrainings = async() => {
        trainings = await apiFacade.trainings.getSummaries();
    };
    onMount(() => getTrainings())
</script>

<div>
    <h1>Trainings</h1>
    {#each trainings as training}
    <div>
      <a href="{base}/?id={training.id}">{training.id}</a>&nbsp;{training.days.length}&nbsp;
    </div>
	{/each}
</div>