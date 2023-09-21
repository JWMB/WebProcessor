<script lang="ts">
    import textureMapStr from '/static/cleanUIAssets.json?raw';
	import { groupByX } from 'src/arrayUtils';

    export let history: {time: Date, message: any }[] = [];
    export let getPositioning: (date: Date) => string | null;

    $:visibleItems = history.map(o => ({ item: o, position: getPositioning(o.time) }))
            .filter(o => o.position != null);

    const createLookup = () => {
        const textureMap = JSON.parse(textureMapStr);
        const frames = textureMap["frames"];

        const xx = Object.keys(frames)
            .map(o => ({ m: o.match(/game_icons_\d+_(\w+).png/), id: o, data: frames[o]}))
            .filter(o => o.m != null && o.m.length > 1)
            .map(o => ({ gameId: (o.m || [])[1], data: o.data}));

        return groupByX(xx, o => o.gameId.toLowerCase(), o => { 
            const d = o[0].data;
            return { frame: (<{x:number, y:number, w: number, h: number}>d["frame"]), pivot: (<{x:number, y:number}>d["pivot"])};
        });
    };

    const lookup = createLookup();

    const getGameImage = (id: string) => {
        const o = lookup[id.toLowerCase()];
        if (o != null) {
            // const clip = `clip-path: rect(${o.frame.x}px, ${o.frame.y}px, ${o.frame.w}px, ${o.frame.h}px);`;
            // console.log(pts); // {l: 810, r: 1010, t: 391, b: 591}
            const pts = { l: o.frame.x, r: o.frame.x + o.frame.w, t: o.frame.y, b: o.frame.y + o.frame.h};
            //const pts = { l: 270, r: 320, t: 10, b: 80 };
            // translate(-${pts.l * 0}px, ${pts.t * 0}px) 
            // transform: scale(1); 
            const scale = 0.15;
            const transform = `transform: translate(-${pts.l * 1}px, -${pts.t * 1}px) scale(${scale});`;
            const color = `background-color:#3399ee;`;
            const clip = `clip-path: polygon(${pts.l}px ${pts.t}px, ${pts.r}px ${pts.t}px, ${pts.r}px ${pts.b}px, ${pts.l}px ${pts.b}px , ${pts.l}px ${pts.t}px);`;
            return `<img src='../cleanUIAssets.png' style='${transform} ${color} ${clip}'></img>`;
        } else {
            //console.log(":(", id);
            return "";
        }
    };
    
    const getColor = (item: any) =>{
        const msg = item.message;
        if (msg != null) {
            const className = msg["className"];
            if (className == "AnswerLogItem") {
                return msg["correct"] ? "#00BB00" : "#EE0000";
            } else if (className == "NewProblemLogItem") {
                return "#0000FF";
            }
        }
        return "#000000";
    }

    const getExerciseName = (msg: any) => {
        const name = ((msg ||{})["exercise"] || "");
        const index = name.indexOf("#");
        return index > 0 ? name.substring(0, index) : name;
    }
    const getSimpleTitle = (item: any) => {
        if (item == null) return "";

        const msg = item.message;
        if (msg != null) {
            const className = msg["className"];
            if (className == "AnswerLogItem") {
                return `${msg["answer"] || ""}`;

            } else if (className == "NewProblemLogItem") {
                return `${msg["problem_string"]} ${msg["problem_type"] || ""} level ${msg["level"]}`;

            } else if (className == "NewPhaseLogItem") {
                return `${getExerciseName(msg)}`; // (day ${msg["training_day"]})`;

            } else if (className == "PhaseEndLogItem") {
                return `PhaseEnd`;

            } else if (className == "LeaveTestLogItem") {
                return `LeaveTest`;

            } else if (className == "EndOfDayLogItem") {
                return `EndOfDay`;
            }
        }
        return JSON.stringify(msg);
    };

    const getTitle = (item: any) => {
        const getPreviousOfType = (t: string) => {
            if (history == null) return null;
            const tmp = history
                .filter(o => (o.message || {})["className"] == t)
                .filter(o => o.time < item.time)
                .toSorted((a, b) => a.time.valueOf() - b.time.valueOf());
            return tmp.length ? tmp[tmp.length - 1] : null
        };

        const msg = item.message;
        if (msg != null) {
            const className = msg["className"];
            if (className == "AnswerLogItem") {
                return `Answer: ${getSimpleTitle(item)} (${getSimpleTitle(getPreviousOfType("NewProblemLogItem"))})`;

            } else if (className == "NewProblemLogItem") {
                return `Question: ${getSimpleTitle(item)} (${getSimpleTitle(getPreviousOfType("NewPhaseLogItem"))})`;

            } else if (className == "NewPhaseLogItem") {
                return `Entered exercise '${getSimpleTitle(item)}''`;
            }
        }
        return getSimpleTitle(item);
    };

    const getIcon = (item: any) => {
        const msg = item.message;
        if (msg != null) {
            const className = msg["className"];
            if (className == "AnswerLogItem") {
                return msg["correct"] ?
                `
<svg xmlns="http://www.w3.org/2000/svg" fill="#00aa00" width="15" height="15" viewBox="0 0 24 24">
    <path d="M19 0h-14c-2.762 0-5 2.239-5 5v14c0 2.761 2.238 5 5 5h14c2.762 0 5-2.239 5-5v-14c0-2.761-2.238-5-5-5zm-8.959 17l-4.5-4.319 1.395-1.435 3.08 2.937 7.021-7.183 1.422 1.409-8.418 8.591z"/>
</svg>
        `:
                `
<svg xmlns="http://www.w3.org/2000/svg" fill="#cc0000" width="18" height="18" clip-rule="evenodd" fill-rule="evenodd" stroke-linejoin="round" stroke-miterlimit="2" viewBox="0 0 24 24">
    <path d="m12.002 21.534c5.518 0 9.998-4.48 9.998-9.998s-4.48-9.997-9.998-9.997c-5.517 0-9.997 4.479-9.997 9.997s4.48 9.998 9.997 9.998zm0-8c-.414 0-.75-.336-.75-.75v-5.5c0-.414.336-.75.75-.75s.75.336.75.75v5.5c0 .414-.336.75-.75.75zm-.002 3c-.552 0-1-.448-1-1s.448-1 1-1 1 .448 1 1-.448 1-1 1z" fill-rule="nonzero"/>
</svg>
`;

            } else if (className == "NewProblemLogItem") {
                return `
<svg xmlns="http://www.w3.org/2000/svg" fill="#6699ff" width="12" height=12" viewBox="0 0 24 24">
    <path d="M12 0c-6.627 0-12 5.373-12 12s5.373 12 12 12 12-5.373 12-12-5.373-12-12-12zm0 18.25c-.69 0-1.25-.56-1.25-1.25s.56-1.25 1.25-1.25c.691 0 1.25.56 1.25 1.25s-.559 1.25-1.25 1.25zm1.961-5.928c-.904.975-.947 1.514-.935 2.178h-2.005c-.007-1.475.02-2.125 1.431-3.468.573-.544 1.025-.975.962-1.821-.058-.805-.73-1.226-1.365-1.226-.709 0-1.538.527-1.538 2.013h-2.01c0-2.4 1.409-3.95 3.59-3.95 1.036 0 1.942.339 2.55.955.57.578.865 1.372.854 2.298-.016 1.383-.857 2.291-1.534 3.021z"/>
</svg>`;
            } else if (className == "NewPhaseLogItem") {
                return `<span style="font-family:ui-monospace; font-size: x-small; font-weight: 800;">${getExerciseName(msg)}</span>`; 
                //return getGameImage(msg["exercise"]);
            } else {
                // console.log("??", msg);
            }
        }
        return `
<svg xmlns="http://www.w3.org/2000/svg" fill="#6699ff" width="18" height="18" clip-rule="evenodd" fill-rule="evenodd" stroke-linejoin="round" stroke-miterlimit="2" viewBox="0 0 24 24">
    <path d="m12 5.72c-2.624-4.517-10-3.198-10 2.461 0 3.725 4.345 7.727 9.303 12.54.194.189.446.283.697.283s.503-.094.697-.283c4.977-4.831 9.303-8.814 9.303-12.54 0-5.678-7.396-6.944-10-2.461z" fill-rule="nonzero"/>
</svg>`;

    };

</script>

<style>
    .tooltip {
      position: relative;
      display: inline-block;
      /*border-bottom: 1px dotted black;  If you want dots under the hoverable text */
    }
 
    .tooltip .tooltiptext {
      visibility: hidden;
      width: 120px;
      background-color: black;
      color: #fff;
      text-align: center;
      padding: 5px 0;
      border-radius: 6px;
     
      position: absolute;
      z-index: 1;
    }
    
    .tooltip:hover .tooltiptext {
      visibility: visible;
    }
</style>

{#each visibleItems as item}
<span class="tooltip" style="color: {getColor(item.item)}; {item.position}">
    {@html getIcon(item.item)}
    <span class="tooltiptext">{getTitle(item.item)}</span>
</span>
{/each}
