<script lang="ts">
	import type { TrainingSummaryWithDaysDto } from 'src/apiClient';
	import { base } from '$app/paths';
	import { onMount } from 'svelte';
	import { Grid, type GridOptions } from 'ag-grid-community';

	import 'ag-grid-community/styles/ag-grid.css';
	import 'ag-grid-community/styles/ag-theme-alpine.css';
	import { DateUtils } from 'src/utilities/DateUtils';
	import { TrainingDayTools } from 'src/services/trainingDayTools';

	export let trainingSummaries: TrainingSummaryWithDaysDto[] = [];

	function getDateHeader(date: Date) {
		const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
		const weekday = date.getDay();
		return days[weekday].substring(0, 1);
	}

    export let numDays = 10;

	const {trainings, startDate} = TrainingDayTools.getLatestNumDaysStats(numDays, trainingSummaries);

	const dayRenderer = (params: any) => {
		function describeArc(x: number, y: number, radius: number, startAngle: number, endAngle: number) {
			function polarToCartesian(centerX: number, centerY: number, radius: number, angleInDegrees: number) {
				const angleInRadians = ((angleInDegrees - 90) * Math.PI) / 180.0;
				return {
					x: centerX + radius * Math.cos(angleInRadians),
					y: centerY + radius * Math.sin(angleInRadians)
				};
			}
			const start = polarToCartesian(x, y, radius, endAngle);
			const end = polarToCartesian(x, y, radius, startAngle);
			const largeArcFlag = endAngle - startAngle <= 180 ? '0' : '1';
			const d = ['M', start.x, start.y, 'A', radius, radius, 0, largeArcFlag, 0, end.x, end.y].join(' ');
			return d;
		}

		const dayData = params.data.days.filter((d: any) => d.dayIndex === params.colDef.dayIndex)[0];
		if (dayData == null) {
			return '';
		}

		const timeActiveFract = dayData.timeActive / dayData.timeTarget;
		const timeTotalFract = dayData.timeTotal / dayData.timeTarget;
		const circleRadius = 9;
		const green = '#5a5';
		const red = '#bdb';
		const gray = '#eee';
		const time = `
<g transform="translate(${circleRadius},${circleRadius + 8})">
	<circle r="${circleRadius}" fill="${gray}" stroke="black" stroke-width="0.5"></circle>
	<path fill="none" stroke="${red}" stroke-width="${circleRadius}" d="${describeArc(0, 0, circleRadius / 2, 0, timeTotalFract * 360)}"></path>
	<path fill="none" stroke="${green}" stroke-width="${circleRadius}" d="${describeArc(0, 0, circleRadius / 2, 0, timeActiveFract * 360)}"></path>
</g>`.trim();

		const rectSize = { x: 30, y: 10 };
		const correct = `
<g transform="translate(30,13)">
	<rect x="0" width="${rectSize.x}" height="${rectSize.y}" rx="2" fill="${gray}" stroke="black" stroke-width="0.5" />
	<rect x="0" width="${dayData.winRate * rectSize.x}" height="${rectSize.y}" rx="2" fill="${green}" />
</g>
`.trim();
		return `<svg viewBox="0 0 60 30">${time}${correct}</svg>`;
	};

	const dayColumns = new Array(numDays + 1).fill(0).map((_, i) => {
		const d = DateUtils.addDays(startDate, i);
		return {
			headerName: getDateHeader(d),
			headerTooltip: DateUtils.toIsoDate(d),
			dayIndex: i,
			sortable: false,
			resizable: false,
			cellRenderer: dayRenderer
		};
	});

	const gridOptions: GridOptions<any> = {
		defaultColDef: {
			sortable: true,
			unSortIcon: true,
			resizable: true,
			flex: 1
			// minWidth: 170,
		},
		columnDefs: [
			{
				headerName: 'Account information',
				children: [
					{
						headerName: 'Username',
						field: 'uuid',
						cellRenderer: (params: any) => {
							return `${params.value} (${params.data.id}) <a href="${base}/training?id=${params.data.id}">Open</a>`;
						}
					},
					{ headerName: 'Days', headerTooltip: 'Days trained', field: 'totalDays' },
					{
						headerName: 'Per week',
						headerTooltip: 'Average days trained per week',
						field: 'daysPerWeek',
						cellRenderer: (params: any) => params.value.toFixed(1)
					},
					{ headerName: 'First', headerTooltip: 'First training', field: 'firstDate' },
					{ headerName: 'Latest', headerTooltip: 'Latest training', field: 'latestDate' },
					{ headerName: 'TargetTime', headerTooltip: '', field: 'targetTime' }
				]
			},
			{
				headerName: `Sessions ${DateUtils.toIsoDate(startDate)} - ${DateUtils.toIsoDate(DateUtils.addDays(startDate, numDays))}`,
				children: dayColumns
			}
		],
		rowData: trainings,
		//onRowClicked: (e) => dispatch('clickedRow', { id: e.data.id })
	};

	onMount(() => {
		const eGridDiv = document.getElementById('myGrid');
		if (eGridDiv != null) {
			const grid = new Grid(eGridDiv, gridOptions);
			if (gridOptions.columnApi != null) {
				gridOptions.columnApi.autoSizeAllColumns(); //sizeColumnsToFit();
			}
		}
	});
</script>

<div>
	<div id="myGrid" style="height: 500px;" class="ag-theme-alpine" />
</div>
