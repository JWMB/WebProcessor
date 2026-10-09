<script lang="ts">
	import { onMount } from "svelte";
	import TrainingDaysChart from "src/components/trainingDaysChart.svelte";
	import { groupBy, groupByToKeyValue, max, min, sum } from "../../../arrayUtils";
	import { DateUtils } from "../../../utilities/DateUtils";
	import { type PatchTrainingDto, type PhaseStatistics, type Training, type TrainingDayAccount } from "../../../apiClient";
	import { ApiFacade } from "../../../apiFacade";
	import { getApi, userStore } from "../../../globalStore";

	const apiFacade = getApi() as ApiFacade;

    let trainingId: number;

    let training: Training;
    let trainingDays: TrainingDayAccount[] = [];
    let phaseStatistics: PhaseStatistics[] = [];
    
    let patchDto: PatchTrainingDto = {};

    const currentRole = $userStore?.role || "";
    const canEdit = currentRole.indexOf("Admin") >= 0;

    let phasesByExercise: {exercise: string, phases: PhaseStatistics[]}[] = [];
    let maxDay = 0;
    let table = { header: [""], rows: [[""]]};

    function getStats(phases: PhaseStatistics[]) {
        return { 
            maxLevel: phases == null ? -1 : max(phases.map(o => o.level_max)),
            problems: phases == null ? -1 : sum(phases.map(o => o.num_questions)),
            accuracy: phases == null ? -1 : sum(phases.map(o => o.num_questions)) == 0 ? "" : `${Math.round(100 * sum(phases.map(o => o.num_correct_first_try)) / (sum(phases.map(o => o.num_questions)) ?? 1))}%`,
        };
    }

    function getPatchFromTraining(t: Training) {
        return <PatchTrainingDto>{
            ageBracket: t.ageBracket,
            timeLimit: t.settings.timeLimits[0],
            trainingPlanName: t.trainingPlanName
        };
    }

    const loadData = async () => {
		[trainingDays, phaseStatistics, training] = await Promise.all([
                apiFacade.aggregates.trainingDayAccount(trainingId),
				apiFacade.aggregates.phaseStatistics(trainingId),
				apiFacade.trainings.getById(trainingId)
			]);

        patchDto = getPatchFromTraining(training);

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
            ["Date"].concat(dayArray.map(o => DateUtils.toDayMonth(dayStartEnd[o.toString()]?.start ?? "1900-01-01"))),
            ["Total time"].concat(dayArray.map(o => { const d = trainingDays.find(p => p.trainingDay == o); return d == null ? "" : (d.responseMinutes + d.remainingMinutes).toString(); })),
            ["Response time"].concat(dayArray.map(o => { const d = trainingDays.find(p => p.trainingDay == o); return d == null ? "" : (d.responseMinutes).toString(); })),
            ["-----"],
        ];

        phasesByExercise.forEach(kv => {
            rows.push([kv.exercise]);
            const statsPerDay = dayArray.map(day => getStats(byDay[day]?.filter(o => o.exercise == kv.exercise)));
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

    function patchTraining(dto: PatchTrainingDto) {
        const current = getPatchFromTraining(training);
        Object.keys(dto).forEach(k => {
            if ((<any>current)[k] == (<any>dto)[k]) {
                (<any>dto)[k] = null;
            }
        });
        console.log("patching", dto);
        if (Object.keys(dto).length == 0) return;
        apiFacade.trainings.patch(training.id, dto);
    }
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
            Time limit: <input type="number" readonly={!canEdit} bind:value={patchDto.timeLimit} /> 
        </div>
        <div>
            Age bracket: {training.ageBracket}
        </div>
        <div>
            Training plan: <input type="text" readonly={!canEdit} bind:value={patchDto.trainingPlanName} /> 
        </div>
        {#if canEdit}
        <button on:click={() => patchTraining(patchDto)}>Submit</button>
        {/if}

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