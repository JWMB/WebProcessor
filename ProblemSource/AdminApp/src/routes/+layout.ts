import { browser } from '$app/environment';
import { goto } from '$app/navigation';
import { base } from '$app/paths';
import { userStore } from 'src/globalStore';
import { Startup } from 'src/startup';
import { initStrings } from 'src/utilities/LanguageService';
import { get } from 'svelte/store';
import langstrings from 'src/utilities/language_strings.json';
import type { LayoutLoad } from '../../.svelte-kit/types/src/routes/$types'
import { handleRedirects } from 'src/services/redirects';

export const load: LayoutLoad = async ({ routeId }) => {
    console.log('load layout', routeId)
    if (browser) {
        initStrings(langstrings);
        new Startup().init(globalThis);
        await handleRedirects(routeId || '');
        return {
            pageInited: true
        }
    }
}