<script lang="ts">
    import { apiFacade as apiFacadeStore } from '../../globalStore';
    import { get } from 'svelte/store';
	import type { Account } from 'src/apiClient';
	import { onMount } from 'svelte';

    const apiFacade = get(apiFacadeStore);

    let accounts: Account[] = [];
    const getAccounts = async() => {
        accounts = await apiFacade.accounts.get(0, 10, "latest", true);
    };
    onMount(() => getAccounts())
</script>

<div>
    {#each accounts as account}
    <div>
      <a href="/?id={account.id}">{account.id}</a>&nbsp;{account.numDays}&nbsp;{account.latest}
    </div>
	{/each}
</div>