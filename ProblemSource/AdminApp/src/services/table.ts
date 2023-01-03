export class TableDef {
    rows: any[] = [];

    getHeaderRows(): TableHeaderCell[][] {
        return [
            [{ colSpan: 1, text: ""}]
        ];
    }

    getBodyRows(): TableHeaderCell[][] {
        return [
            [{ colSpan: 1, text: ""}]
        ];
    }

    static create(rows: any[], columns: any[] | any) {
        for (const iterator of TableDef.flatten(columns, [])) {
            console.log(iterator);
        }
    }

    static *flatten(parent: any, path: string[]): Generator<{path:string[], value: any}> {
        function *handle(p: string[], key: string, value: any) {
            path.push(key);
            yield * TableDef.flatten(value, path);
            path.pop();
        }
        if (Array.isArray(parent)) {
            for (let index = 0; index < parent.length; index++) {
                yield * handle(path, `[${index}]`, parent[index]);
                // path.push(`[${index}]`);
                // yield * TableDef.flatten(parent[index], path);
                // path.pop();
            }
        } else if (parent == null) {
        } else if (typeof parent === "object") {
            const keys = Object.keys(parent);
            for (const key of keys) {
                yield * handle(path, key, parent[key]);
                // path.push(key);
                // yield * TableDef.flatten(parent[key], path);
                // path.pop();
            }
        } else {
            yield { path: path.slice(), value: parent };
        }
    }
}

export interface TableHeaderCell {
    colSpan: number;
    text: string;
    tooltip?: string;
}
