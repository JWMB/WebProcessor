export class DateUtils {
    public static toDate(val: string | Date | number) {
        switch (typeof val) {
            case "string": return new Date(val);
            case "number": return new Date(val);
            default: return val;
        }
    }
    public static toIsoDate(val: string | Date | number) {
        const date = DateUtils.toDate(val);
        return `${date.getFullYear()}-${(date.getMonth() + 1).toString().padStart(2,'0')}-${(date.getDate() + 1).toString().padStart(2, '0')}`;
    }
}