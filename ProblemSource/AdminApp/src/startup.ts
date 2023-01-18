import { goto } from "$app/navigation";
import { base } from '$app/paths';
import { ApiException } from "./apiClient";
import { ApiFacade } from './apiFacade';
import { notificationsStore, apiFacade, loggedInUser, type NotificationItemDto } from './globalStore.js';
import { SeverityLevel } from "./types";

export class Startup {
    init(root: typeof globalThis | Window) {
		this.initApi(root.location);
		this.setupTopLevelErrorHandling(root);
    }
	
    static resolveLocalServerBaseUrl(location: Location) {
        return location.host.indexOf("localhost") >= 0 || location.host.indexOf(":8080") > 0
        ? "https://localhost:7173" : location.origin;
    }

    initApi(location: Location) {
        const f = new ApiFacade(Startup.resolveLocalServerBaseUrl(location));
		apiFacade.set(f);
        f.accounts.getLoggedInUser()
            .then(r => {
                loggedInUser.set({ username:r.username, loggedIn: true, role: r.role });
            })
            .catch(err => console.log("not logged in", err));
	}

    setupTopLevelErrorHandling(root: typeof globalThis | Window) {
		root.onunhandledrejection = (e) => {
            let notification: NotificationItemDto | null = null;

            if (!!e.reason) {
                let statusPrefix = "";
                let message = "Unknown";
                let details: {[key: string]: string} = {};
                if (e.reason instanceof Error) {
                    message = e.reason.message;
                    if (e.reason instanceof ApiException) {
                        if (e.reason.status === 401) {
                            goto(`${base}/login`);
                            return;
                        } else if (e.reason.status === 404) {
                            console.log("404!");
                            notification = { text: "Not found", severity: SeverityLevel.warning };
                            return;
                        } else {
                            let s = e.reason.status;
                            if (e.reason.response.startsWith("{")) {
                                const errorDetails = JSON.parse(e.reason.response);
                                message = errorDetails.title;
                                if (errorDetails.status) s = errorDetails.status;
                                details["details"] = errorDetails.details;
                            } else {
                                message = e.reason.response;
                            }
                            statusPrefix = `${s}: `;
                            details["status"] = s.toString();
                        }
                    }
                    details["stack"] = e.reason.stack ?? "";
                }
                notification = { text: `${statusPrefix}${message}`, data: e.reason, details: details, severity: SeverityLevel.error };
            } else {
                notification = { text: "Unknown", data: e, severity: SeverityLevel.error };
            }

            notificationsStore.add(notification);
		}
	}
}