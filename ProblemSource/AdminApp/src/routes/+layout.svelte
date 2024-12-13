<script lang="ts">
	import { base } from '$app/paths';
	import { Modals, closeModal } from 'svelte-modals';

	import type { PageData } from './$types';

	import { goto } from '$app/navigation';
	import { onMount } from 'svelte';
	import { initWidgetImplementationScript } from 'src/humany-embed';
	import '../app.css';
	import HelpWidget, { showHelpPage } from '../components/helpWidget.svelte';
	import NotificationBar from '../components/notificationBar.svelte';
	import { getString } from '../utilities/LanguageService';
	import { getApi, userStore } from '../globalStore';

	export let data: PageData;

	let useSimpleLayout = false;
	if (data.route && data.route.id && data.route.id.indexOf('/help') > -1) {
		useSimpleLayout = true;
	}

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

{#if useSimpleLayout}
	<slot />
{:else}
	{#if data.pageInited}
		<div class="login-status">
			<button class="inline-button" on:click={() => showHelpPage('en')}>FAQ</button>
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

	<HelpWidget />
{/if}

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
