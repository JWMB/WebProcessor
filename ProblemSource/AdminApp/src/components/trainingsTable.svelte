<script lang="ts">
	import type { TrainingSummary } from "src/apiClient";
    import { base } from '$app/paths';
	import { onMount } from 'svelte';
	import { Grid } from 'ag-grid-community';

    import 'ag-grid-community/styles/ag-grid.css';
	import 'ag-grid-community/styles/ag-theme-alpine.css';

    const numDaysBack = 10;
    let fromDate = new Date(Date.now() - numDaysBack * 1000 * 60 * 60 * 24);

    const formatTraining = (training: TrainingSummary) => {
        const targetTime = 20;

        const withDayIndex = training.days.filter(d => new Date(d.startTime) >= fromDate)
                .map(d => ({
                    dayIndex: getDaysBetween(fromDate, new Date(d.startTime)),
                    ss: d.remainingMinutes,
                    timeTarget: targetTime,
                    timeActive: d.responseMinutes,
                    timeTotal: d.remainingMinutes + d.responseMinutes,
                    info: d
                }));
                
        const firstDay = training.days[0] || { accountUuid: "N/A", startTime: new Date() };
        const uuid = firstDay.accountUuid;
        const daysSinceStart = (Date.now().valueOf() - firstDay.startTime.valueOf()) / 1000 / 60 / 60 / 24;
        return {
            uuid: uuid,
            startDate: firstDay.startTime,
            totalDays: training.days.length,
            daysPerWeek: training.days.length / (daysSinceStart / 7),
            days: withDayIndex
        };
    };
    let trainingSummaries: TrainingSummary[] = [
        { id: 1, days: [
            { accountId: 1, accountUuid: "aa", trainingDay: 1, startTime: new Date("2023-01-01"), endTimeStamp: new Date(), 
            numRacesWon: 1, numRaces: 1, numPlanetsWon: 1, numCorrectAnswers: 1, numQuestions: 1, responseMinutes:8, remainingMinutes:4 }] }
    ];
    let trainings = trainingSummaries.map(formatTraining);

    function getDateOnly(date: Date) {
        const str = date.toISOString();
        const index = str.indexOf("T");
        return str.substring(0, index);
        // const msOfTime = date.getHours() * 60 * 60 * 1000 + date.getMinutes() * 60 * 1000 + date.getSeconds() * 1000 + date.getMilliseconds();
    }
    function getDaysBetween(dateStart: Date, dateEnd: Date) {
        const diff = dateEnd.valueOf() - dateStart.valueOf();
        return Math.floor(diff / 1000 / 60 / 60 / 24);
    }

    function getDateHeader(date: Date) {
        const days = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];
        return days[date.getDay()].substring(0, 1);
    }

	const dayRenderer = (params: any) => {
        function describeArc(x: number, y: number, radius: number, startAngle: number, endAngle: number){
            function polarToCartesian(centerX: number, centerY: number, radius: number, angleInDegrees: number) {
                const angleInRadians = (angleInDegrees-90) * Math.PI / 180.0;
                return {
                    x: centerX + (radius * Math.cos(angleInRadians)),
                    y: centerY + (radius * Math.sin(angleInRadians))
                };
            }
            const start = polarToCartesian(x, y, radius, endAngle);
            const end = polarToCartesian(x, y, radius, startAngle);
            const largeArcFlag = endAngle - startAngle <= 180 ? "0" : "1";
            const d = [
                "M", start.x, start.y, 
                "A", radius, radius, 0, largeArcFlag, 0, end.x, end.y
            ].join(" ");
            return d;       
        }

        // console.log(params.data.days, params.colDef.dayIndex);
		const dayData = params.data.days[params.colDef.dayIndex];
		if (dayData == null) {
			return "";
		}

		const timeActiveFract = dayData.timeActive / dayData.timeTarget;
		const timeTotalFract = dayData.timeTotal / dayData.timeTarget;
		const circleRadius = 9;
		const green = "#5a5";
		const red = "#bdb";
		const gray = "#eee";
		const time = `
<g transform="translate(${circleRadius},${circleRadius + 8})">
	<circle r="${circleRadius}" fill="${gray}" stroke="black" stroke-width="0.5"></circle>
	<path fill="none" stroke="${red}" stroke-width="${circleRadius}" d="${describeArc(0,0,circleRadius/2, 0, timeTotalFract * 360)}"></path>
	<path fill="none" stroke="${green}" stroke-width="${circleRadius}" d="${describeArc(0,0,circleRadius/2, 0, timeActiveFract * 360)}"></path>
</g>`.trim();

		const rectSize = { x: 30, y: 10 };
		const correct = `
<g transform="translate(30,13)">
	<rect x="0" width="${rectSize.x}" height="${rectSize.y}" rx="2" fill="${gray}" stroke="black" stroke-width="0.5" />
	<rect x="0" width="${dayData.correct * rectSize.x}" height="${rectSize.y}" rx="2" fill="${green}" />
</g>
`.trim();
		return `<svg>${time}${correct}</svg>`;
	};

	const dayColumns = new Array(numDaysBack).fill(0).map((v, i) => {
        const d = new Date(fromDate.valueOf() + i * 24 * 60 * 60 * 1000);
        return {
            headerName: getDateHeader(d),
            headerTooltip: getDateOnly(d),
            dayIndex: i,
            sortable: false,
            resizable: false,
            cellRenderer: dayRenderer
		}});

    const gridOptions = {
		defaultColDef: {
			sortable: true,
			unSortIcon: true,
			resizable: true,
			flex: 1,
			// minWidth: 170,
		},
  		columnDefs: [
			{ headerName: "Account information", 
			  children: [
				{ headerName: 'Username', field: 'uuid', 
					cellRenderer: (params: any) => { return `<a href="${base}/?id=${params.data.id}">${params.value}</a>`; }
				 },
				{ headerName: 'Days', headerTooltip: "Days trained", field: 'totalDays' },
				{ headerName: 'Per week', headerTooltip: "Average days trained per week", field: 'daysPerWeek' }
			  ]
			},
			{ headerName: "Latest days", children: dayColumns }
		],
		rowData: trainings
	};

    onMount(() => {
		const eGridDiv = document.getElementById("myGrid");
		if (eGridDiv != null) {
			const grid = new Grid(eGridDiv, gridOptions);
		}
	});

</script>

<div >
	<div id="myGrid" style="height: 500px; width: 1200px" class="ag-theme-alpine"></div>
</div>
