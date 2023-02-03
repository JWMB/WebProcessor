import { goto } from "$app/navigation";
import { base } from "$app/paths";
import { userStore } from "src/globalStore";
import { get } from "svelte/store";

export async function handleRedirects(routeId: string) {
    await userStore.inited;
    const user = get(userStore);
    console.log("base", base)
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
}