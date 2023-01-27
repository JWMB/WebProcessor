export class UrlParameterManager {
    public static isEnabled = true;

    public static getParams(url: string = ''): { [key: string]: string; } {
        if (url === "") {
            url = document.location.href;
        }
        const splittedURL = url.split("?");
        const params: { [key: string]: string; } = {};
        if (splittedURL.length > 1) {
            const paramStrings = splittedURL[1].split("&");
            paramStrings.forEach((str) => {
                const pair = str.split("=");
                if (pair.length === 1) {
                    pair.push("");
                }
                params[pair[0].toLowerCase()] = pair[1];
            });
        }
        if (UrlParameterManager.isEnabled === false) {
            return {};
        } else {
            return params;
        }
    }

    public static setParams(url: string = '', params: { [key: string]: string; }, redirect = true) {
        if (url === "") {
            url = document.location.href;
        }
        const splittedURL = url.split("?");
        url = splittedURL[0] + '?';
        for (let p in params) {
            if (params.hasOwnProperty(p)) {
                url += p;
                if (params[p]) {
                    url += '=' + params[p];
                }
            }
        }
        if (redirect) {
            history.pushState(null, '', url);
        }
        return url;
    }

    public static setParam(url: string, param: string, value: string | null, redirect = true) {
        if (url === "") {
            url = document.location.href;
        }
        const params = UrlParameterManager.getParams(url);
        if (value) {
            params[param] = value;
        } else {
            delete params[param];
        }
        UrlParameterManager.setParams(url, params)
    }

    public static getStringParam(url: string, param: string) {
        if (url === "") {
            url = document.location.href;
        }
        const params = UrlParameterManager.getParams(url);
        return params[param] || '';
    }

    public static getBoolParam(url: string, param: string) {
        const stringValue = UrlParameterManager.getStringParam(url, param);
        return !!stringValue;
    }

    public static getNumberParam(url: string, param: string) {
        const stringValue = UrlParameterManager.getStringParam(url, param);
        return parseFloat(stringValue) || 0;
    }
}
