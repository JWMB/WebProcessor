<script lang="ts">
	// import { Chart } from 'chart.js';
	import Chart from 'chart.js/auto'; // automatically register plugins so we don't have to elsewhere
	import type { TrainingDayAccount } from 'src/apiClient';
	import { onMount } from 'svelte';

	export let data: TrainingDayAccount[];
	let chart: Chart;

	const update = () => {
		if (!data || !data.length || !chart) return;

		const sorted = data.sort((a, b) => a.trainingDay - b.trainingDay);

		chart.options = {
			scales: {
		        y: {
					min: 0,
				}
			}
		};
		chart.data = {
			labels: sorted.map((o) => o.trainingDay.toString()),
			datasets: [
				{
					label: 'Response time',
					backgroundColor: 'rgb(255, 99, 132)',
					borderColor: 'rgb(255, 99, 132)',
					data: sorted.map((o) => o.responseMinutes)
				},
				{
					label: 'Total time',
					backgroundColor: 'rgb(132, 99, 255)',
					borderColor: 'rgb(132, 99, 255)',
					data: sorted.map((o) => Math.min(100, o.responseMinutes + o.remainingMinutes))
				}
			]
		};

		chart.update();
	};

	$: {
		if (data?.length > 0) {
			update();
		}
	}

	onMount(() => {
		const context = (<HTMLCanvasElement>document.getElementById('trainingDaysChart')).getContext('2d');
		chart = new Chart(context, {
			type: 'line',
			data: { labels: [], datasets: [] },
			options: {
				plugins: {
					legend: { display: true, position: 'left' }
				},
				animation: false
			}
		});

		if (data?.length > 0) update();
	});
</script>

<main>
	<div>
		<canvas id="trainingDaysChart" width="800" height="150" />
	</div>
</main>
