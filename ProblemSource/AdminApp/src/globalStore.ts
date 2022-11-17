import { writable } from 'svelte/store';
import type { ApiFacade } from './apiFacade';

//export const apiFacade = writable(ApiFacade);
export const apiFacade = writable<ApiFacade>();
