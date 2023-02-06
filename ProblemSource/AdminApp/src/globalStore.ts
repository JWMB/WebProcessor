import { browser } from '$app/environment';
import { get, writable } from 'svelte/store';
import type { LoginCredentials } from './apiClient';
import { ApiFacade } from './apiFacade';
import type { CurrentUserInfo } from './currentUserInfo';
import { Startup } from './startup';
import { SeverityLevel, type NotificationItem } from './types';

let _apiFacade: ApiFacade;
export function getApi() {
    if (browser) {
        if (!_apiFacade) {
            _apiFacade = new ApiFacade(Startup.resolveLocalServerBaseUrl(window.location));
        }
        return _apiFacade;
    }
}

//export const trainingUpdates = writable<TrainingUpdateMessage[]>([]);

export interface NotificationItemDto extends Omit<NotificationItem, "createdAt"> {
    createdAt?: Date;
}

export const notificationsStore = (() => {
    const store = writable<NotificationItem[]>([]);

    function add(item: NotificationItemDto) {
        const logFunc = item.severity == SeverityLevel.critical || item.severity == SeverityLevel.error
            ? console.error : (item.severity == SeverityLevel.warning ? console.warn : console.log);
        logFunc(item.text, item);

        item.createdAt = item.createdAt ?? new Date(Date.now());

        const n = get(store);
        n.push(<NotificationItem>item);
        store.set(n);
    }

    function removeAt(index: number) {
        const n = get(store);
        n.splice(index, 1);
        store.set(n);
    }

    return {
        subscribe: store.subscribe,
        add: add,
        removeAt: removeAt
    }
})();

export const userStore = (() => {
    const loggedInUser = writable<CurrentUserInfo | null>(null);
    async function getLoggedInUser(): Promise<void> {
        return new Promise<void>((resolve) => {
            if (browser) {
                getApi()?.accounts.getLoggedInUser()
                    .then(r => {
                        console.log("logged in with user:", r)
                        loggedInUser.set({ username: r.username, loggedIn: true, role: r.role });
                        setTimeout(() => {
                            resolve();
                        }, 1000)
                    })
                    .catch(err => {
                        console.log("not logged in", err);
                        loggedInUser.set(null);
                        resolve();
                    });
            }
        });
    }
    const inited = getLoggedInUser();

    return {
        inited,
        login: async (credentials: LoginCredentials) => {
            await getApi()?.accounts.login(credentials);
            await getLoggedInUser()
        },
        logout: async () => {
            await getApi()?.accounts.logout();
        },
        subscribe: loggedInUser.subscribe
    }
})()