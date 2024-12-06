import { SvelteComponentTyped } from 'svelte';
import type { PageLoad } from './$types';

export const load: PageLoad = ({ params }) => {
	let posts: Array<{ title: string; categories: string[]; posted: Date; slug: string; content: ConstructorOfATypedSvelteComponent; lang: string }> = [];
	const allPosts = import.meta.glob('/src/help/posts/**/*.md', { eager: true }); // Load all Markdown files
	for (const path in allPosts) {
		const file: { metadata: { title: string; categories: string; posted: string }; default: ConstructorOfATypedSvelteComponent } = allPosts[path] as any;
		if (!path.includes(`/${params.lang}/`)) continue; // Filter by language

		const metadata = file.metadata;
		const slug = path.split('/').at(-1)?.replace('.md', '') || 'non-existing';
		const title = metadata.title;
		const categories = metadata.categories?.split('|') || [];
		const posted = new Date(metadata.posted);
		const content = file.default;
		const lang = params.lang;
		posts.push({ slug, title, categories, posted, content, lang });
	}

	let categories: Array<{ title: string; posted: Date; slug: string }> = [];
	const allCategories = import.meta.glob('/src/help/categories/**/*.md', { eager: true }); // Load all Markdown files
	for (const path in allCategories) {
		const file: { metadata: { title: string; categories: string; posted: string } } = allCategories[path] as any;
		if (!path.includes(`/${params.lang}/`)) continue; // Filter by language

		const metadata = file.metadata;
		const slug = path.split('/').at(-1)?.replace('.md', '') || 'non-existing';
		const title = metadata.title;
		const posted = new Date(metadata.posted);

		categories.push({ slug, title, posted });
	}
	return {
		posts,
		categories
	};
};
