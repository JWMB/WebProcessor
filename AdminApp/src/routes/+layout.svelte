<script lang="ts">
	export const prerender = true;

    import { apiFacade } from '../globalStore.js';
    import { ApiFacade } from '../apiFacade';
	import { onMount } from 'svelte';

    const apiBaseUrl = "https://localhost:7090";
    const apiFacadeInstance = new ApiFacade(apiBaseUrl);
    apiFacade.set(apiFacadeInstance);

    // onMount(() => {
    //     apiFacadeInstance.aggregates.trainingDayAccount(715955).then(r => console.log(r));
    // });
</script>

<nav>
	<a href="./contacts">Contacts</a>
	<a href="./workflows">Workflows</a>
	<a href="./simulation">Simulation</a>
</nav>
<div class="page-container">
	<slot />
</div>

<style>
	nav {
		position: absolute;
		top: 0;
		left: 0;
		right: 0;
		z-index: 1;
		width: 100%;
		height: 40px;
		display: flex;
		background: white;
		align-items: center;
		gap: 10px;
	}
	.page-container {
		position: absolute;
		top: 40px;
		left: 0;
		right: 0;
		bottom: 0;
	}
</style>