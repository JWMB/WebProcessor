<script lang="ts">
	import TrainingView from '../../../components/trainingView.svelte';
	import { onMount } from 'svelte';

	let trainingId: number;

	onMount(() => {
		trainingId = parseFloat(new URLSearchParams(window.location.search).get('id') ?? '715955');
	});
</script>

<TrainingView trainingId={trainingId} />
