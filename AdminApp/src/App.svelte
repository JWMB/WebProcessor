<script lang="ts">
  import { apiFacade as apiFacadeStore } from './globalStore';
  import { get } from 'svelte/store';
  import Chart from 'chart.js/auto';
  //import { Chart, registerables } from 'chart.js' // see https://stackoverflow.com/questions/67060070/chart-js-core-js6162-error-error-line-is-not-a-registered-controller
  import { afterUpdate, onMount } from 'svelte';
  import type { Account } from './apiClient';

  const apiFacade = get(apiFacadeStore);

  let chart: Chart | null = null;

  let perExerciseChartData: { exercise: string, days: {day: number, maxLevel: number, totalResponseTime: number}[]}[] = [];

  let accounts: Account[] = [];
  const getAccounts = async() => {
    accounts = await apiFacade.accounts.get(0, 20, "latest", true);
  };

  function groupBy<T>(xs: T[], groupFunc: (val: T) => string): {[key: string]: T[]} {
      return xs.reduce(function(rv, curr) {
        (rv[groupFunc(curr)] = rv[groupFunc(curr)] || []).push(curr);
        return rv;
      }, {});
  };
  const max = (xs: number[]) => xs.reduce((p, c) => p > c ? p : c); 
  const sum = (xs: number[]) => xs.reduce((p, c) => p + c); 
  const min = (xs: number[]) => xs.reduce((p, c) => p > c ? c : p); 

  let gotChartData = false;
  afterUpdate(() => {
    if (gotChartData) {
      gotChartData = false;

      const maxDay = max(perExerciseChartData.map(o => max(o.days.map(p => p.day))));
      for (const iterator of perExerciseChartData) {
        const context = (<HTMLCanvasElement>document.getElementById(`chart_${iterator.exercise}`)).getContext("2d");
        const chart = new Chart(context, 
          {
            type: 'scatter',
            data: { 
              //labels: iterator.days.map(o => o.day),
              datasets: [ 
              {
                label: 'Max level',
                backgroundColor: 'rgb(255, 99, 132)',
                borderColor: 'rgb(255, 99, 132)',
                // data: iterator.days.map(o => o.maxLevel),
                data: iterator.days.map(o => ({ x: o.day, y: o.maxLevel })),
                yAxisID: "y"
              },
              {
                label: 'Response time',
                backgroundColor: 'rgb(132, 255, 99)',
                borderColor: 'rgb(132, 255, 99)',
                // data: iterator.days.map(o => o.totalResponseTime / 60 / 1000),
                data: iterator.days.map(o => ({ x: o.day, y: o.totalResponseTime / 60 / 1000})),
                yAxisID: "y1"
              },
            ]},
            options: {
              plugins: {
                legend: { display: true, position: "left" },
                title: { display: true, text: iterator.exercise }
              },
              scales: {
                // xAxes: [{
                //     display: true,
                //     scaleLabel: {
                //         display: true,
                //         labelString: 'Month'
                //     }
                // }],
                x: {
                  min: 0,
                  beginAtZero: true,
                  suggestedMax: maxDay,
                  max: maxDay,
                },
                y: {
                  beginAtZero: true,
                  type: 'linear',
                  display: true,
                  position: 'left',
                },
                y1: {
                  beginAtZero: true,
                  type: 'linear',
                  display: true,
                  position: 'right',
                  grid: {
                    drawOnChartArea: false, // only want the grid lines for one axis to show up
                  },
                }
              }
            }
          });
          chart.update();
        }
      }
  });

  const accountId = 715955; //644507
  const loadData = async () => {
    const [trainingDays, phaseStatistics] = await Promise.all([
      apiFacade.aggregates.trainingDayAccount(accountId),
      apiFacade.aggregates.phaseStatistics(accountId) 
    ]);

    const byExercise = groupBy(phaseStatistics, o => o.exercise.split('#')[0]);

    perExerciseChartData = Object.keys(byExercise).map(key => {
      const phases = byExercise[key];
      const byDay = groupBy(phases, p => `${p.training_day}`);
      return {
        exercise: key,
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
    gotChartData = true;
    // console.log(perExerciseChartData);

    const byDay = groupBy(trainingDays, o => `${o.trainingDay}`);
    const singleTrainingDays = Object.keys(byDay).map(key => byDay[key][0]);
    chart.data = {
      labels: singleTrainingDays.map(o => o.trainingDay.toString()),
      datasets: [
        {
          label: 'Response',
          backgroundColor: 'rgb(255, 99, 132)',
          borderColor: 'rgb(255, 99, 132)',
          data: singleTrainingDays.map(o => o.responseMinutes)
        },
        {
          label: 'Total',
          backgroundColor: 'rgb(132, 99, 255)',
          borderColor: 'rgb(132, 99, 255)',
          data: singleTrainingDays.map(o => Math.min(100, o.responseMinutes + o.remainingMinutes))
        }
      ]
    };

    //chart_timePerExercise
    chart.config.options.scales.y = { stacked: true };
    chart.data.datasets = [];
    Object.keys(byExercise).map(key => {
      const inEx = byExercise[key];
      // TODO: nswag! o.timestamp is a string, not a Date!
      const timeSeries = singleTrainingDays.map(std => {
        const aa = inEx.filter(o => o.training_day == std.trainingDay);
        return aa.length > 0 ? sum(aa.map(o => (new Date(o.end_timestamp).valueOf() - new Date(o.timestamp).valueOf()) / 1000 / 60)) : 0;
      });
      chart.data.datasets.push(
        {
          label: key,
          data: timeSeries,
          //backgroundColor
        }
      );
    });

    chart.update();
  };

  onMount(() => {
    const context = (<HTMLCanvasElement>document.getElementById('myChart')).getContext("2d");
    chart = new Chart(context, 
      {
        type: 'line',
        data: { labels: [], datasets: [] },
        options: {
          plugins: {
            legend: { display: true, position: "left" },
          }
        }
      });
    loadData();
    getAccounts();
  });
</script>

<main>
  <div>
    <canvas id="myChart" width="800" height="400"></canvas>
  </div>

  <div>
    <canvas id="chart_timePerExercise" width="800" height="400"></canvas>
  </div>

  <div>
    {#each perExerciseChartData as exerciseChart}
		<li>
      <canvas id="chart_{exerciseChart.exercise}" width="800" height="200"></canvas>
		</li>
  	{/each}
  </div>

  <div>
    {#each accounts as account}
		<li>
			{account.id} {account.numDays} {account.latest}
		</li>
	{/each}
  </div>
</main>

<style>
</style>