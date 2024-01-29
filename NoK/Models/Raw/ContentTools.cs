﻿using System.Text.RegularExpressions;

namespace NoK.Models.Raw
{
    public static class ContentTools
    {
        public static string Process(string content, Func<string, string>? preprocess = null)
        {
            if (preprocess != null)
                content = preprocess(content);

            if (content.Contains("[vektor") || content.Contains("[exempel"))
            { }

            content = HandleFakeTags(content);
            return content;
            //return Replacer.ReplaceWrapped(content, "`", "`",
            //    s => SimpleMath.parseMath(s),
            //    s => s);
        }

        public static string HandleFakeTags(string str)
        {
            Regex createRxForTag(string tag)
            {
                return new Regex(@$"\[{tag}([^\]]+)\](.+?)?\[\/{tag}\]");
            }

            Func<string, string> createHandleTag(string tag, Func<Dictionary<string, string>, string> render)
            {
                var rx = createRxForTag(tag);

                return (str) =>
                {
                    return rx.Replace(str, m =>
                    {
                        // TODO: do this in C#
                        //var template = document.createElement("template");
                        var innerHtml = $"<div {m.Groups[0].Value}>{m.Groups[1].Value}</div>";
                        //template.innerHTML = innerHtml;
                        //var el = template.content.querySelector("div");
                        var x = new Dictionary<string, string> { { "innerHTML", innerHtml } };
                        //el.getAttributeNames().forEach(n => x[n] = el.getAttribute(n));
                        return render(x);
                    });
                };
            }

            //const fExampleRow = createHandleTag("vektorExampleRow", (o) => `<ul><i>${o["prefix"] || ""}</i><b>${o["comment"] || ""}</b>${o["answer"] || ""}${o["innerHTML"] || ""}</ul>`);
            var createForExampleRow = createHandleTag("vektorExampleRow",
                o => $"<ul><i>${Get(o, "prefix")}</i><b>${Get(o, "comment")}</b>${Get(o, "answer")}${Get(o, "innerHTML")}</ul>");
            //const fAssignment = createHandleTag("vektorAssignment", (o) => `<div><i>${o["prefix"] || ""}</i><b>${o["comment"] || ""}</b>${o["answer"] || ""}${o["innerHTML"] || ""}</div>`);
            var createForAssignment = createHandleTag("vektorAssignment",
                o => $"<div><i>${Get(o, "prefix")}</i><b>${Get(o, "comment")}</b>${Get(o, "answer")}${Get(o, "innerHTML")}</div>");

            string Get(Dictionary<string, string> o, string key) => o.GetValueOrDefault(key, "");

            var result = createForAssignment(createForExampleRow(str))
                .ReplaceRx(@"\[(\/?)(vanstermarginal)\]", "<$1h4>")
                .ReplaceRx(@"\[(\/?)(vektorExample)\]", "<$1ul>")
                .ReplaceRx(@"\[(\/?)(vektorExampleRow)\]", "<$1li>")
                // .replace(@"\[(\/?)(vektorExampleRow)\]", "")
                .ReplaceRx(@"\[(\/?)(vektorReview)\]", m => m.Value.IndexOf("[/") < 0 ? "<div style=\"background-color:#fee\">" : "</div>")
                //(str: string, ...args: any[]) => str.indexOf("[/") < 0 ? `<div style="background-color:#fee">` : "</div>")
                //.replace(@"\[(\/?)(definition)\]", (str: string, ...args: any[]) => str.indexOf("[/") < 0 ? `<div style="background-color:#efe">` : "</div>")
                .ReplaceRx(@"\[(\/?)(exempel)\]", m => m.Value.IndexOf("[/") < 0 ? "<div style=\"background-color:#efe\">" : "</div>")
                //(str: string, ...args: any[]) => str.indexOf("[/") < 0 ? `<div style="background-color:#efe">` : "</div>")
                .ReplaceRx(@"\[(\/?)(exempelSvar)\]", m => m.Value.IndexOf("[/") < 0 ? "<div style=\"background-color:#efd\">" : "</div>")
                //(str: string, ...args: any[]) => str.indexOf("[/") < 0 ? `<div style="background-color:#efd">` : "</div>")
                //.replace(@"\<(\/?)(oembed)", "<$1embed")
                .Replace(@"src=\""\/([^\""]+)", "src=\"https://files.matematik.nokportalen.se/public/$1")
                .ReplaceRx(@"\[input ([^\]]+)\]", m => $"<input type='${(m.Groups[0].Value == "unit=\"Ja/Nej\"" ? "checkbox" : "text")}'>")
                //(str: string, ...args: any[]) => { return `<input type='${args[0] == 'unit="Ja/Nej"' ? "checkbox" : "text"}'>`; })
                ;
            return result;
        }
    }

    public static class Replacer
    {
        public delegate string Replace(string s, int? start, int? end);
        public static string ReplaceWrapped(string str, string openingString, string closingString, Replace? replaceInner = null, Replace? replaceOuter = null)
        {
            var index = 0;
            var isInside = false;
            var lookFor = openingString;
            var parts = new List<string>();

            void Add(int i, int n)
            {
                var part = str.Substring(i, n);
                parts.Add((!isInside ? replaceInner?.Invoke(part, i, n) : replaceOuter?.Invoke(part, i, n)) ?? part);
            }

            while (true)
            {
                var next = str.IndexOf(lookFor, index);
                isInside = !isInside;
                if (next < 0)
                {
                    Add(index, str.Length);
                    break;
                }
                Add(index, next);
                lookFor = isInside ? openingString : closingString;
                index = next + 1;
            }
            return parts.Any() ? string.Join("", parts) : replaceOuter == null ? str : replaceOuter(str, index, null);
        }
    }

}