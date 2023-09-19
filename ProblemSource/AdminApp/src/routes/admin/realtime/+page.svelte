<script lang="ts">
	import { trainingUpdateStore } from '../../../globalStore.js';
	import { onMount } from 'svelte';
	import Realtimeline from 'src/components/realtimeline.svelte';
	import { RealtimelineTools } from 'src/services/realtimelineTools.js';

    const cutoff = 5 * 60 * 1000;
    const getPositioning = RealtimelineTools.createPositioningFunction(cutoff);

    const rtlTools = new RealtimelineTools(cutoff + 2 * 60 * 1000);
    { // for testing:
        RealtimelineTools.testData().forEach(o => rtlTools.append(o));
    }

    let allData = rtlTools.allData;

    trainingUpdateStore.subscribe(msgs => { 
        msgs.forEach(m => rtlTools.append(m));
    });

    onMount(() => {
        const interval = setInterval(() => {
            rtlTools.clearExpired();
            allData = rtlTools.allData;
        }, 100);
        return () => clearInterval(interval);
    })
</script>

<div>
    <table>
        <tr>
            <th>User</th>
            <!-- <th style="width: 800px;"> -->
            <th>
                {#each [0, 0.5, 1, 2, 3, 4, 5] as minutes}
                    <span style="{getPositioning(new Date(Date.now().valueOf() - minutes * 60 * 1000))}">{minutes}</span>
                {/each}
            </th>
        </tr>
        {#each allData as info}
        <tr>
            <td>{info.username}</td>
            <td>
                <Realtimeline history={info.events} getPositioning={getPositioning}></Realtimeline>
            </td>
        </tr>
    {/each}
    </table>
</div>