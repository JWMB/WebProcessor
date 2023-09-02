<script lang="ts">
	import { onMount } from "svelte";
	import { getApi } from 'src/globalStore';
	import type { ApiFacade } from 'src/apiFacade';
	import { groupBy, groupByToKeyValue, max, min, sum } from "src/arrayUtils";
	import type { PhaseStatistics, Training, TrainingDayAccount } from "src/apiClient";
	import { DateUtils } from "src/utilities/DateUtils";
	import TrainingDaysChart from "src/components/trainingDaysChart.svelte";

	const apiFacade = getApi() as ApiFacade;

    let trainingId: number;

    let training: Training;
    let trainingDays: TrainingDayAccount[] = [];
    let phaseStatistics: PhaseStatistics[] = [];

    let phasesByExercise: {exercise: string, phases: PhaseStatistics[]}[] = [];
    let maxDay = 0;
    let table = { header: [""], rows: [[""]]};

    function getStats(phases: PhaseStatistics[]) {
        return { 
            maxLevel: max(phases.map(o => o.level_max)),
            problems: sum(phases.map(o => o.num_questions)),
            accuracy: sum(phases.map(o => o.num_questions)) == 0 ? "" : `${Math.round(100 * sum(phases.map(o => o.num_correct_first_try)) / (sum(phases.map(o => o.num_questions)) ?? 1))}%`,
        };
    }

    const loadData = async () => {
		[trainingDays, phaseStatistics, training] = await Promise.all([
                apiFacade.aggregates.trainingDayAccount(trainingId),
				apiFacade.aggregates.phaseStatistics(trainingId),
				apiFacade.trainings.getById(trainingId)
			]);

        phaseStatistics = phaseStatistics.map(o => ({...o, exercise: o.exercise.split("#")[0]}));

        phasesByExercise = groupByToKeyValue(phaseStatistics, ps => ps.exercise) //.split("#")[0]
            .sort((a, b) => (a.key < b.key ? -1 : (a.key > b.key ? 1 : 0)))
            .map(o => ({ exercise: o.key, phases: o.value }));

        const byDay = groupBy(phaseStatistics, ps => ps.training_day.toString());

        const dayStartEnd = Object.fromEntries(Object.entries(byDay).map(o => (
            [o[0],
             {
                start: min(o[1].map(p => DateUtils.toDate(p.timestamp).valueOf())),
                end: min(o[1].map(p => DateUtils.toDate(p.end_timestamp).valueOf())),
            }]
        )));
        maxDay = max(phaseStatistics.map(o => o.training_day));
        const dayArray = Array.from(Array(maxDay).keys()).map(o => o + 1);

        const header = [""].concat(dayArray.map(o => o.toString()));

        const rows = [
            ["Date"].concat(dayArray.map(o => DateUtils.toDayMonth(dayStartEnd[o.toString()].start))),
            ["Total time"].concat(dayArray.map(o => { const d = trainingDays.find(p => p.trainingDay == o); return d == null ? "" : (d.responseMinutes + d.remainingMinutes).toString(); })),
            ["Response time"].concat(dayArray.map(o => { const d = trainingDays.find(p => p.trainingDay == o); return d == null ? "" : (d.responseMinutes).toString(); })),
            ["-----"],
        ];

        phasesByExercise.forEach(kv => {
            const statsPerDay = dayArray.map(day => getStats(byDay[day].filter(o => o.exercise == kv.exercise)));
            rows.push([kv.exercise]);
            Object.keys(statsPerDay[0]).forEach(key => {
                rows.push([`--${key}`].concat(statsPerDay.map(o => (!(<any>o)[key] ? "" : (<any>o)[key].toString()))));
            });
        });
        table = { header: header, rows: rows };
	};

	onMount(() => {
        const parm = new URLSearchParams(window.location.search).get('id');
        if (!parm) throw new Error("Id missing");
		trainingId = parseFloat(parm);
        if (!trainingId) throw new Error("Id missing");

        loadData();
	});
</script>

<h1>NOTE: temporary view</h1>
<h4>
    This page is a very simple view of training data. We will make it user-friendly when time and resources so allow.
</h4>

<div>
    {#if !!training}
    <div>
        Training: {training.username}
    </div>
    <div>
        Time limit: {training.settings?.timeLimits[0] || "N/A"}
    </div>
    <div>
        Age bracket: {training.ageBracket}
    </div>

    {#if !!trainingDays}
    <TrainingDaysChart data={trainingDays} />
    {/if}

    {#if !!phasesByExercise}
    <table>
        {#each table.header as th}
        <th>{th}</th>
        {/each}
        {#each table.rows as tr}
        <tr>
            {#each tr as td}
            <td>{td}</td>
            {/each}
        </tr>
        {/each}
    </table>
    {/if}
    {/if}
</div>