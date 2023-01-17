import { get, writable } from 'svelte/store';
import type { ApiFacade } from './apiFacade';
import type { CurrentUserInfo } from './currentUserInfo';
import type { TrainingUpdateMessage } from './services/realtime';
import { SeverityLevel, type NotificationItem } from './types';

export const apiFacade = writable<ApiFacade>();
export const loggedInUser = writable<CurrentUserInfo | null>();

//export const trainingUpdates = writable<TrainingUpdateMessage[]>([]);

export const notificationsStore = (() => {
    const store = writable<NotificationItem[]>([]);
    function add(item: NotificationItem) {
        const logFunc = item.severity == SeverityLevel.critical || item.severity == SeverityLevel.error
            ? console.error : (item.severity == SeverityLevel.warning ? console.warn : console.log);
        logFunc(item.text);

        const n = get(store);
        n.push(item);
        store.set(n);
    }
    return {
        subscribe: store.subscribe,
        add: add
    }
})();
