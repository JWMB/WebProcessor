<script lang="ts">
  import { apiFacade as apiFacadeStore } from './globalStore';
  import { get } from 'svelte/store';
  import Chart from 'chart.js/auto'; // automatically register plugins so we don't have to elsewhere
  //import { Chart, registerables } from 'chart.js' // see https://stackoverflow.com/questions/67060070/chart-js-core-js6162-error-error-line-is-not-a-registered-controller
  import type { Account } from './apiClient';
  import { onMount } from 'svelte';

  const apiFacade = get(apiFacadeStore);

  let accounts: Account[] = [];
  const getAccounts = async() => {
    accounts = await apiFacade.accounts.get(0, 10, "latest", true);
  };

  onMount(() => getAccounts())
</script>

<main>
  <div>
    {#each accounts as account}
    <div>
      <a href="/?id={account.id}">{account.id}</a>{account.numDays} {account.latest}
    </div>
	  {/each}
  </div>
</main>

<style>
</style>