<script lang="ts">
	import { browser } from '$app/environment';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { createEventDispatcher, tick } from 'svelte';

	const dispatchEvent = createEventDispatcher<{ selected: string }>();

	export let tabs: Array<{ label?: string; id: string; image?: string }> = [];
	export let selected: string = '';
	export let urlParam = '';

	$: current = selected || tabs[0]?.id;

	$: onPageChange(), $page;

	function onPageChange() {
		if (urlParam) {
			const urlTab = $page.url.searchParams.get(decodeURI(urlParam));
			if (urlTab && browser) {
				selectTab(urlTab, false);
			}
		}
	}
	onPageChange();

	async function selectTab(tabId: string, changeUrl = true) {
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
		align-items: flex-start;
	}

	.tabs-container :global(button) {
		margin-top: 3px;
		margin-left: 10px;
	}

	button.tab {
		background: white;
		border: 1px solid #bebebe;
		height: 40px;
		color: black;
		border-radius: 0;
		border-top-left-radius: 6px;
		border-top-right-radius: 6px;
		padding: 0px 10px;
		margin-top: 0;
		margin-left: 0;
		margin-bottom: -1px;
		border-bottom: none;
	}

	.tab.selected {
		background: white;
		height: 41px;
	}
</style>
