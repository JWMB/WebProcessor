import { sveltekit } from '@sveltejs/kit/vite';
import basicSsl from '@vitejs/plugin-basic-ssl'
import type { UserConfig } from 'vite';
import path from 'path';
// import { env as envDynPriv } from '$env/dynamic/private'
// import { env as envDynPub } from '$env/dynamic/public'

// TODO: import.meta does not contain 'env' (import.meta.env.VITE_HTTPS does not work)
// TODO: process.env does not contain any variables from .env files (process.env.VITE_HTTPS does not work)
let useHttps = process.env.COMPUTERNAME !== "CND1387M7P";
if (process.env.HTTPS === "false") useHttps = false;
console.log("useHttps", useHttps, process.env);
const port = process.env.PORT ? parseInt(process.env.PORT) : 5171;
// const base = true ? "./" : undefined;
// envDynPub.PUBLIC_LOCAL_SERVER_PATH = envDynPriv["PUBLIC_LOCAL_SERVER_PATH"] || envDynPub.PUBLIC_LOCAL_SERVER_PATH;

const config: UserConfig = { //
	plugins: [
		sveltekit(),
	].concat(useHttps ? [new Promise(res => res([basicSsl()]))] : []),
	server: {
		port: port,
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
	// base: base,
	resolve: { alias: { src: path.resolve('./src') } },
	envPrefix: "PUBLIC"
	// build: { rollupOptions: { cache: false} }
};

export default config;
