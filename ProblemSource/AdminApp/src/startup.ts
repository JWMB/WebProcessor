import { goto } from "$app/navigation";
import { base } from '$app/paths';
import { ApiException } from "./apiClient";
import { notificationsStore, type NotificationItemDto } from './globalStore.js';
import { SeverityLevel } from "./types";
import { PUBLIC_LOCAL_SERVER_PATH } from '$env/static/public'

export class Startup {
    init(root: typeof globalThis | Window) {
        this.setupTopLevelErrorHandling(root);

        if (root.location.pathname.toLowerCase().endsWith("index.html")) {
            const urlSearchParams = new URLSearchParams(window.location.search);
            let path = urlSearchParams.get("path");
            if (path != null && path.length > 1) {
                if (path.startsWith("/")) path = path.substring(1);
                goto(`${base}/${path}`);
            }
        }
    }

    static resolveLocalServerBaseUrl(location: Location) {
        return PUBLIC_LOCAL_SERVER_PATH || location.origin;
    }

    setupTopLevelErrorHandling(root: typeof globalThis | Window) {
        root.onunhandledrejection = (e) => {
            let notification: NotificationItemDto | null = null;

            if (!!e.reason) {
                let statusPrefix = "";
                let message = "Unknown";
                let details: { [key: string]: string } = {};
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