// import { minidenticon } from "minidenticons";

export class Avatar {
    static getHash(str: string, max?: number | null, offset: number = 0) {
        let hash = 0,
            i, chr;
        if (str.length === 0) return hash;
        for (i = 0; i < str.length; i++) {
            chr = str.charCodeAt(i);
            hash = ((hash << 5) - hash) + chr;
            hash |= 0; // Convert to 32bit integer
        }
        hash += offset;
        if (max != null)
            return (hash % max + max) % max;
        return hash;
    }

    static createPointArray(id: string): {x: number, y:number}[] {
        const hash = Avatar.getHash(id);
        return [...Array(25)]
            .map((e, i) => hash & (1 << (i % 15)) ? { x: i > 14 ? 7 - ~~(i / 5) : ~~(i / 5), y: i % 5 } : null)
            .filter(o => o != null)
            .map(o => ({x: o?.x || 0, y: o?.y || 0}));
    }

    static create(id: string) {
        const sizeFact = 0.5;
        const orgSize = 8;
        const midY = (orgSize / 2 - 1) * sizeFact;
        const array = Avatar.createPointArray(id).map(xy => ({ x: xy.x * sizeFact, y: xy.y * sizeFact}));
        // const rx = /x=\"(\d+)\" y=\"(\d+)\"/g;
        // const array = [...minidenticon(id).matchAll(rx)].map(o => ({ x: parseFloat(o[1]), y: parseFloat(o[2])}))
        //     .map(xy => ({ x: xy.x * sizeFact, y: xy.y * sizeFact}));
    
        const getMinAndDiff = (arr: number[]) => {
            const min = Math.min.apply(null, arr);
            return { min: min, diff: Math.max.apply(null, arr) - min };
        }
        const getBounds = (arr: {x: number, y: number}[]) => {
            const x = getMinAndDiff(array.map(o => o.x));
            const y = getMinAndDiff(array.map(o => o.y));
            return { x: x.min, y: y.min, width: x.diff, height: y.diff };
        }
        const toRect = (x: number, y: number, size: number = sizeFact) => `<rect x="${x}" y="${y}" width="${size}" height="${size}"></rect>`;
        const createGroup = (id: string, content: string, attrs: string) => `<g filter="url(#outline)" id=${id} ${attrs}>${content}</g>`;
        const createSection = (id: string, coords: {x:number, y:number}[], attrs: string) => createGroup(id, coords.map(o => toRect(o.x, o.y)).join(""), attrs);
    
        const h = Avatar.getHash(id, 360);
        const s = Avatar.getHash(id, 50) + 30;
    
        const sections =
            `<defs>
                <filter id="outline">
    <feMorphology operator="dilate" in="SourceAlpha" radius="0.1"/>
    <feComposite in="SourceGraphic"/>
    </filter></defs>` +
            createSection("top", array.filter(o => o.y < midY), `fill="hsl(${Avatar.getHash(id.substring(0, 4), 360)}, ${100}%, 50%)"`) +
            createSection("bottom", array.filter(o => o.y >= midY), `fill="hsl(${Avatar.getHash(id.substring(0, 6), 360)}, ${100}%, 50%)"`);
    
        const radius = [0, 30, 50][Avatar.getHash(id, 3)];
        const style = `
    border-radius: ${radius}%;
    background-color: hsl(${h}, ${s}%, 80%);
    height: 24px;
    width: 24px;
    display: inline-flex;`.replace(/\n/, "");
    
        const oo = -((orgSize * sizeFact - 1) / 2) / 2;
        const stroke = ""; //`stroke="black" stroke-opacity="0.8" stroke-width="${0.1 * sizeFact}"`;
        const viewBox = `viewBox="${oo} ${oo} ${orgSize * sizeFact} ${orgSize * sizeFact}"`;
    
        const svg = `<svg ${viewBox} ${stroke} xmlns="http://www.w3.org/2000/svg">${sections}</svg>`;
    
        // transform: scale(0.6); 
        return `
    <div style="${style}">
    ${svg}
    </div>`;
    }
}

