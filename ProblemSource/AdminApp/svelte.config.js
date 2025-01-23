//import adapter from '@sveltejs/adapter-auto';
import adapter from '@sveltejs/adapter-static';
import { vitePreprocess } from '@sveltejs/kit/vite';
import { mdsvex } from 'mdsvex';
// import preprocess from 'svelte-preprocess';

/** @type {import('mdsvex').MdsvexOptions} */
const mdsvexOptions = {
	extensions: ['.md']
}

/** @type {import('@sveltejs/kit').Config} */
const config = {
	// Consult https://github.com/sveltejs/svelte-preprocess
	// for more information about preprocessors
	// preprocess: preprocess(),
	extensions: ['.svelte', '.md'],
	preprocess: [vitePreprocess(), mdsvex(mdsvexOptions)],

	kit: {
		// adapter: adapter()
		adapter: adapter({ fallback: 'index.html' }),
		prerender: { entries: ['/help/en', '/help/en/first'] },
		paths: {
			base: "/admin",
			// Not working:
			// https://github.com/sveltejs/kit/issues/2958
			// https://github.com/sveltejs/kit/pull/7543
		},
	}
};

export default config;
