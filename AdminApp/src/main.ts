import { apiFacade } from './globalStore.js';
import { ApiFacade } from './apiFacade';
import './app.css'
import App from './App.svelte'

const apiBaseUrl = "https://localhost:7090";
const apiFacadeInstance = new ApiFacade(apiBaseUrl);
apiFacade.set(apiFacadeInstance);

const app = new App({
  target: document.getElementById('app')
})

export default app
