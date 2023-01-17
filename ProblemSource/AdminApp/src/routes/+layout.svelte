<script lang="ts">
	export const prerender = false;
	export const ssr = false;

    import { notificationsStore, apiFacade, loggedInUser } from '../globalStore.js';
	import { base } from '$app/paths';
	import { browser } from '$app/environment';
	import { Realtime, type TrainingUpdateMessage } from '../services/realtime.js';
	import { onDestroy } from 'svelte';
	import NotificationBar from 'src/components/notificationBar.svelte';
	import { Startup } from 'src/startup.js';

	const realtime = new Realtime<TrainingUpdateMessage>();

	async function logout() {
		if (realtime.isConnected) {
			realtime.disconnect();
		}
		await $apiFacade.accounts.logout();
		$loggedInUser = { username: "", loggedIn: false, role: "" };
	}

	async function toggleRealtimeConnection() {
		if (realtime.isConnected) {
			realtime.disconnect();
		} else {
			realtime.onConnected = () => console.log("ok, connected");
			realtime.onDisconnected = (err) => console.log("disconnected", err);
			realtime.onReceived = msg => { 
				console.log("received", msg.username, msg.events);
				notificationsStore.add({ createdAt: new Date(Date.now()), text: msg.username });
				// $notifications = [...$notifications, { createdAt: new Date(Date.now()), text: msg.username }];
			};
			try { await realtime.connect(Startup.resolveLocalServerBaseUrl(window.location)); }
			catch (err) { console.log("error connecting", err); }
		}
	}

	onDestroy(() => {
		console.log("Destroy!  ");
		realtime.disconnect();
	});

	if (browser) {
		new Startup().init(globalThis);
	}
</script>

<nav>
	<a href="{base}/">Home</a>
	<a href="{base}/teacher">Teacher</a>
	{#if $loggedInUser?.role == "Admin"}
	<a href="{base}/admin">Admin</a>
	{/if}
	{#if $loggedInUser?.loggedIn}
	<a href="{base}/" on:click={logout}>Log out {$loggedInUser?.username}</a>
	{:else}
	<a href="{base}/login">Log in</a>
	{/if}
	{#if $loggedInUser?.role == "Admin"}
	<button on:click={() => toggleRealtimeConnection()}>{realtime.isConnected ? "Disconnect" : "Connect"}</button> <!--TODO: how to auto-update text (connect/disconnect)?-->
	{/if}
</nav>
<div class="page-container">
	<NotificationBar></NotificationBar>
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