<script lang="ts">
    // import { Bar } from 'svelte-chartjs';
    import { Chart,Title,Tooltip,Legend,BarElement,CategoryScale,LinearScale, type ChartDataset, BarController } from 'chart.js';
	import type { TrainingSummaryDto } from '../apiClient';
	import convert from 'color-convert';
	import { onMount } from 'svelte';
	import { DateUtils } from '../utilities/DateUtils';

    Chart.register(Title,Tooltip,Legend,BarElement,CategoryScale,LinearScale, BarController);
    export let data: TrainingSummaryDto[] = [];
    const weeksBack = 7;
    const now = Date.now();
    const weekMs = 1000 * 60 * 60 * 24 * 7;

    function weekToDate(weekFromStartWeek: number) {
        return new Date(Date.now() + (weekFromStartWeek - weeksBack) * weekMs);
    }

    function getSeries(all: TrainingSummaryDto[]) {
        function getWeeksFromStartWeek(date: string | Date | undefined) {
            if (!date) return -1;
            const val = DateUtils.toDate(date).valueOf();
            return weeksBack - Math.ceil(1.0 * (now - val) / weekMs);
        }

        const startAndLatest = all
            .map(o => ({ startWeek: getWeeksFromStartWeek(o.firstLogin), latestWeek: getWeeksFromStartWeek(o.lastLogin) }))
            .filter(o => o.startWeek >= 0);

        const weekArray = [...Array(weeksBack).keys()];

        const perWeek = weekArray
            .map(week => {
                const startedThisWeek = startAndLatest.filter(o => o.startWeek == week);

                const timeSeries = weekArray.map(w => { 
                    return week > w ? 0 : startedThisWeek.filter(o => o.latestWeek >= w).length;
                });
                
                const rgb = convert.hsl.rgb([(360 * week) / weeksBack, (week % 2) * 50 + 50, 50]);
				return <ChartDataset<"bar">>{
					label: `Week of ${DateUtils.toIsoDate(weekToDate(week))}`,
					data: timeSeries,
					fill: true,
					backgroundColor: `rgba(${rgb[0]}, ${rgb[1]}, ${rgb[2]}, 1)`,
                    borderColor: 'rgb(0, 0, 0)',
				};
            }).filter(o => o.data.length > 0);
        return perWeek;
    }

    onMount(() => {
        let chart: Chart;
        const context = (<HTMLCanvasElement>document.getElementById(`chart_starts`)).getContext('2d');
        chart = new Chart(context as CanvasRenderingContext2D, {
            type: "bar",
            data: {
                labels: [...Array(weeksBack).keys()].map(week => DateUtils.toIsoDate(weekToDate(week))), //DateUtils.toIsoDate
                datasets: getSeries(data)
            },
            options: {
                responsive: true,
                scales: {
                    y: {
                        stacked: true
                    },
                    x: { stacked: true }
                }
            }
        });
        chart.update();
    });
    // const config = <ChartData<"bar", number[], unknown>>{
    //     //labels: ['Red', 'Blue', 'Yellow', 'Green', 'Purple', 'Orange'],
    //     datasets: getSeries(data),
    // };
    // console.log("config", config);
</script>

<main>
    with start: {data.filter(o => !!o.firstLogin).length}
	<canvas id="chart_starts" width="800" height="400" />
</main>

<!-- <Bar data={config} options={{ responsive: true }} /> -->