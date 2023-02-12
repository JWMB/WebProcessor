import { goto } from "$app/navigation";
import { base } from "$app/paths";
import { userStore } from "src/globalStore";
import { get } from "svelte/store";

export async function handleRedirects(routeId: string) {
    await userStore.inited;
    const user = get(userStore);

    console.log("handleRedirect", user, routeId);

    if (!user) {
        const returnUrl = window.location.pathname.substring(base.length) + window.location.search;
        const next = base + '/login' + "?returnUrl=" + encodeURIComponent(returnUrl);

        if (routeId !== '/login') {
            goto(next); // TODO: for some reason the url parameters are removed..?
        }
    } else {
        if (routeId === '/login' || routeId === '/') {
            if (user.role === 'Admin') {
                goto(base + '/admin');
            } else {
                goto(base + '/teacher');
            }
        } else {
            // TODO: why redirect here?
            // if (user.role !== 'Admin' && routeId?.indexOf('/admin') !== 0) {
            //     goto(base + '/login');
            // }
        }
    }
}