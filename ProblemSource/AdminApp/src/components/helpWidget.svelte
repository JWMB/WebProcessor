<script lang="ts" context="module">
	import { writable } from 'svelte/store';
	import { fade, scale } from 'svelte/transition';

	export const showHelpPage = (path: string) => {
		currentUrl.set(path);
	};

	export const closeHelpPage = () => {
		currentUrl.set('');
	};

	const currentUrl = writable<string>('');
</script>

<script lang="ts">
</script>

{#if $currentUrl}
	<div class="widget-container" in:scale out:fade>
		<button class="close-button" on:click={() => closeHelpPage()}>X</button>
		<div class="widget-content">
			<iframe src={'./help/' + $currentUrl} />
		</div>
	</div>
{/if}

<style>
	.widget-container {
		position: fixed;
		padding: 0px;
		width: 100%;
		max-width: 360px;
		height: 100%;
		max-height: 600px;
		background-color: white;
		border-radius: 10px;
		box-shadow: 0px 3px 7px rgba(0, 0, 0, 0.223);
		right: 10px;
		bottom: 10px;
		overflow: hidden;
	}
	.widget-content {
		width: 100%;
		height: 100%;
	}
	.close-button {
		position: absolute;
		top: 5px;
		right: 5px;
	}
	iframe {
		border: none;
		width: 100%;
		height: 100%;
	}
</style>
