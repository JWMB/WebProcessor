import type { TrainingSummaryWithDaysDto } from "src/apiClient";
import { max } from "src/arrayUtils";
import { DateUtils } from "src/utilities/DateUtils";

export interface XX {

}

export class TrainingDayTools {
    public static getLatestNumDaysStats(numDays: number, trainingSummaries: TrainingSummaryWithDaysDto[]) {
        const latestTimestamp = max(trainingSummaries.map((ts) => max(ts.days.filter(d => d.numQuestions > 0).map(d => new Date(d.startTime).valueOf()))));

        let fromDate = DateUtils.getDatePart(DateUtils.addDays(latestTimestamp, -numDays + 1));

        const mappedTrainings = trainingSummaries.map(training => {
            const withDayIndex = training.days
                .filter((d) => new Date(d.startTime) >= fromDate)
                .map((d) => ({
                    dayIndex: DateUtils.getDaysBetween(fromDate, DateUtils.getDatePart(d.startTime)),
                    timeActive: d.responseMinutes,
                    timeTotal: d.remainingMinutes + d.responseMinutes,
                    timeTotalOfTargetPercent: Math.round(100 * (d.remainingMinutes + d.responseMinutes) / (training.targetMinutesPerDay || 10000)),
                    correct: d.numCorrectAnswers / d.numQuestions,
                    winRate: d.numRacesWon / d.numRaces
                }));
            const withEmptyDays = Array.from(Array(numDays).keys()).map(i => withDayIndex.find(o => o.dayIndex == i) ?? {});
        
            const firstDay = training.days[0];
            // const lastDay = training.days[training.days.length - 1] || firstDay;
            const uuid = training.username; // firstDay.accountUuid;
            const daysSinceStart = firstDay ? DateUtils.getDaysBetween(new Date(firstDay.startTime), new Date(latestTimestamp)) : 1;
            // console.log(uuid, training.days.length, daysSinceStart);
            return {
                id: training.id,
                uuid: uuid,
                startDate: firstDay ? firstDay.startTime : null,
                totalDays: training.days.length,
                firstDate: firstDay ? DateUtils.toIsoDate(new Date(firstDay.startTime)) : "",
                latestDate: firstDay ? DateUtils.toIsoDate(new Date(training.days[training.days.length - 1].startTime)) : "",
                daysPerWeek: training.days.length / (daysSinceStart / 7),
                days: withEmptyDays,
                targetTime: training.targetMinutesPerDay
            };
        });

        return { trainings: mappedTrainings, startDate: fromDate };
    }
}