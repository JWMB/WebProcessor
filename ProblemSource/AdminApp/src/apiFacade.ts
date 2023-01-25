import { AccountsClient, AggregatesClient, TestingClient, TrainingsClient } from "./apiClient";
import { RequestAdapter } from "./RequestAdapter";

export class ApiFacade {
    private aggregatesClient: AggregatesClient;
    private accountsClient: AccountsClient;
    private trainingsClient: TrainingsClient;
    private testingClient: TestingClient;

    constructor(baseUrl: string) {
        const http =
            // { fetch: fetch };
            { fetch: (r: Request, init?: RequestInit) => fetch(RequestAdapter.createFetchArguments(r, init)) }
        this.aggregatesClient = new AggregatesClient(baseUrl, http);
        this.accountsClient = new AccountsClient(baseUrl, http);
        this.trainingsClient = new TrainingsClient(baseUrl, http);
        this.testingClient = new TestingClient(baseUrl, http);
    }

    get aggregates() { return this.aggregatesClient; }
    get accounts() { return this.accountsClient; }
    get trainings() { return this.trainingsClient; }
    get testing() { return this.testingClient; }
}