<script lang="ts">
	export const prerender = true;

    import { apiFacade } from '../globalStore.js';
    import { ApiFacade } from '../apiFacade';
	import { onMount } from 'svelte';
	import { ApiException } from '../apiClient';
	import { goto } from '$app/navigation';
	import { base } from '$app/paths';

	console.log("init layout");
    // const apiBaseUrl = window.location.host.indexOf("localhost") >= 0 || window.location.host.indexOf(":8080") > 0
	// 	? "https://localhost:7173" : window.location.origin;
    // // const apiBaseUrl = "";
    // const apiFacadeInstance = new ApiFacade(apiBaseUrl);
    // apiFacade.set(apiFacadeInstance);

	onMount(() => {
		console.log("window", window);

		const apiBaseUrl = window.location.host.indexOf("localhost") >= 0 || window.location.host.indexOf(":8080") > 0
			? "https://localhost:7173" : window.location.origin;
		// const apiBaseUrl = "";
		const apiFacadeInstance = new ApiFacade(apiBaseUrl);
		apiFacade.set(apiFacadeInstance);

		window.onunhandledrejection = (e) => {
		  if (e.reason instanceof ApiException) {
			const apiEx = <ApiException>e.reason;
			if (apiEx.status === 401) {
				console.warn("Not logged in!!", base);
				goto(`${base}/login`);
				return;
			} else if (apiEx.status === 404) {
				console.log("404!");
				return;
			}
		  } else if (!!e.reason?.message) {
			console.log(e.reason.message, e.reason.stack);
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
	});
</script>

<nav>
	<a href="{base}/">Home</a>
	<a href="{base}/teacher">Teacher</a>
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