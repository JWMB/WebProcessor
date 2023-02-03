<script lang="ts">
	import { notificationsStore, userStore } from '../../globalStore.js';
	import { base } from '$app/paths';
	import { Realtime } from '../../services/realtime.js';
	import { onDestroy } from 'svelte';
	import { Startup } from 'src/startup.js';
	import type { TrainingUpdateMessage } from 'src/types.js';

	const realtime = new Realtime<TrainingUpdateMessage>();
	let realtimeConnected: boolean | null = false;

	async function toggleRealtimeConnection() {
		if (realtime.isConnected) {
			realtimeConnected = null;
			realtime.disconnect();
		} else {
			realtime.onConnected = () => {
				realtimeConnected = true;
			};
			realtime.onDisconnected = (err) => {
				realtimeConnected = false;
				console.log('disconnected', err);
			};
			realtime.onReceived = (msg) => {
				console.log('received', msg.username, msg.events);
				notificationsStore.add({ createdAt: new Date(Date.now()), text: msg.username });
				// $notifications = [...$notifications, { createdAt: new Date(Date.now()), text: msg.username }];
			};
			realtimeConnected = null;
			try {
				await realtime.connect(Startup.resolveLocalServerBaseUrl(window.location));
			} catch (err) {
				console.log('error connecting', err);
			}
		}
	}

	onDestroy(() => {
		realtime.disconnect();
	});
</script>

<nav>
	{#if $userStore}
		<a href="{base}/">Home</a>
		<a href="{base}/admin">Admin</a>
		<a href="{base}/admin/teacher">Teacher</a>
		<a href="{base}/teacher">Teacher2</a>
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
