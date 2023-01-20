export class RequestAdapter {
    static createUrlAndInit(input: Request) {
        return {
            url: input.url,
            init: {
                body: input.body, cache: input.cache, credentials: input.credentials, headers: input.headers, integrity: input.integrity,
                keepalive: input.keepalive, method: input.method, mode: input.mode, redirect: input.redirect, referrer: input.referrer,
                referrerPolicy: input.referrerPolicy, signal: input.signal
            }
        };
    }

    static createFetchArguments(input: RequestInfo | URL | string, init?: RequestInit): Request {
        let url = "";
        if (input instanceof URL) {
            url = input.toString();
        } else if (typeof (input) === "string") {
            url = input;
        } else {
            const urlAndInit = RequestAdapter.createUrlAndInit(input);
            url = urlAndInit.url;
            init = urlAndInit.init;
        }

        if (init == null) init = <RequestInit>{};

        const headers = new Headers(init.headers);
        init.headers = headers;
        init.credentials = "include";
        init.mode = "cors";

        return new Request(url, init);
    }

    static extendFetch() {
        // https://stackoverflow.com/questions/45425169/intercept-fetch-api-requests-and-responses-in-javascript
        const origFetch = fetch;
        console.warn("origFetch", origFetch);
        // const {fetch: origFetch} = globalThis.window;
        globalThis.window.fetch = async (input: RequestInfo | URL | string, init?: RequestInit) => {
            const response = await origFetch(input, init);

            if (!response.ok && response.status === 401) {
                console.warn("401!!");
                return Promise.reject(response);
            }

            // response interceptor
            const json = () =>
                response
                    .clone()
                    .json()
                    .then((data) => ({ ...data, title: `Intercepted: ${data.title}` }));
            response.json = json;

            return response;
        }
    }
}