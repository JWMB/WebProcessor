<script lang="ts">
	import type { TrainingSummaryDto } from 'src/apiClient';
	import { Grid, type GridOptions } from 'ag-grid-community';

	import 'ag-grid-community/styles/ag-grid.css';
	import 'ag-grid-community/styles/ag-theme-alpine.css';
	import { createEventDispatcher, onMount } from 'svelte';
	import { min, max, sum } from '../arrayUtils';

	export let trainingSummaries: { group: string; summaries: TrainingSummaryDto[] }[] = [];

	const dispatch = createEventDispatcher<{ clickedRow: { group: string } }>();

	const xxx = (params: any, func: (data: TrainingSummaryDto[]) => any) => {
		const typed = <TrainingSummaryDto[]>params.value;
		return func(typed);
	};
	const gridOptions: GridOptions<any> = {
		defaultColDef: {
			sortable: true,
			unSortIcon: true,
			resizable: true,
			flex: 1
		},
		columnDefs: [
			{ headerName: 'Group', field: 'group' },
			{
				headerName: 'Num trainings',
				field: 'summaries',
				cellRenderer: (params: any) => params.value.length
			},
			{
				headerName: 'Num started',
				field: 'summaries',
				cellRenderer: (params: any) => xxx(params, (ts) => ts.filter((t) => t.firstLogin != null).length)
			},
			{
				headerName: 'Max day',
				field: 'summaries',
				cellRenderer: (params: any) => xxx(params, (ts) => max(ts.map((t) => t.trainedDays)))
			},
			{
				headerName: 'Min day',
				field: 'summaries',
				cellRenderer: (params: any) => xxx(params, (ts) => min(ts.filter((t) => t.firstLogin != null).map((t) => t.trainedDays)))
			},
			{
				headerName: 'Avg days',
				field: 'summaries',
				cellRenderer: (params: any) => xxx(params, (ts) => sum(ts.filter((t) => t.firstLogin != null).map((t) => t.trainedDays)) / ts.filter((t) => t.firstLogin != null).length)
			},
			{
				headerName: 'Latest',
				field: 'summaries',
				cellRenderer: (params: any) => xxx(params, (ts) => max(ts.filter((t) => t.lastLogin != null).map((t) => t.lastLogin!.valueOf())))
			}
		],
		rowData: trainingSummaries,
		onRowClicked: (e) => dispatch('clickedRow', { group: e.data.group })
	};

	onMount(() => {
		const eGridDiv = document.getElementById('agTrainingGroups');
		if (eGridDiv != null) {
			const grid = new Grid(eGridDiv, gridOptions);
			if (gridOptions.columnApi != null) {
				gridOptions.columnApi.autoSizeAllColumns();
			}
		}
	});
</script>

<div>
	<div id="agTrainingGroups" style="height: 200px;" class="ag-theme-alpine" />
</div>
