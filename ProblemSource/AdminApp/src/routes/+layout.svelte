<script lang="ts">
	export const prerender = false;
	export const ssr = false;

	import { notificationsStore, apiFacade, loggedInUser } from '../globalStore.js';
	import { base } from '$app/paths';
	import { browser } from '$app/environment';
	import { Realtime } from '../services/realtime.js';
	import { onDestroy } from 'svelte';
	import NotificationBar from 'src/components/notificationBar.svelte';
	import { Startup } from 'src/startup.js';
	import type { TrainingUpdateMessage } from 'src/types.js';
	import { Modals, closeModal } from 'svelte-modals';

	const realtime = new Realtime<TrainingUpdateMessage>();
	let realtimeConnected: boolean | null = false;

	async function logout() {
		if (realtime.isConnected) {
			realtime.disconnect();
		}
		await $apiFacade.accounts.logout();
		$loggedInUser = null;
	}

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

	if (browser) {
		new Startup().init(globalThis);
	}
</script>

<nav>
	{#if $loggedInUser?.loggedIn == true}
		<a href="{base}/">Home</a>

		{#if $loggedInUser.role == 'Admin'}
			<a href="{base}/admin">Admin</a>
			<a href="{base}/admin/teacher">Teacher</a>
			<a href="{base}/teacher">Teacher2</a>
		{/if}
		{#if $loggedInUser.role == 'Admin'}
			<button disabled={realtimeConnected == null} on:click={() => toggleRealtimeConnection()}>{realtimeConnected == true ? 'Disconnect' : 'Connect'}</button>
		{/if}
		<a href="{base}/" on:click={logout}>Log out {$loggedInUser?.username}</a>
	{:else}
		<a href="{base}/login">Log in</a>
	{/if}
</nav>
<div class="page-container">
	<NotificationBar />
	<slot />
</div>

<Modals>
	<div slot="backdrop" class="backdrop" on:click={closeModal} />
</Modals>

<style>
	:global(body) {
		font-family: monospace;
	}
	:global(input) {
		font-family: monospace;
	}
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

	.backdrop {
		position: fixed;
		z-index: 10;
		top: 0;
		bottom: 0;
		right: 0;
		left: 0;
		background: rgba(0, 0, 0, 0.5);
	}
</style>
