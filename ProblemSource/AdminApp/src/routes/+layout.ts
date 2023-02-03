import { browser } from '$app/environment';
import { goto } from '$app/navigation';
import { base } from '$app/paths';
import { userStore } from 'src/globalStore';
import { Startup } from 'src/startup';
import { get } from 'svelte/store';
import type { LayoutLoad } from '../../.svelte-kit/types/src/routes/$types'

export const load: LayoutLoad = async ({ routeId }) => {
    if (browser) {
        new Startup().init(globalThis);
        await userStore.inited;

        // route guards
        const user = get(userStore);
        if (!user) {
            if (routeId !== '/login') {
                goto(base + '/login');
            }
        } else {
            if (routeId === '/login') {
                if (user.role === 'Admin') {
                    goto(base + '/admin');
                } else {
                    goto(base + '/teacher');
                }
            } else {
                if (user.role !== 'Admin' && routeId?.indexOf('/admin') !== 0) {
                    goto(base + '/login');
                }
            }
        }
        return {
            pageInited: true
        }
    }

}