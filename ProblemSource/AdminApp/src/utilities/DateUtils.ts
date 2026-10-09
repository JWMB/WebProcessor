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

    public static getIntDaysBetween(dateStart: string | Date | number, dateEnd: string | Date | number) {
        const diff = DateUtils.getDatePart(DateUtils.toDate(dateEnd)).valueOf() - DateUtils.getDatePart(DateUtils.toDate(dateStart)).valueOf();
        const days = diff / 1000 / 60 / 60 / 24;
        return Math.round(days);
	}

    public static getDaysBetween(dateStart: string | Date | number, dateEnd: string | Date | number, floor:boolean = true) {
        const diff = DateUtils.toDate(dateEnd).valueOf() - DateUtils.toDate(dateStart).valueOf();
        const days = diff / 1000 / 60 / 60 / 24;
        return floor ? Math.floor(days) : days;
    }
    
    public static getMsBetween(dateStart: string | Date | number, dateEnd: string | Date | number) {
        return DateUtils.toDate(dateEnd).valueOf() - DateUtils.toDate(dateStart).valueOf();
    }

    // public static getTimeDiffCategoryName(categoryId: number) {
    //     return {
    //         0: "Today",
    //         1: "Yesterday",
    //         2: "This week",
    //         3: "This month",
    //         4: "Last month",
    //         5: "Old"
    //     }[categoryId] || "";
    // }

    public static getTimeDiffCategory(intervalMs: number): { msRounded: number, name: string} {
        const hour = 1000 * 60 * 60; 
        const day = hour * 24;
        // const hours = intervalMs / 1000 / 60 / 60; 
        const days = intervalMs / day;
        if (hour <= 1)
            return { msRounded: hour * 1, name: "Last hour" };
        if (hour <= 2)
            return { msRounded: hour * 2, name: "Last 2 hours" };
        if (hour <= 4)
            return { msRounded: hour * 4, name: "Last 4 hours" };
        if (days <= 1)
            return { msRounded: day * 1, name: "Today" };
        if (days <= 2)
            return { msRounded: day * 2, name: "Yesterday" };
        if (days <= 7)
            return { msRounded: day * 7, name: "This week" };
        if (days <= 30)
            return { msRounded: day * 30, name: "This month" };
        if (days <= 60)
            return { msRounded: day * 60, name: "Last month" };
        return { msRounded: day * 365, name: "Old" };
    }
}
