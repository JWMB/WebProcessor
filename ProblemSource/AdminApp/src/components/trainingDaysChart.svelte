<script lang="ts">
  import { Chart } from "chart.js";
  import type { TrainingDayAccount } from "src/apiClient";
  import { onMount } from "svelte";

  export let data: TrainingDayAccount[];
  let chart: Chart;

  const update = () => {
    chart.data = {
      labels: data.map(o => o.trainingDay.toString()),
      datasets: [
        {
          label: 'Response',
          backgroundColor: 'rgb(255, 99, 132)',
          borderColor: 'rgb(255, 99, 132)',
          data: data.map(o => o.responseMinutes)
        },
        {
          label: 'Total',
          backgroundColor: 'rgb(132, 99, 255)',
          borderColor: 'rgb(132, 99, 255)',
          data: data.map(o => Math.min(100, o.responseMinutes + o.remainingMinutes))
        }
      ]
    };    
    chart.update();
  };

  $: {
    if (data?.length > 0) {
        update();
    }
  }

  onMount(() => {
    const context = (<HTMLCanvasElement>document.getElementById('myChart')).getContext("2d");
    chart = new Chart(context, 
      {
        type: 'line',
        data: { labels: [], datasets: [] },
        options: {
          plugins: {
            legend: { display: true, position: "left" },
          },
          animation: false
        }
      });
      
      if (data?.length > 0) update();
  });
</script>

<main>
    <div>
      <canvas id="myChart" width="800" height="400"></canvas>
    </div>
</main>  