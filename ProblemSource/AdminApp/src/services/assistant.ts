export class Assistant {
    static openWidgetOnGuide(guideId: number, uriName: string) {
        Assistant.openWidgetAndExecuteRouting("teacher", router => router.navigate("guide", { guide: guideId.toString(), uriName: uriName }));
    }

    static openWidgetWithSearch(phrase: string) {
        Assistant.openWidgetAndExecuteRouting("teacher", router => router.navigate("index", { phrase: phrase }));
    }

    private static openWidgetAndExecuteRouting(widgetId: string, routerCall: (router: any) => void) {
        const widget = (<any>window).humany.widgets.find(widgetId);
        widget.invoke("open").then(() => {
            widget.container.getAsync('router').then((router: any) => routerCall(router));
        });
    }
}