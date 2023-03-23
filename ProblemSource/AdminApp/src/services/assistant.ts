export class Assistant {
    constructor(private widgetId: string) {
    }

    openWidgetOnGuide(guideId: number) {
        this.openWidgetAndExecuteRouting(router => router.navigate("guide", { guide: guideId.toString(), uriName: "ABC" }));
    }

    openWidgetWithSearch(phrase: string) {
        this.openWidgetAndExecuteRouting(router => router.navigate("index", { phrase: phrase }));
    }

    openWidgetWithFirstSearchHit(phrase: string) {
        Assistant.searchGuide(phrase, this.widgetId, "ki-study.humany.net").then(id => {
            if (!id) {
                console.warn(`No guide "${phrase}" found`);
                return;
            }
            this.openWidgetOnGuide(id);
        });
    }

    private openWidgetAndExecuteRouting(routerCall: (router: any) => void) {
        const widget = (<any>window).humany.widgets.find(this.widgetId);
        widget.invoke("open").then(() => {
            widget.container.getAsync('router').then((router: any) => routerCall(router));
        });
    }

    private static async searchGuide(phrase: string, widgetId: string, domain: string) {
        const url = `https://${domain}/${widgetId}/guides?client=bf8d9822-7929-1010-8e57-385304987de4&phrase=${phrase}&skip=0&take=10&sorting.type=popularity&sorting.direction=descending`;

        const result = await fetch(url, {
            "headers": {
              "accept": "*/*",
              "accept-language": "sv-SE,sv;q=0.9,en-US;q=0.8,en;q=0.7",
            },
            "referrer": window.location.toString(),
            "referrerPolicy": "strict-origin-when-cross-origin",
            "body": null,
            "method": "POST",
            "mode": "cors",
            "credentials": "omit"
        });
        const json = await result.json();
        const firstHit = json["Matches"][0] || {};
        return firstHit["Id"]; //{ id: firstHit["Id"], relativeUrl: firstHit["RelativeUrl"] };
    }
}