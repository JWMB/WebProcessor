export class DateUtils {
    public static toDate(val: string | Date | number) {
        switch (typeof val) {
            case "string": return new Date(val);
            case "number": return new Date(val);
            default: return val;
        }
    }

    public static equals(val1: string | Date | number, val2: string | Date | number, allowedDiffMs: number = 0) {
        const d1 = DateUtils.toDate(val1).valueOf();
        const d2 = DateUtils.toDate(val2).valueOf();
        return Math.abs(d1 - d2) <= allowedDiffMs;
    }
    public static toIsoDate(val: string | Date | number) {
        const date = DateUtils.toObject(val);
        return `${date.year}-${DateUtils.pad2(date.month)}-${DateUtils.pad2(date.day)}`;
    }

    public static toObject(val: string | Date | number) {
        const date = DateUtils.toDate(val);
        return { year: date.getFullYear(), month: date.getMonth() + 1, day: date.getDate(), hour: date.getHours(), minute: date.getMinutes(), second: date.getSeconds(), millisecond: date.getMilliseconds() };
    }

    public static pad2(val: string | number) {
        return val.toString().padStart(2, '0');
    }

    public static toDayMonth(val: string | Date | number) {
        const date = DateUtils.toObject(val);
        return `${date.day}/${date.month}`;
    }

    public static getWeekDay(val: string | Date | number) {
        return DateUtils.toDate(val).getDay();
    }
    public static getWeekDayName(val: string | Date | number) {
        return ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"][DateUtils.getWeekDay(val)];
    }

    public static getDatePart(val: string | Date | number) {
        const date = DateUtils.toDate(val);
		const msOfTime = date.getHours() * 60 * 60 * 1000 + date.getMinutes() * 60 * 1000 + date.getSeconds() * 1000 + date.getMilliseconds();
		return new Date(date.valueOf() - msOfTime);
    }
    
    public static addDays(val: string | Date | number, days: number) {
		return new Date(DateUtils.toDate(val).valueOf() + days * 24 * 60 * 60 * 1000);
    }
    
    public static getDaysBetween(dateStart: string | Date | number, dateEnd: string | Date | number, floor:boolean = true) {
        const diff = DateUtils.toDate(dateEnd).valueOf() - DateUtils.toDate(dateStart).valueOf();
        const days = diff / 1000 / 60 / 60 / 24;
        return floor ? Math.floor(days) : days;
	}
}
