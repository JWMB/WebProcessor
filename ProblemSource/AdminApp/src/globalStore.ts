import { writable } from 'svelte/store';
import type { ApiFacade } from './apiFacade';
import type { CurrentUserInfo } from './currentUserInfo';

//export const apiFacade = writable(ApiFacade);
export const apiFacade = writable<ApiFacade>();
export const loggedInUser = writable<CurrentUserInfo | null>();