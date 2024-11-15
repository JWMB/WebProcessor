<script lang="ts">
	import { realtimeTrainingListener, userStore } from '../../globalStore.js';
	import { base } from '$app/paths';
	import { onDestroy } from 'svelte';
	
	let realtimeConnected: boolean | null = realtimeTrainingListener.connected();
	const connectionSignal = (status: boolean | null) => {
		realtimeConnected = status;
	}
	realtimeTrainingListener.connectionSignal.on(connectionSignal);

	const toggleRealtimeConnection = async () => {
		if (realtimeTrainingListener.connected() == true) {
			realtimeTrainingListener.disconnect();
		} else {
			await realtimeTrainingListener.connect();
		}
	}

	onDestroy(() => {
		realtimeTrainingListener.disconnect();
	});
</script>

<nav>
	{#if $userStore}
		<a href="{base}/">Home</a>
		<a href="{base}/admin">Admin</a>
		<a href="{base}/admin/teacher">Teacher</a>
		<a href="{base}/admin/overview">Overview</a>
		<a href="{base}/teacher">Teacher2</a>
		<a href="{base}/admin/realtime">Realtime</a>
		<button disabled={realtimeConnected == null} on:click={() => toggleRealtimeConnection()}>{realtimeConnected == true ? 'Disconnect' : 'Connect'}</button>
	{/if}
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
