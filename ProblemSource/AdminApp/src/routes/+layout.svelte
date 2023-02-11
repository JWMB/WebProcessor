<script lang="ts">
	export const prerender = false;
	export const ssr = false;

	import { base } from '$app/paths';
	import NotificationBar from 'src/components/notificationBar.svelte';
	import { Modals, closeModal } from 'svelte-modals';
	import { getApi, userStore } from 'src/globalStore';
	import type { PageData } from './$types';
	import { getString } from 'src/utilities/LanguageService';
	import { goto } from '$app/navigation';
	import { onMount } from 'svelte';
	import { initWidgetImplementationScript } from 'src/humany-embed';

	export let data: PageData;

	async function logout() {
		await getApi()?.accounts.logout();
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
		<!-- <a href="//ki-study.humany.net/admin-notices-ow">Notices</a> -->
		{#if $userStore}
			<span> {$userStore?.username}</span>
			<button on:click={logout}>{getString('navbar_logout_label')}</button>
		{:else}
			<button on:click={login}>{getString('navbar_login_label')}</button>
		{/if}
	</div>
	<slot />
{/if}
		
<a href="//ki-study.humany.net/teacher">Help</a>

<NotificationBar />

<Modals>
	<div slot="backdrop" class="modal-backdrop" on:click={closeModal} />
</Modals>

<style global>
	/* attempt to use humany inline, but only show notices section */
	.humany_admin-notices-ow div[data-name="widget-header"] {
		display: none;
	}
	.humany_admin-notices-ow div[data-name="index-primary-area"] {
		display: none;
	}
	.humany_admin-notices-ow div[data-name="index-secondary-area"] {
		display: none;
	}
	.humany_admin-notices-ow div[data-name="search"] {
		display: none;
	}
	.humany_admin-notices-ow div[data-name="copyright"] {
		display: none;
	}

	body {
		font-family: sans-serif;
	}
	input {
		font-family: sans-serif;
	}

	:global(button) {
		border: 1px solid #4ba7b2;
		background: white;
		color: #4ba7b2;
		border-radius: 5px;
		padding: 0px 10px;
		height: 30px;
		vertical-align: middle;
	}

	:global(button.primary) {
		font-weight: bold;
		background: #4ba7b2;
		color: white;
		border: none;
	}

	.login-status {
		position: absolute;
		top: 10px;
		right: 10px;
		display: flex;
		align-items: center;
		gap: 11px;
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

	html {
		box-sizing: border-box;
	}

	*,
	*:before,
	*:after {
		box-sizing: inherit;
	}

	[data-tooltip] {
		position: relative;
		z-index: 2;
		display: block;
		color: red;
	}

	[data-tooltip]:before,
	[data-tooltip]:after {
		visibility: hidden;
		opacity: 0;
		pointer-events: none;
		transition: 0.2s ease-out;
		transform: translate(-50%, 5px);
	}

	[data-tooltip]:before {
		position: absolute;
		bottom: 120%;
		left: 50%;
		margin-bottom: 5px;
		padding: 7px;
		width: 100%;
		min-width: 170px;
		max-width: 250px;
		-webkit-border-radius: 3px;
		-moz-border-radius: 3px;
		border-radius: 3px;
		background-color: #000;
		background-color: hsla(0, 0%, 20%, 0.9);
		color: #fff;
		content: attr(data-tooltip);
		text-align: center;
		font-size: 12px;
		font-weight: normal;
		line-height: 1.2;
		transition: 0.2s ease-out;
		white-space: break-spaces;
	}

	[data-tooltip]:after {
		position: absolute;
		bottom: 120%;
		left: 50%;
		width: 0;
		border-top: 5px solid #000;
		border-top: 5px solid hsla(0, 0%, 20%, 0.9);
		border-right: 5px solid transparent;
		border-left: 5px solid transparent;
		content: ' ';
		font-size: 0;
		line-height: 0;
	}

	[data-tooltip]:hover:before,
	[data-tooltip]:hover:after {
		visibility: visible;
		opacity: 1;
		transform: translate(-50%, 0);
	}

	[data-tooltip='false']:hover:before,
	[data-tooltip='false']:hover:after {
		visibility: hidden;
		opacity: 0;
	}
</style>
