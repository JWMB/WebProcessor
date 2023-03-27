import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vitest/config';
import basicSsl from '@vitejs/plugin-basic-ssl'
import path from 'path';

// TODO: import.meta does not contain 'env' (import.meta.env.VITE_HTTPS does not work)
// TODO: process.env does not contain any variables from .env files (process.env.VITE_HTTPS does not work)
const useHttps = process.env.COMPUTERNAME !== "CND1387M7P";

// basicSsl().then(zx => zx);

export default defineConfig({
	plugins: [sveltekit()].concat(useHttps ? [new Promise(res => res([basicSsl()]))] : []),
	test: {
		include: ['src/**/*.{test,spec}.{js,ts}']
	},
	server: {
		port: 5171,
		https: useHttps,
		// proxy: {
		// 	'/api': {
		// 		target: 'https://localhost:7173', // The API is running locally via IIS on this port
		// 		changeOrigin: true,
		// 		secure: false,
		// 		//rewrite: (path) => path.replace(/^\/api/, '') // The local API has a slightly different path
		// 	  }
		// }
	},
	resolve: { alias: { src: path.resolve('./src') } }
});
