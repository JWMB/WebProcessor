import { SvelteComponentTyped } from 'svelte';

const allPosts = import.meta.glob('/src/help/posts/**/*.md', { eager: true });

export const load = async ({ params }) => {
	const allPosts = import.meta.glob('/src/help/posts/**/*.md', { eager: true }); // Load all Markdown files

	console.log('allPosts', allPosts);
	const path = '/src/help/posts/' + params.lang + '/' + params.slug + '.md';
	const file: { metadata: { title: string; categories: string; posted: string }; default: ConstructorOfATypedSvelteComponent } = allPosts[path] as any;

	const metadata = file.metadata;
	const slug = path.split('/').at(-1)?.replace('.md', '') || 'non-existing';
	const title = metadata.title;
	const categories = metadata.categories?.split('|') || [];
	const posted = new Date(metadata.posted);
	const content = file.default;
	const lang = params.lang;
	console.log({ slug, title, categories, posted, content, lang });
	return { slug, title, categories, posted, content, lang };
};
