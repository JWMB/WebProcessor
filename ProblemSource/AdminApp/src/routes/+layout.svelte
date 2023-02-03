<script lang="ts">
	export const prerender = false;
	export const ssr = false;

	import { base } from '$app/paths';
	import { browser } from '$app/environment';
	import NotificationBar from 'src/components/notificationBar.svelte';
	import { Startup } from 'src/startup.js';
	import { Modals, closeModal } from 'svelte-modals';
	import { getApi, userStore } from 'src/globalStore';
	import type { PageData } from './$types';

	export let data: PageData;

	async function logout() {
		await getApi()?.accounts.logout();
	}
</script>

<div class="login-status">
	{#if $userStore}
		<a href="{base}/" on:click={logout}>Log out {$userStore?.username}</a>
	{:else}
		<a href="{base}/login">Log in</a>
	{/if}
</div>

<NotificationBar />
{#if data.pageInited}
	<slot />
{/if}

<Modals>
	<div slot="backdrop" class="modal-backdrop" on:click={closeModal} />
</Modals>

<style>
	:global(body) {
		font-family: monospace;
	}
	:global(input) {
		font-family: monospace;
	}

	.login-status {
		position: absolute;
		top: 10px;
		right: 10px;
	}

	.modal-backdrop {
		position: fixed;
		z-index: 10;
		top: 0;
		bottom: 0;
		right: 0;
		left: 0;
		background: rgba(0, 0, 0, 0.5);
	}
</style>
