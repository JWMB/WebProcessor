import { goto } from "$app/navigation";
import { base } from "$app/paths";
import { ApiException } from "./apiClient";
import { notificationsStore, type NotificationItemDto } from "./globalStore";
import { SeverityLevel } from "./types";


export interface ExtendedError {
    message: string | null;
    name: string | null;
    stack: string | null;
    cause: unknown;
    httpStatus: number | null;
    httpResponse: { [key: string]: any } | string | null;
    httpHeaders: { [key: string]: any } | null;
}

export class ErrorHandling {
    static getErrorObject(err: unknown): ExtendedError {
        const result = <ExtendedError>{ message: null, httpStatus: null, name: null, stack: null };
        if (err == null) {
            
        } else if (typeof err === "object") {
            if (err instanceof ApiException) {
                console.log("ApiException", JSON.stringify(err));
                result.message = err.message;
                result.name = err.name;
                result.httpStatus = err.status;
                result.stack = err.stack || null;
                result.cause = err.cause;
                if (err.response.startsWith("{")) {
                    // type, title, status, traceId
                    const errorDetails = JSON.parse(err.response);
                    result.httpResponse = errorDetails;
                    if (!!errorDetails.title) {
                        result.message = errorDetails.title;
                    }
                } else if (err.response.length > 0) {
                    result.httpResponse = err.response;
                }

            } else if (err instanceof Error) {
                console.log("Error", JSON.stringify(err));
                result.message = err.message;
                result.name = err.name;
                result.stack = err.stack || null;
                result.cause = err.cause;
            }
        } else if (typeof err === "string") {
            result.message = err;
        }

        return result;
    }

    static setupTopLevelErrorHandling(root: typeof globalThis | Window) {
        root.onunhandledrejection = (e) => {
            let notification: NotificationItemDto | null = null;

            if (!!e.reason) {
                let statusPrefix = "";
                let message = "Unknown";
                let details: { [key: string]: string } = {};

                //TODO: use const parsed = ErrorHandling.getErrorObject(e.reason);
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
                } else if (typeof e.reason === "string") {
                    message = e.reason;
                }
                notification = { text: `${statusPrefix}${message}`, data: e.reason, details: details, severity: SeverityLevel.error };
            } else {
                notification = { text: typeof e === "string" ? e : "Unknown", data: e, severity: SeverityLevel.error };
            }

            notificationsStore.add(notification);
        }
    }
}
