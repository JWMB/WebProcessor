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

    function getDateHeader(date: Date) {
        const days = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];
        return days[date.getDay()].substring(0, 1);
    }

    let dateHeaders: {text:string, tooltip:string}[] = [];
    let dayArraysPerTraining: { trainingId: number, uuid: string, daysArray: (TrainingDayAccount | null)[] }[] = [];
    const numDaysBack = 10;
    let fromDate = new Date(Date.now() - numDaysBack * 1000 * 60 * 60 * 24);
    const getTrainings = async() => {
        trainings = await apiFacade.trainings.getSummaries();

        const emptyDaysArray = new Array(numDaysBack).fill(null);
        dateHeaders = emptyDaysArray.map((o, i) => {
            const d = new Date(fromDate.valueOf() + i * 24 * 60 * 60 * 1000);
            return {
                tooltip: getDateOnly(d),
                text: getDateHeader(d)
            };
        });

        dayArraysPerTraining = trainings.map(t => {
            const withDayIndex = t.days.filter(d => new Date(d.startTime) >= fromDate)
                .map(d => ({ dayIndex: getDaysBetween(fromDate, new Date(d.startTime)), info: d}));

            const inArray = emptyDaysArray.map((o, i) => withDayIndex.filter(d => d.dayIndex == i));
            const result = { trainingId: t.id, uuid: t.days[0]?.accountUuid || "N/A", daysArray: inArray.map(o => o == null || o.length == 0 ? null : o[0].info)};
            return result;
        });
    };
    onMount(() => getTrainings())
</script>

<div>
    <h1>Trainings</h1>
    <table>
        <tr>
            <th>
                Id
            </th>
            {#each dateHeaders as header}
            <th>{header.text}</th>
            {/each}
        </tr>
        {#each dayArraysPerTraining as training}
        <tr>
          <td>
            <a href="{base}/?id={training.trainingId}">{training.uuid}</a>
          </td>
          {#each training.daysArray as day}
            <td>
                {Math.round(100 * (day?.numQuestions || 0) / (day?.numCorrectAnswers || 1))}%
            </td>
          {/each}
          <!-- <div style="width: 10px; height: 10px">
            <CirclePercentage progress=0.5></CirclePercentage>
         </div> -->
        </tr>
        <tr>
            <td></td>
            {#each training.daysArray as day}
              <td>
                {Math.round((day?.responseMinutes || 0) + (day?.remainingMinutes || 0))}
              </td>
            {/each}
            <!-- <div style="width: 10px; height: 10px">
              <CirclePercentage progress=0.5></CirclePercentage>
           </div> -->
        </tr>
        {/each}
    </table>

</div>