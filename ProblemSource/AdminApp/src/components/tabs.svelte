<script lang="ts">
	import { browser } from '$app/environment';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { createEventDispatcher, onMount, tick } from 'svelte';

	const dispatchEvent = createEventDispatcher<{ selected: string }>();

	export let tabs: Array<{ label?: string; id: string; image?: string }> = [];
	export let selected: string = '';
	export let urlParam = '';

	$: current = selected || tabs[0]?.id;
	$: onPageChange(), $page;
	function onPageChange() {
		console.log('onPageChange');
		if (browser && urlParam) {
			const urlTab = $page.url.searchParams.get(urlParam);
			if (urlTab) {
				console.log('urlTab', urlTab);
				selectTab(decodeURI(urlTab), false);
			} else if (tabs.length > 0) {
				selectTab(tabs[0].id, true);
			} else {
				console.log('tabs not available at start');
			}
		}
	}

	async function selectTab(tabId: string, changeUrl = true) {
		console.log('select tab', tabId, changeUrl);
		if (!tabs.find((t) => t.id === tabId)) {
			return;
		}
		await tick();
		selected = tabId;
		dispatchEvent('selected', tabId);
		if (urlParam && changeUrl) {
			let query = new URLSearchParams($page.url.searchParams);
			query.set(urlParam, encodeURI(tabId));
			goto('?' + query.toString());
		}
	}
</script>

<div class="tabs-container">
	{#each tabs as tab}
		<button
			class="tab"
			class:selected={tab.id === current}
			on:click={() => {
				selectTab(tab.id);
			}}>{tab.label || tab.id}</button>
	{/each}
	<slot />
</div>

<style>
	.tabs-container {
		width: 100%;
		border-bottom: 1px solid #bebebe;
		margin-bottom: 5px;
		display: flex;
		align-items: center;
	}

	.tabs-container :global(button) {
		margin-top: 0px;
		margin-left: 10px;
	}

	button.tab {
		background: #ededed;
		border: 1px solid #ededed;
		height: 34px;
		color: rgb(108 108 108);
		border-radius: 0;
		border-top-left-radius: 6px;
		border-top-right-radius: 6px;
		padding: 0px 10px;
		margin-top: 0;
		margin-left: 0;
		margin-bottom: -1px;
		border-bottom: none;
		min-width: 72px;
	}

	.tab.selected {
		background: white;
		height: 35px;
		color: black;
		border-color: #bebebe;
	}
</style>
