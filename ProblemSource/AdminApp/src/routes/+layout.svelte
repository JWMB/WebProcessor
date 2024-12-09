<script lang="ts">
	import { base } from '$app/paths';
	import NotificationBar from 'src/components/notificationBar.svelte';
	import { Modals, closeModal } from 'svelte-modals';
	import { getApi, userStore } from 'src/globalStore';
	import type { PageData } from './$types';
	import { getString } from 'src/utilities/LanguageService';
	import { goto } from '$app/navigation';
	import { onMount } from 'svelte';
	import { initWidgetImplementationScript } from 'src/humany-embed';
	import '../app.css';

	export let data: PageData;

	async function logout() {
		await getApi()?.users.logout();
		goto(base + '/login');
	}
	async function login() {
		goto(base + '/login');
	}

	onMount(() => {
		initWidgetImplementationScript();
	});
</script>

{#if data.pageInited}
	<div class="login-status">
		{#if $userStore}
			<!-- <a href="//ki-study.humany.net/admin-notices-ow">Notices</a> -->
			<span> {$userStore?.username}</span>
			<button on:click={logout}>{getString('navbar_logout_label')}</button>
		{:else}
			<button on:click={login}>{getString('navbar_login_label')}</button>
		{/if}
	</div>
	<slot />
{:else}
	<a href="login">Loading...</a>
{/if}

{#if $userStore}
	<a href="/admin/help/en">Help</a>
{/if}

<NotificationBar />

<Modals>
	<div slot="backdrop" class="modal-backdrop" on:click={closeModal} />
</Modals>

<style global>
	.login-status {
		position: absolute;
		top: 10px;
		right: 10px;
		display: flex;
		align-items: center;
		gap: 11px;
		z-index: 2;
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
