import { UsersClient, AggregatesClient, TestingClient, TrainingsClient } from "./apiClient";
import { RequestAdapter } from "./RequestAdapter";

export class ApiFacade {
    private aggregatesClient: AggregatesClient;
    private usersClient: UsersClient;
    private trainingsClient: TrainingsClient;
    private testingClient: TestingClient;

    impersonateUser: string | null = null;

    constructor(baseUrl: string) {
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
    }

    get aggregates() { return this.aggregatesClient; }
    get users() { return this.usersClient; }
    get trainings() { return this.trainingsClient; }
    get testing() { return this.testingClient; }
}