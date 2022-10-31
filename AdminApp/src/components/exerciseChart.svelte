<script lang="ts">
  import { Chart } from "chart.js";
  import { onMount } from "svelte";

  let chart: Chart;
  export let data: {
    exercise: string;
    days: {
        day: number;
        maxLevel: number;
        totalResponseTime: number;
    }[];
    maxDay: number;
    };

    const createChart = () => {
        if (!!chart) return;
        const context = (<HTMLCanvasElement>document.getElementById(`chart_${data.exercise}`)).getContext("2d");
        chart = new Chart(context, 
          {
            type: 'scatter',
            data: { 
              //labels: data.days.map(o => o.day),
              datasets: [ 
              {
                label: 'Max level',
                backgroundColor: 'rgb(255, 99, 132)',
                borderColor: 'rgb(255, 99, 132)',
                data: data.days.map(o => ({ x: o.day, y: o.maxLevel })),
                yAxisID: "y"
              },
              {
                label: 'Response time',
                backgroundColor: 'rgb(132, 255, 99)',
                borderColor: 'rgb(132, 255, 99)',
                data: data.days.map(o => ({ x: o.day, y: o.totalResponseTime / 60 / 1000})),
                yAxisID: "y1"
              },
            ]},
            options: {
              plugins: {
                legend: { display: true, position: "left" },
                title: { display: true, text: data.exercise }
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
                  suggestedMax: data.maxDay,
                  max: data.maxDay,
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
        };

    const update = () => {
        createChart();
        chart.update();
    }

    $: {
        console.log("ooo", data, chart, mounted);
        if (!!data && mounted) {
            update();
        }
    }

    let mounted = false;
    onMount(() => {
        mounted = true;
        console.log("xxx", data, chart);
        if (!!data) {
            update();
        }
    });
</script>

<main>
    <canvas id="chart_{data.exercise}" width="800" height="200"></canvas>
</main>