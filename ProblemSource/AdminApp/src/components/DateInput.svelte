<script lang="ts">
	import { DateUtils } from "src/utilities/DateUtils";

    // import { onMount } from 'svelte'
    // export let format = 'YYYY-MM-DD'
    export let date = new Date();
      
    let internal: string;
  
    
    const input = (x: Date) => (internal = DateUtils.toIsoDate(x))
    const output = (x: string) => (date = DateUtils.toDate(x));
  
    $: input(date)
    $: output(internal)
  </script>
  
  <input type="date" bind:value={internal} />
  