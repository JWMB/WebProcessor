import { sveltekit } from '@sveltejs/kit/vite';
import basicSsl from '@vitejs/plugin-basic-ssl'
import type { UserConfig } from 'vite';
import path from 'path';

const config: UserConfig = { //
	plugins: [
		sveltekit(),
		basicSsl()
	],
	server: {
		port: 5171,
		https: true,
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
};

export default config;
