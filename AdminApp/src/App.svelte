<script lang="ts">
  import { apiFacade as apiFacadeStore } from './globalStore';
  import { get } from 'svelte/store';
  import Chart from 'chart.js/auto';
  //import { Chart, registerables } from 'chart.js' // see https://stackoverflow.com/questions/67060070/chart-js-core-js6162-error-error-line-is-not-a-registered-controller
  import { onMount } from 'svelte';
  import type { Account } from './apiClient';

  const apiFacade = get(apiFacadeStore);

  let chart: Chart | null = null;

  let accounts: Account[] = [];
  const getAccounts = async() => {
    accounts = await apiFacade.accounts.get(0, 20, "latest", true);
  };

  const accountId = 715955; //644507
  const loadData = async () => {

    const [trainingDays, phaseStatistics] = await Promise.all([
      apiFacade.aggregates.trainingDayAccount(accountId),
      apiFacade.aggregates.phaseStatistics(accountId) 
    ]);

    const result = await apiFacade.aggregates.trainingDayAccount(accountId);

    chart.data = {
      labels: trainingDays.map((o, i) => i.toString()),
      datasets: [
        {
          label: 'Response',
          backgroundColor: 'rgb(255, 99, 132)',
          borderColor: 'rgb(255, 99, 132)',
          data: result.map(o => o.responseMinutes)
        },
        {
          label: 'Total',
          backgroundColor: 'rgb(132, 99, 255)',
          borderColor: 'rgb(132, 99, 255)',
          data: result.map(o => o.responseMinutes + o.remainingMinutes)
        }
      ]
    };
    chart.update();
  };

  onMount(() => {
    const context = (<HTMLCanvasElement>document.getElementById('myChart')).getContext("2d");
    chart = new Chart(context, 
      {
        type: 'line',
        data: { labels: [], datasets: [] },
        options: {}
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
    {#each accounts as account}
		<li>
			{account.id} {account.numDays} {account.latest}
		</li>
	{/each}
  </div>
</main>

<style>
</style>