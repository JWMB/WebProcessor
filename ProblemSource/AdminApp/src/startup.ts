import { goto } from "$app/navigation";
import { base } from '$app/paths';
import { ApiException } from "./apiClient";
import { ApiFacade } from './apiFacade';
import { apiFacade } from './globalStore.js';

export class Startup {
    init(root: typeof globalThis | Window) {
		this.initApi(root.location);
		this.setupTopLevelErrorHandling(root);
    }
	
    static resolveBaseUrl(location: Location) {
        return location.host.indexOf("localhost") >= 0 || location.host.indexOf(":8080") > 0
        ? "https://localhost:7173" : location.origin;
    }

    initApi(location: Location) {
		apiFacade.set(new ApiFacade(Startup.resolveBaseUrl(location)));
	}

    setupTopLevelErrorHandling(root: typeof globalThis | Window) {
		root.onunhandledrejection = (e) => {
		  if (e.reason instanceof ApiException) {
			const apiEx = <ApiException>e.reason;
			if (apiEx.status === 401) {
				goto(`${base}/login`);
				return;
			} else if (apiEx.status === 404) {
				console.log("404!");
				return;
			}
		  } else if (!!e.reason?.message) {
			console.log(e.reason.message, { stack: e.reason.stack });
			return;
		  }
		  console.log('we got exception, but the app has crashed', e);
			// here we should gracefully show some fallback error or previous good known state
			// this does not work though:
			// current = C1; 
			
			// todo: This is unexpected error, send error to log server
			// only way to reload page so that users can try again until error is resolved
			// uncomment to reload page:
			// window.location = "/oi-oi-oi";
		}
	}
}