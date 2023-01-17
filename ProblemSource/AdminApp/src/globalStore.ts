import { get, writable } from 'svelte/store';
import type { ApiFacade } from './apiFacade';
import type { CurrentUserInfo } from './currentUserInfo';
import { SeverityLevel, type NotificationItem } from './types';

export const apiFacade = writable<ApiFacade>();
export const loggedInUser = writable<CurrentUserInfo | null>();

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
    return {
        subscribe: store.subscribe,
        add: add
    }
})();
