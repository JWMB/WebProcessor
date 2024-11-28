// @ts-nocheck
const defaultRootUrl = "//ki-study.humany.net/";

export function initWidgetImplementationScript(implementationUriName) {
    let c = defaultRootUrl + (implementationUriName || "default") + "/embed.js";
    for (var h, i = /[?&]{1}(humany[^=]*)=([^&#]*)/g; h = i.exec(window.location.search);)
        c += (-1 < c.indexOf("?") ? "&" : "?") + h[1] + "=" + h[2];

    const f = document.createElement("script"); 
    if (!f) return;
    f.async = !0;
    f.src = c;

    const g = document.getElementsByTagName("script")[0];
    if (!!g && !!g.parentNode) g.parentNode.insertBefore(f, g);
    else document.head.appendChild(f);

    const humanyObjectName = "Humany";
    window[humanyObjectName] = window[humanyObjectName] || {
        _c: [],
        configure: function () {
            window[humanyObjectName]._c.push(arguments)
        }
    };;

    const j = humanyObjectName.toLowerCase();
    window[j] = window[j] || {
        _c: [],
        configure: function () {
            window[j]._c.push(arguments)
        }
    }
}
