//import adapter from '@sveltejs/adapter-auto';
import adapter from '@sveltejs/adapter-static';
import { vitePreprocess } from '@sveltejs/kit/vite';
import { mdsvex } from 'mdsvex';
// import preprocess from 'svelte-preprocess';

/** @type {import('mdsvex').MdsvexOptions} */
const mdsvexOptions = {
	extensions: ['.md']
}

console.log("svelte process.env.NODE_ENV", process.env.NODE_ENV); //  env: { NODE_ENV: 'production' }
const basePath = ["production", "docker"].indexOf(process.env.NODE_ENV) >= 0 ? "/admin" : undefined;
//const basePath = false ? undefined : "/admin";
console.log("svelte process.env", process.env); //  env: { NODE_ENV: 'production' }
console.log("svelte meta", import.meta.env);


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
			base: basePath,
			// Not working:
			// https://github.com/sveltejs/kit/issues/2958
			// https://github.com/sveltejs/kit/pull/7543
		},
	}
};

export default config;
