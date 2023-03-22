export class Assistant {
    private static widgetId = "teacher";
    static openWidgetOnGuide(guideId: number, uriName: string) {
        Assistant.openWidgetAndExecuteRouting(Assistant.widgetId, router => router.navigate("guide", { guide: guideId.toString(), uriName: uriName }));
    }

    static openWidgetWithSearch(phrase: string) {
        Assistant.openWidgetAndExecuteRouting(Assistant.widgetId, router => router.navigate("index", { phrase: phrase }));
    }

    static openWidgetWithFirstSearchHit(phrase: string) {
        Assistant.searchGuide(phrase).then(o => {
            if (!o?.id || !o?.relativeUrl) return;
            Assistant.openWidgetOnGuide(o.id, o.relativeUrl);
        });
    }


    private static openWidgetAndExecuteRouting(widgetId: string, routerCall: (router: any) => void) {
        const widget = (<any>window).humany.widgets.find(widgetId);
        widget.invoke("open").then(() => {
            widget.container.getAsync('router').then((router: any) => routerCall(router));
        });
    }

    private static async searchGuide(phrase: string) {
        // const url = `https://ki-study.humany.net/teacher/guides?client=bf8d9822-7929-1010-8e57-385304987de4&funnel=teacher&site=%2F%2Fkistudysync.azurewebsites.net%2Fadmin%2Fteacher&categories=&phrase=${phrase}&skip=0&take=10&sorting.type=popularity&sorting.direction=descending&p.LastGuideId=100`;
        const url = `https://ki-study.humany.net/teacher/guides?client=bf8d9822-7929-1010-8e57-385304987de4&phrase=${phrase}&skip=0&take=10&sorting.type=popularity&sorting.direction=descending`;

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
        return { id: firstHit["Id"], relativeUrl: firstHit["RelativeUrl"] };
    }
}