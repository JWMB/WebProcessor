<script lang="ts">
    import { apiFacade as apiFacadeStore } from '../../globalStore';
    import { get } from 'svelte/store';
	// import type { Account } from 'src/apiClient';
	import { onMount } from 'svelte';
    import { base } from '$app/paths';
	import type { Training, TrainingDayAccount, TrainingSummary } from 'src/apiClient';
	import CirclePercentage from '../../components/circlePercentage.svelte';

    const apiFacade = get(apiFacadeStore);

    let trainings: TrainingSummary[] = [];

    function getDateOnly(date: Date)
    {
        const str = date.toISOString();
        const index = str.indexOf("T");
        return str.substring(0, index);
        // const msOfTime = date.getHours() * 60 * 60 * 1000 + date.getMinutes() * 60 * 1000 + date.getSeconds() * 1000 + date.getMilliseconds();
    }
    function getDaysBetween(dateStart: Date, dateEnd: Date) {
        const diff = dateEnd.valueOf() - dateStart.valueOf();
        return Math.floor(diff / 1000 / 60 / 60 / 24);
    }

    let dates: string[] = [];
    let dayArraysPerTraining: { trainingId: number, uuid: string, daysArray: (TrainingDayAccount | null)[] }[] = [];
    const numDaysBack = 10;
    let fromDate = new Date(Date.now() - numDaysBack * 1000 * 60 * 60 * 24);
    const getTrainings = async() => {
        trainings = await apiFacade.trainings.getSummaries();

        const emptyDaysArray = new Array(numDaysBack).fill(null);
        dates = emptyDaysArray.map((o, i) => getDateOnly(new Date(fromDate.valueOf() + i * 24 * 60 * 60 * 1000)))

        dayArraysPerTraining = trainings.map(t => {
            const withDayIndex = t.days.filter(d => new Date(d.startTime) >= fromDate)
                .map(d => ({ dayIndex: getDaysBetween(fromDate, new Date(d.startTime)), info: d}));

            const inArray = emptyDaysArray.map((o, i) => withDayIndex.filter(d => d.dayIndex == i));
            return { trainingId: t.id, uuid: t.days[0]?.accountUuid || "N/A", daysArray: inArray.map(o => o == null || o.length == 0 ? null : o[0].info)};
        });
    };
    onMount(() => getTrainings())
</script>

<div>
    <h1>Trainings</h1>
    <!-- {#each trainings as training}
    <div>
      <a href="{base}/?id={training.id}">{training.id}</a>&nbsp;{training.days.length}&nbsp;
      <CirclePercentage progress=0.5></CirclePercentage>
    </div>
	{/each} -->
    <table>
        <th>
            Id
        </th>
        {#each dates as day}
        <th>{day}<th>
        {/each}
        {#each dayArraysPerTraining as training}
        <tr>
          <a href="{base}/?id={training.trainingId}">{training.uuid}</a>
          {#each training.daysArray as day}
            <td>{Math.round(100 * (day?.numQuestions || 0) / (day?.numCorrectAnswers || 1))}%</td>
            <td>{Math.round((day?.responseMinutes || 0) + (day?.remainingMinutes || 0))}min</td>
          {/each}
          <div style="width: 10px; height: 10px">
            <CirclePercentage progress=0.5></CirclePercentage>
         </div>
        </tr>
        {/each}
    </table>

</div>