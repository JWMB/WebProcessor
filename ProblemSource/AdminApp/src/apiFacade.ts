import { UsersClient, AggregatesClient, TestingClient, TrainingsClient, TelemetryClient } from "./apiClient";
import { RequestAdapter } from "./RequestAdapter";

export class ApiFacade {
    private aggregatesClient: AggregatesClient;
    private usersClient: UsersClient;
    private trainingsClient: TrainingsClient;
    private testingClient: TestingClient;
    private telemetryClient: TelemetryClient;

    impersonateUser: string | null = null;

    getUrl(path: string, urlParams?: { [key: string]: any }) {
        const parms = urlParams == null ? "" : `?${Object.keys(urlParams).map(k => `${k}=${encodeURIComponent(`${urlParams[k]}`)}`).join("&")}`;
        return `${this.baseUrl}${path}${parms}`;
    }
    
    constructor(private baseUrl: string) {
        // console.log("api baseUrl", baseUrl);
        const http = {
            fetch: (r: Request, init?: RequestInit) => {
                init = init || <RequestInit>{};
                if (!!this.impersonateUser) {
                    const headers = new Headers(init.headers);
                    headers.set("Impersonate-User", this.impersonateUser);
                    init.headers = headers;
                }
                return fetch(RequestAdapter.createFetchArguments(r, init));
            }
        };
        this.aggregatesClient = new AggregatesClient(baseUrl, http);
        this.usersClient = new UsersClient(baseUrl, http);
        this.trainingsClient = new TrainingsClient(baseUrl, http);
        this.testingClient = new TestingClient(baseUrl, http);
        this.telemetryClient = new TelemetryClient(baseUrl, http);
    }

    get aggregates() { return this.aggregatesClient; }
    get users() { return this.usersClient; }
    get trainings() { return this.trainingsClient; }
    get testing() { return this.testingClient; }
    get telemetry() { return this.telemetryClient; }
}