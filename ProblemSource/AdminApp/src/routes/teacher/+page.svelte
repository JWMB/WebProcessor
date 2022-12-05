<script lang="ts">
    import { apiFacade as apiFacadeStore } from '../../globalStore';
    import { get } from 'svelte/store';
	// import type { Account } from 'src/apiClient';
	import { onMount } from 'svelte';
    import { base } from '$app/paths';
	import type { Training } from 'src/apiClient';

    const apiFacade = get(apiFacadeStore);

    let trainings: Training[] = [];
    const getTrainings = async() => {
        trainings = await apiFacade.trainings.get();
    };
    onMount(() => getTrainings())
</script>

<div>
    {#each trainings as training}
    <div>
      <a href="{base}/?id={training.id}">{training.id}</a>&nbsp;{training.trainingPlanName}&nbsp;
    </div>
	{/each}
</div>