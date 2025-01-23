<script lang="ts">
	import type { PageData } from './$types';
	export let data: PageData;
</script>

<svelte:head>
	<title>{data.title}</title>
</svelte:head>

<h2>{data.title}</h2>
{#if data.content}
	<svelte:component this={data.content} />
{/if}
<a href="./">Back</a>
