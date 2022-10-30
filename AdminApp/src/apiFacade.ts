import { AccountsClient, AggregatesClient } from "./apiClient";

export class ApiFacade {
    private aggregatesClient: AggregatesClient;
    private accountsClient: AccountsClient;

    constructor(baseUrl: string) {
        const http = { fetch: fetch.bind(window) };
        this.aggregatesClient = new AggregatesClient(baseUrl, http);
        this.accountsClient = new AccountsClient(baseUrl, http);
    }

    get aggregates() { return this.aggregatesClient; }
    get accounts() { return this.accountsClient; }
}