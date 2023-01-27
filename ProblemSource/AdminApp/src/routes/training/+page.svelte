<script lang="ts">
	import TrainingView from '../../components/trainingView.svelte';
	import { onMount } from 'svelte';

	let accountId: number;
	onMount(() => {
		accountId = parseFloat(new URLSearchParams(window.location.search).get('id') ?? '715955');
	});
</script>

<TrainingView {accountId} />
