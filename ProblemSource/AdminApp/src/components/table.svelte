<script lang="ts">
	import { TableDef, type TableHeaderCell } from "src/services/table";


    // let header = 
    
    export let def: TableDef = new TableDef();
</script>

<div>
    <table>
        <thead>
            {#each def.getHeaderRows() as headerRow}
            <tr>
                {#each headerRow as cell}
                <th colspan="{cell.colSpan}">{cell.text}</th>
                {/each}
            </tr>
            {/each}
        </thead>
        <tbody>
            {#each def.getBodyRows() as row}
            <tr>
                {#each row as cell}
                <td colspan="{cell.colSpan}">{cell.text}</td>
                {/each}
            </tr>
            {/each}
        </tbody>
    </table>
</div>