import { browser } from '$app/environment';
import { get, writable } from 'svelte/store';
import type { LoginCredentials } from './apiClient';
import { ApiFacade } from './apiFacade';
import type { CurrentUserInfo } from './currentUserInfo';
import { Assistant } from './services/assistant';
import { resolveLocalServerBaseUrl, Startup } from './startup';
import { SeverityLevel, type NotificationItem } from './types';
import type { TrainingUpdateMessage } from './types.js';
import { Realtime } from './services/realtime';
import { Signal0 } from './utilities/signal';

let _apiFacade: ApiFacade;
export function getApi() {
    if (browser) {
        if (!_apiFacade) {
            _apiFacade = new ApiFacade(resolveLocalServerBaseUrl(window.location));
            const urlParams = new URLSearchParams(window.location.search);
            const impersonate = urlParams.get("impersonate");
            if (impersonate != null) {
                _apiFacade.impersonateUser = impersonate;
            }
        }
        return _apiFacade;
    }
}

export const trainingUpdateStore = (() => {
    const store = writable<TrainingUpdateMessage[]>([]);
    return {
        subscribe: store.subscribe,
        add: (item: TrainingUpdateMessage) => {
            // const n = get(store);
            // n.push(item);
            // store.set(n);
            store.set([item]);
        }
    }
 })();

export const realtimeTrainingListener = (() => { 
    const realtime = new Realtime<TrainingUpdateMessage>();
	let realtimeConnected: boolean | null = false;

    const signal = new Signal0<boolean | null>();
    return {
        connectionSignal: signal,
        connect: async () => {
            if (realtime.isConnected) return;

            realtime.onConnected = () => {
                realtimeConnected = true;
                signal.trigger(true);
			};
			realtime.onDisconnected = (err) => {
				realtimeConnected = false;
                signal.trigger(false);
			};
			realtime.onReceived = (msg) => {
				trainingUpdateStore.add(msg);
			};

            signal.trigger(null);
            try {
				await realtime.connect(Startup.resolveLocalServerBaseUrl(window.location));
			} catch (err) {
				console.error('error connecting', err);
			}
        },
        disconnect: () => realtime.disconnect(),
        connected: () => realtimeConnected,
        destroy: () => realtime.disconnect()
    };
})();

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

export const assistanStore = (() => {
    return new Assistant("teacher");
})();

export const userStore = (() => {
    const loggedInUser = writable<CurrentUserInfo | null>(null);
    async function getLoggedInUser(): Promise<void> {
        return new Promise<void>((resolve) => {
            if (browser) {
                getApi()?.users.getLoggedInUser()
                    .then(r => {
                        console.log("logged in with user:", r);
                        loggedInUser.set({ username: r.username, loggedIn: true, role: r.role });
                        // TODO: why wait?
                        // setTimeout(() => {
                        //     resolve();
                        // }, 1000);
                        resolve();
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
            await getApi()?.users.login(credentials);
            await getLoggedInUser();
        },
        logout: async () => {
            await getApi()?.users.logout();
        },
        subscribe: loggedInUser.subscribe
    }
})()