import { goto } from "$app/navigation";
import { base } from "$app/paths";
import { userStore } from "../globalStore";
import { get } from "svelte/store";

export async function handleRedirects(routeId: string) {
    await userStore.inited;
    const user = get(userStore);

    console.log("handleRedirect", user, routeId, window.location.pathname);
    
    if (/\/login$/.test(window.location.pathname)) {
        return;
    }

    const _goto = (url: string) => {
        //goto(url); // seems my svelte routing has broken with some update..?
        window.location.href = url;
    }

    if (!user) {
        const returnUrl = window.location.pathname.substring(base.length) + window.location.search;
        const next = base + '/login' + 
            (window.location.search.indexOf("returnUrl") >= 0 
            ? window.location.search
             : "?returnUrl=" + encodeURIComponent(returnUrl));

        console.log("next", next);

        if (routeId !== '/login') {
            _goto(next); // TODO: for some reason the url parameters are removed..?
        }
    } else {
        if (routeId === '/login' || routeId === '/') {
            if (user.role === 'Admin') {
                _goto(base + '/admin');
            } else {
                _goto(base + '/teacher');
            }
        } else {
            console.warn("no routeId");
            // TODO: why redirect here?
            // if (user.role !== 'Admin' && routeId?.indexOf('/admin') !== 0) {
            //     goto(base + '/login');
            // }
        }
    }
}