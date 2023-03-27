import { goto } from "$app/navigation";
import { base } from '$app/paths';
import { PUBLIC_LOCAL_SERVER_PATH } from '$env/static/public'
import { ErrorHandling } from "./errorHandling";

export class Startup {
    init(root: typeof globalThis | Window) {
        ErrorHandling.setupTopLevelErrorHandling(root);

        if (root.location.pathname.toLowerCase().endsWith("index.html")) {
            const urlSearchParams = new URLSearchParams(window.location.search);
            let path = urlSearchParams.get("path");
            if (path != null && path.length > 1) {
                if (path.startsWith("/")) path = path.substring(1);
                goto(`${base}/${path}`);
            }
        }
    }

    // // TODO: moved to separate export b/c bizarre "Cannot access 'Startup' before initialization"
    // static resolveLocalServerBaseUrl(location: Location) {
    //     return PUBLIC_LOCAL_SERVER_PATH || location.origin;
    // }
}

export function resolveLocalServerBaseUrl(location: Location) {
    return PUBLIC_LOCAL_SERVER_PATH || location.origin;
}
