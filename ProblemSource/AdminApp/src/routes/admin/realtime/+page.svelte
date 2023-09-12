<script lang="ts">
	import type { TrainingUpdateMessage } from 'src/types.js';
	import { trainingUpdateStore } from '../../../globalStore.js';

    const history: { [key:number]: TrainingUpdateMessage[]} = {};
    const trainingIds: number[] = [];

    trainingUpdateStore.subscribe(msgs => { 
        console.log("realtime", msgs);
        msgs.forEach(m => {
            if (history[m.TrainingId] == null) {
                history[m.TrainingId] = [];
                trainingIds.push(m.TrainingId);
            }
            history[m.TrainingId].push(m);
        });
    });
</script>

<div>
    {#each trainingIds as id}
    <div>
        {id}: {history[id].length}
    </div>
    {/each}
</div>