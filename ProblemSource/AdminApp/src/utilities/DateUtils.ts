export class DateUtils {
    public static toDate(val: string | Date | number) {
        switch (typeof val) {
            case "string": return new Date(val);
            case "number": return new Date(val);
            default: return val;
        }
    }
    public static toIsoDate(val: string | Date | number) {
        const date = DateUtils.toObject(val);
        return `${date.year}-${DateUtils.pad2(date.month)}-${DateUtils.pad2(date.day)}`;
    }

    public static toObject(val: string | Date | number) {
        const date = DateUtils.toDate(val);
        return { year: date.getFullYear(), month: date.getMonth() + 1, day: date.getDate() + 1, hour: date.getHours(), minute: date.getMinutes(), second: date.getSeconds(), millisecond: date.getMilliseconds() };
    }

    public static pad2(val: string | number) {
        return val.toString().padStart(2, '0');
    }

    public static toDayMonth(val: string | Date | number) {
        const date = DateUtils.toObject(val);
        return `${date.day}/${date.month}`;
    }
}
