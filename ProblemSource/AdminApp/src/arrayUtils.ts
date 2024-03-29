export function groupBy<T>(xs: T[], groupFunc: (val: T) => string): { [key: string]: T[] } {
  return xs.reduce(function (rv, curr) {
    (rv[groupFunc(curr)] = rv[groupFunc(curr)] || []).push(curr);
    return rv;
  }, {});
};

export function groupByToKeyValue<T>(xs: T[], groupFunc: (val: T) => string): { key: string, value: T[] }[] {
  return Object.entries(groupBy<T>(xs, groupFunc)).map(o => ({ key: o[0], value:o[1]}));
}

export function max(xs: number[]) { return xs.length === 0 ? 0 : xs.reduce((p, c) => p > c ? p : c); }
export function min(xs: number[]) { return xs.length === 0 ? 0 : xs.reduce((p, c) => p > c ? c : p); }
export function sum(xs: number[]) { return xs.length === 0 ? 0 : xs.reduce((p, c) => p + c); }
