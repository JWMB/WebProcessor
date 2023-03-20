<script lang="ts">
	import { Chart } from 'chart.js';
	import type { PhaseStatistics } from 'src/apiClient';
	import { onMount } from 'svelte';
	import convert from 'color-convert';
	import { groupBy, max, sum } from '../arrayUtils';

	export let data: PhaseStatistics[];
	let chart: Chart;

	const update = () => {
		if (!chart) return;
		if (!data) return;
		const byExercise = groupBy(data, (o) => o.exercise.split('#')[0]);
		const exerciseNames = Object.keys(byExercise);
		const maxDay = max(exerciseNames.map((key) => max(byExercise[key].map((o) => o.training_day))));
		const days = [...Array(maxDay).keys()].map((o) => o + 1);

		(chart.data.labels = days.map((o) => o.toString())),
			(chart.data.datasets = exerciseNames.map((key, index) => {
				const inEx = byExercise[key];
				// TODO: nswag! o.timestamp is a string, not a Date!
				const timeSeries = days.map((std) => {
					const aa = inEx.filter((o) => o.training_day == std);
					return aa.length > 0 ? sum(aa.map((o) => (new Date(o.end_timestamp).valueOf() - new Date(o.timestamp).valueOf()) / 1000 / 60)) : 0;
				});
				const rgb = convert.hsl.rgb([(360 * index) / exerciseNames.length, (index % 2) * 50 + 50, 50]);
				return {
					label: key,
					data: timeSeries,
					fill: true,
					backgroundColor: `rgba(${rgb[0]}, ${rgb[1]}, ${rgb[2]}, 1)`
				};
			}));
		chart.update();
	};

	$: {
		if (data?.length > 0) {
			update();
		}
	}

	onMount(() => {
		const context = (<HTMLCanvasElement>document.getElementById('chart_timePerExercise')).getContext('2d');
		if (context == null) throw new Error('No context found');
		chart = new Chart(context, {
			type: 'line',
			data: { labels: [], datasets: [] },
			options: {
				plugins: {
					legend: { display: true, position: 'left' }
				},
				animation: false,
				scales: {
					y: { stacked: true }
				}
			}
		});

		if (data?.length > 0) {
			update();
		}
	});
</script>

<main>
	<div>
		<canvas id="chart_timePerExercise" width="800" height="150" />
	</div>
</main>
