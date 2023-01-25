<script lang="ts">
	import type { TrainingSummaryWithDaysDto } from 'src/apiClient';
	import { base } from '$app/paths';
	import { createEventDispatcher, onMount } from 'svelte';
	import { Grid, type GridOptions } from 'ag-grid-community';

	import 'ag-grid-community/styles/ag-grid.css';
	import 'ag-grid-community/styles/ag-theme-alpine.css';
	import { max } from '../arrayUtils';
	import { identity } from 'svelte/internal';

	export let trainingSummaries: TrainingSummaryWithDaysDto[] = [];
	export let numDays = 10;

	const dispatch = createEventDispatcher<{ clickedRow: { id: number } }>();

	function getDateString(date: Date) {
		const str = date.toISOString();
		const index = str.indexOf('T');
		return str.substring(0, index);
	}
	function getDatePart(date: Date) {
		const msOfTime = date.getHours() * 60 * 60 * 1000 + date.getMinutes() * 60 * 1000 + date.getSeconds() * 1000 + date.getMilliseconds();
		return new Date(date.valueOf() - msOfTime);
	}
	function getDaysBetween(dateStart: Date, dateEnd: Date) {
		const diff = dateEnd.valueOf() - dateStart.valueOf();
		return Math.floor(diff / 1000 / 60 / 60 / 24);
	}
	function addDays(date: Date, days: number) {
		return new Date(date.valueOf() + days * 24 * 60 * 60 * 1000);
	}

	function getDateHeader(date: Date) {
		const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
		const weekday = date.getDay();
		return days[weekday].substring(0, 1);
		// return `<span>${days[weekday].substring(0, 1)}</span>`;
	}

	//console.log("trainingSummaries", trainingSummaries.length, numDays);
	const latestTimestamp = max(trainingSummaries.map((ts) => max(ts.days.map((d) => new Date(d.startTime).valueOf()))));

	let fromDate = new Date(getDatePart(addDays(new Date(latestTimestamp), -numDays)));

	const formatTraining = (training: TrainingSummaryWithDaysDto) => {
		const targetTime = 20;

		const withDayIndex = training.days
			.filter((d) => new Date(d.startTime) >= fromDate)
			.map((d) => ({
				dayIndex: getDaysBetween(fromDate, getDatePart(new Date(d.startTime))),
				timeTarget: targetTime,
				timeActive: d.responseMinutes,
				timeTotal: d.remainingMinutes + d.responseMinutes,
				correct: d.numCorrectAnswers / d.numQuestions,
				winRate: d.numRacesWon / d.numRaces
			}));

		const firstDay = training.days[0] || { accountUuid: 'N/A', startTime: new Date() };
		// const lastDay = training.days[training.days.length - 1] || firstDay;
		const uuid = training.username; // firstDay.accountUuid;
		const daysSinceStart = getDaysBetween(new Date(firstDay.startTime), new Date(latestTimestamp));
		// console.log(uuid, training.days.length, daysSinceStart);
		return {
			id: training.id,
			uuid: uuid,
			startDate: firstDay.startTime,
			totalDays: training.days.length,
			firstDate: getDateString(new Date(training.days[0].startTime)),
			latestDate: getDateString(new Date(training.days[training.days.length - 1].startTime)),
			daysPerWeek: training.days.length / (daysSinceStart / 7),
			days: withDayIndex
		};
	};

	const trainings = trainingSummaries.map(formatTraining);

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
		const d = new Date(fromDate.valueOf() + i * 24 * 60 * 60 * 1000);
		return {
			headerName: getDateHeader(d),
			headerTooltip: getDateString(d),
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
							return `<a href="${base}/?id=${params.data.id}">${params.value}</a>`;
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
					{ headerName: 'Latest', headerTooltip: 'Latest training', field: 'latestDate' }
				]
			},
			{
				headerName: `Sessions ${getDateString(fromDate)} - ${getDateString(addDays(fromDate, numDays))}`,
				children: dayColumns
			}
		],
		rowData: trainings,
		onRowClicked: (e) => dispatch('clickedRow', { id: e.data.id })
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
