import { writable } from 'svelte/store';
import type { ApiFacade } from './apiFacade';
import type { CurrentUserInfo } from './currentUserInfo';
import type { Message } from './services/realtime';

//export const apiFacade = writable(ApiFacade);
export const apiFacade = writable<ApiFacade>();
export const loggedInUser = writable<CurrentUserInfo | null>();

export const trainingUpdates = writable<Message[]>();