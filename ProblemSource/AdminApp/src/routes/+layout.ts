export const prerender = false;
export const ssr = false;

import { browser } from '$app/environment';
import { Startup } from '../startup';
import { initStrings } from '../utilities/LanguageService';
import langstrings from '../utilities/language_strings.json';
import type { LayoutLoad } from '../../.svelte-kit/types/src/routes/$types';
import { handleRedirects } from '../services/redirects';

export const load: LayoutLoad = async (args) => {
	if (browser) {
		initStrings(langstrings);
		new Startup().init(globalThis);
		console.log('main layout.load', args);
		await handleRedirects(args.route.id || '');
		// console.log("main layout.load", args.route);
		// await handleRedirects((args.route || {}).id || '');
		return {
			pageInited: true,
			route: args.route
		};
	}
};
