<script lang="ts">
	import Question from '../question.svelte';
	import type { PageData } from './$types';
	export let data: PageData;

	const categories = data.categories.sort((a, b) => b.posted.getTime() - a.posted.getTime());

	function getPostOfCategory(category: string) {
		return data.posts.filter((p) => p.categories.includes(category)).sort((a, b) => b.posted.getTime() - a.posted.getTime());
	}
</script>

<h1>Math App 2 - Help center</h1>
{#each categories as category}
	<h2>{category.title}</h2>
	{#each getPostOfCategory(category.slug) as post}
		<Question question={post.title}>
			<svelte:component this={post.content} />
			<a href={'./' + post.lang + '/' + post.slug}>Link</a>
		</Question>
	{/each}
{/each}
