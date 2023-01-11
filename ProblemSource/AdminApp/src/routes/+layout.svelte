<script lang="ts">
	export const prerender = true;

    import { apiFacade, loggedInUser } from '../globalStore.js';
    import { ApiFacade } from '../apiFacade';
	import { ApiException } from '../apiClient';
	import { goto } from '$app/navigation';
	import { base } from '$app/paths';
	import type { CurrentUserInfo } from 'src/currentUserInfo.js';

	let loggedInUserInfo: CurrentUserInfo | null; // = get(loggedInUser);
	let apiFacadeInstance: ApiFacade;

	loggedInUser.subscribe(value => {
		loggedInUserInfo = value;
	});

	async function logout() {
		await apiFacadeInstance.accounts.logout();
		loggedInUserInfo = { username: "", loggedIn: false, role: "" };
		loggedInUser.set(loggedInUserInfo);
	}

	function initApi(location: Location) {
		const apiBaseUrl = location.host.indexOf("localhost") >= 0 || location.host.indexOf(":8080") > 0
			? "https://localhost:7173" : location.origin;
		apiFacadeInstance = new ApiFacade(apiBaseUrl);
		apiFacade.set(apiFacadeInstance);
	}

	function setupTopLevelErrorHandling(root: typeof globalThis | Window) {
		root.onunhandledrejection = (e) => {
		  if (e.reason instanceof ApiException) {
			const apiEx = <ApiException>e.reason;
			if (apiEx.status === 401) {
				goto(`${base}/login`);
				return;
			} else if (apiEx.status === 404) {
				console.log("404!");
				return;
			}
		  } else if (!!e.reason?.message) {
			console.log(e.reason.message, { stack: e.reason.stack });
			return;
		  }
		  console.log('we got exception, but the app has crashed', e);
			// here we should gracefully show some fallback error or previous good known state
			// this does not work though:
			// current = C1; 
			
			// todo: This is unexpected error, send error to log server
			// only way to reload page so that users can try again until error is resolved
			// uncomment to reload page:
			// window.location = "/oi-oi-oi";
		}
	}

	if (globalThis == null) {
		console.error("No globalThis", globalThis);
	} else if (globalThis.location == null) {
		console.error("No globalThis.location", globalThis.location);
	} else {
		initApi(globalThis.location);
		setupTopLevelErrorHandling(globalThis);
	}
</script>

<nav>
	<a href="{base}/">Home</a>
	<a href="{base}/teacher">Teacher</a>
	{#if loggedInUserInfo?.loggedIn}
	<a href="{base}/" on:click={logout}>Log out {loggedInUserInfo?.username}</a>
	{:else}
	<a href="{base}/login">Log in</a>
	{/if}
	{#if loggedInUserInfo?.role == "Admin"}
	<a href="{base}/admin">Admin</a>
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