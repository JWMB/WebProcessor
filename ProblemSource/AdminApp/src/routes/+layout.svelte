<script lang="ts">
	export const prerender = false;
	export const ssr = false;

    import { apiFacade, loggedInUser, trainingUpdates } from '../globalStore.js';
	import { base } from '$app/paths';
	import { browser } from '$app/environment';
	import { Realtime, type Message } from '../services/realtime.js';
	import { onDestroy } from 'svelte';
	import NotificationBar from 'src/components/notificationBar.svelte';
	import { Startup } from 'src/startup.js';

	const realtime = new Realtime<Message>();
	let notifications: { createdAt: Date, text: string}[] = [];


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
				// Error: A callback for the method 'receivemessage' threw error 'TypeError: $trainingUpdates is not iterable'.
				notifications = [...notifications, { createdAt: new Date(Date.now()), text: msg.username }];
			};
			try { await realtime.connect(Startup.resolveBaseUrl(window.location)); }
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
	<NotificationBar notifications={notifications}></NotificationBar>
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