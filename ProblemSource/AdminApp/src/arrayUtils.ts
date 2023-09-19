export function groupBy<T>(xs: T[], groupFunc: (val: T) => string): { [key: string]: T[] } {
  return xs.reduce(function (rv, curr) {
    (rv[groupFunc(curr)] = rv[groupFunc(curr)] || []).push(curr);
    return rv;
  }, {});
};

export function groupByX<T, U>(xs: T[], groupFunc: (val: T) => string, valueSelector: (values: T[]) => U): { [key: string]: U } {
  const oo = groupBy<T>(xs, groupFunc);
  const result = {};
  Object.keys(oo).forEach(key => { (<any>result)[key] = valueSelector(oo[key]) });
  return result;
};


export function groupByToKeyValue<T>(xs: T[], groupFunc: (val: T) => string): { key: string, value: T[] }[] {
  return Object.entries(groupBy<T>(xs, groupFunc)).map(o => ({ key: o[0], value:o[1]}));
}
export function groupByToKeyValueX<T, U>(xs: T[], groupFunc: (val: T) => string, valueSelector: (values: T[]) => U): { key: string, value: U }[] {
  return Object.entries(groupBy<T>(xs, groupFunc)).map(o => ({ key: o[0], value: valueSelector((o[1]))}));
}

export function max(xs: number[]) { return xs.length === 0 ? 0 : xs.reduce((p, c) => p > c ? p : c); }
export function min(xs: number[]) { return xs.length === 0 ? 0 : xs.reduce((p, c) => p > c ? c : p); }
export function sum(xs: number[]) { return xs.length === 0 ? 0 : xs.reduce((p, c) => p + c); }
