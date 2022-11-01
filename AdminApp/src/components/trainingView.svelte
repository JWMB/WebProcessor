<script lang="ts">
    import { apiFacade as apiFacadeStore } from '../globalStore';
    import { get } from 'svelte/store';
    import Chart from 'chart.js/auto'; // automatically register plugins so we don't have to elsewhere
    //import { Chart, registerables } from 'chart.js' // see https://stackoverflow.com/questions/67060070/chart-js-core-js6162-error-error-line-is-not-a-registered-controller
    import type { PhaseStatistics, TrainingDayAccount } from '../apiClient';
    import TimePerExerciseAndDayChart from './timePerExerciseAndDayChart.svelte';
    import { groupBy, max } from '../arrayUtils';
    import ExerciseChart from './exerciseChart.svelte';
    import TrainingDaysChart from './trainingDaysChart.svelte';
    import { onMount } from 'svelte';
  
    const apiFacade = get(apiFacadeStore);
  
    let perExerciseChartData: { exercise: string, maxDay: number, days: {day: number, maxLevel: number, totalResponseTime: number}[]}[] = [];
      
    let phaseStatistics: PhaseStatistics[];
    let trainingDays: TrainingDayAccount[];
    let singleTrainingDays: TrainingDayAccount[];
  
    let accountId: number;
    const loadData = async () => {
      [trainingDays, phaseStatistics] = await Promise.all([
        apiFacade.aggregates.trainingDayAccount(accountId),
        apiFacade.aggregates.phaseStatistics(accountId) 
      ]);
  
      const byDay = groupBy(trainingDays, o => `${o.trainingDay}`);
      singleTrainingDays = Object.keys(byDay).map(key => byDay[key][0]);
  
      const byExercise = groupBy(phaseStatistics, o => o.exercise.split('#')[0]);
      perExerciseChartData = Object.keys(byExercise).map(key => {
        const phases = byExercise[key];
        const byDay = groupBy(phases, p => `${p.training_day}`);
        return {
          exercise: key,
          maxDay: max(singleTrainingDays.map(o => o.trainingDay)),
          days: Object.keys(byDay).map(day => {
            const inDay = byDay[day];
            return {
              day: inDay[0].training_day,
              maxLevel: inDay.map(o => o.level_max).reduce((p, c) => p > c ? p : c, 0),
              totalResponseTime: inDay.map(o => o.response_time_total).reduce((p, c) => p + c, 0)
            }
          })
        }
      });
    };
  
    onMount(() => {
      accountId = parseFloat(new URLSearchParams(window.location.search).get("id") ?? "715955")
      loadData();
    });
  </script>
  
  <main>
    <TrainingDaysChart data={singleTrainingDays}></TrainingDaysChart>
    <TimePerExerciseAndDayChart data={phaseStatistics}></TimePerExerciseAndDayChart>
  
    <div>
      {#each perExerciseChartData as exerciseChart}
        <ExerciseChart data={exerciseChart}></ExerciseChart>
        {/each}
    </div>
  </main>
  
  <style>
  </style>