<script lang="ts">
	export const prerender = false;
	export const ssr = false;

    import { apiFacade, loggedInUser, trainingUpdates } from '../globalStore.js';
    // import { ApiFacade } from '../apiFacade';
	import { ApiException } from '../apiClient';
	import { goto } from '$app/navigation';
	import { base } from '$app/paths';
	import type { CurrentUserInfo } from 'src/currentUserInfo.js';
	import { browser } from '$app/environment';
	import { Realtime, type Message } from '../services/realtime.js';
	import { onDestroy } from 'svelte';
	import NotificationBar from 'src/components/notificationBar.svelte';
	import { Startup } from 'src/startup.js';

	let loggedInUserInfo: CurrentUserInfo | null; // = get(loggedInUser);
	// let apiFacadeInstance: ApiFacade;

	const realtime = new Realtime<Message>();
	//let realtimeInfo: { message: Message, removeAt: Date, opacity: number }[] = [];
	let notifications: { createdAt: Date, text: string}[] = [];

	// TODO: use some cookie
	loggedInUser.subscribe(value => {
		loggedInUserInfo = value;
	});

	async function logout() {
		if (realtime.isConnected) {
			realtime.disconnect();
		}
		await $apiFacade.accounts.logout();
		// await apiFacadeInstance.accounts.logout();
		loggedInUserInfo = { username: "", loggedIn: false, role: "" };
		loggedInUser.set(loggedInUserInfo);
	}

	// const resolveBaseUrl = (location: Location) =>
	// 	location.host.indexOf("localhost") >= 0 || location.host.indexOf(":8080") > 0
	// 		? "https://localhost:7173" : location.origin;

	// function initApi(location: Location) {
	// 	apiFacadeInstance = new ApiFacade(resolveBaseUrl(location));
	// 	apiFacade.set(apiFacadeInstance);
	// }

	async function toggleRealtimeConnection() {
		// TODO: how to disconnect on e.g. reload
		if (realtime.isConnected) {
			realtime.disconnect();
		} else {
			realtime.onConnected = () => console.log("ok, connected");
			realtime.onDisconnected = (err) => console.log("disconnected", err);
			realtime.onReceived = msg => { 
				console.log("received", msg.username, msg.events);
				// Error: A callback for the method 'receivemessage' threw error 'TypeError: $trainingUpdates is not iterable'.
				notifications = [...notifications, { createdAt: new Date(Date.now()), text: msg.username }];
				console.log("not", notifications);
				//realtimeInfo = [...realtimeInfo, { message: o, removeAt: new Date(Date.now() + 5 * 1000), opacity: 1}];
			};
			try { await realtime.connect(Startup.resolveBaseUrl(window.location)); }
			catch (err) { console.log("error connecting", err); }
		}
	}

	onDestroy(() => {
		console.log("Destroy!  ");
		realtime.disconnect();
	});

	// function setupTopLevelErrorHandling(root: typeof globalThis | Window) {
	// 	root.onunhandledrejection = (e) => {
	// 	  if (e.reason instanceof ApiException) {
	// 		const apiEx = <ApiException>e.reason;
	// 		if (apiEx.status === 401) {
	// 			goto(`${base}/login`);
	// 			return;
	// 		} else if (apiEx.status === 404) {
	// 			console.log("404!");
	// 			return;
	// 		}
	// 	  } else if (!!e.reason?.message) {
	// 		console.log(e.reason.message, { stack: e.reason.stack });
	// 		return;
	// 	  }
	// 	  console.log('we got exception, but the app has crashed', e);
	// 		// here we should gracefully show some fallback error or previous good known state
	// 		// this does not work though:
	// 		// current = C1; 
			
	// 		// todo: This is unexpected error, send error to log server
	// 		// only way to reload page so that users can try again until error is resolved
	// 		// uncomment to reload page:
	// 		// window.location = "/oi-oi-oi";
	// 	}
	// }

	if (browser) {
		new Startup().init(globalThis);
		// initApi(globalThis.location);
		// setupTopLevelErrorHandling(globalThis);
	}
</script>

<nav>
	<a href="{base}/">Home</a>
	<a href="{base}/teacher">Teacher</a>
	{#if loggedInUserInfo?.role == "Admin"}
	<a href="{base}/admin">Admin</a>
	{/if}
	{#if loggedInUserInfo?.loggedIn}
	<a href="{base}/" on:click={logout}>Log out {loggedInUserInfo?.username}</a>
	{:else}
	<a href="{base}/login">Log in</a>
	{/if}
	{#if loggedInUserInfo?.role == "Admin"}
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