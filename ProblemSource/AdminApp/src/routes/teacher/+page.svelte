<script lang="ts">
	import { onMount, tick } from 'svelte';
	import type { TrainingSummaryWithDaysDto, TrainingSummaryDto } from 'src/apiClient';
	import Tabs from 'src/components/tabs.svelte';
	import ProgressBar from './progress-bar.svelte';
	import CreateTrainingsModal from './create-trainings-modal.svelte';
	import { openModal } from 'svelte-modals';
	import Switch from 'src/components/switch.svelte';
	import { getApi } from 'src/globalStore';
	import type { ApiFacade } from 'src/apiFacade';
	import { getString } from 'src/utilities/LanguageService';

	const apiFacade = getApi() as ApiFacade;

	let detailedTrainingsData: TrainingSummaryWithDaysDto[] = [];
	let showStatsForLast7days = false;
	$: noOfDays = showStatsForLast7days ? 7 : 9999;

	$: trainings = calculateTrainingStats(detailedTrainingsData, noOfDays);
	let groups: { group: string; summaries: TrainingSummaryDto[] }[];

	onMount(() => getData());

	async function getData() {
		if (apiFacade == null) {
			console.error('apiFacade null');
			return;
		}
		const groupsData = await apiFacade.trainings.getGroups();
		groups = Object.entries(groupsData).map((o) => ({ group: o[0], summaries: o[1] }));
	}

	async function onSelectGroup(groupId: string) {
		console.log('onSelectGroup', groupId);
		await tick();
		detailedTrainingsData = await apiFacade.trainings.getSummaries(groupId);
		await tick();
		console.log(detailedTrainingsData, trainings);
	}

	function calculateTrainingStats(data: TrainingSummaryWithDaysDto[], numberOfDays = 7) {
		const average = (arr: number[]) => {
			return arr.reduce((p, c) => p + c, 0) / arr.length;
		};
		return data.map((t) => {
			const dateRange = t.days.slice(-numberOfDays);
			const accuracy = average(dateRange.map((d) => d.numCorrectAnswers / (d.numQuestions || 1))) || 0;
			const effectiveTime = average(dateRange.map((d) => d.responseMinutes / (d.responseMinutes + d.remainingMinutes || 1))) || 0;
			return {
				id: t.id,
				username: t.username,
				trainedDays: t.days.length,
				trainedDaysMax: 30,
				accuracy,
				effectiveTime,
				isAccuracyLow: accuracy < 0.4,
				isEffectiveTimeLow: effectiveTime < 0.5,
				isDaysTrainedLow: false,
				comments: [] as Array<{ type: 'Critical' | 'Warning' | 'Info'; description: string }>
			};
		});
	}

	function onSelectTraining(trainingId: number) {
		console.log('training id', trainingId);
		// goto(`${base}/training?id=${trainingId}`);
	}

	function onCreateGroup() {
		openModal(CreateTrainingsModal, {
			onCreateGroup: (id) => {
				getData();
			}
		});
	}

	function log(t: any) {
		console.log('test');
		return 1;
	}
</script>

<div class="teacher-view">
	<h2>{getString('teacher_groups_header')}</h2>

	{#if groups && groups.length > 0}
		<Tabs
			urlParam="group"
			tabs={groups.map((g) => {
				return { id: g.group };
			})}
			on:selected={(e) => onSelectGroup(e.detail)}
			><button on:click={onCreateGroup}>{getString('teacher_create_group_label')}</button>
		</Tabs>
	{/if}
	{#if trainings && trainings.length > 0}
		<div class="training-header">
			<h2>{getString('teacher_training_header')}</h2>
			<label for="range">
				{getString('teacher_stats_range_label')}
				<span class:activeLabel={!showStatsForLast7days}>{getString('teacher_stats_range_all_days')}</span>
				<Switch name="range" bind:checked={showStatsForLast7days} />
				<span class:activeLabel={showStatsForLast7days}>{getString('teacher_stats_range_last_week')}</span>
			</label>
		</div>

		<table>
			<tr>
				<th class="user-column">{getString('teacher_trainings_column_header_user')}</th>
				<th class="days-trained-column">{getString('teacher_trainings_column_header_days_trained')}</th>
				<th class="effective-time-column">{getString('teacher_trainings_column_header_effective_time')}</th>
				<th class="accuracy-column">{getString('teacher_trainings_column_header_accuracy')}</th>
				<th class="notes-column">{getString('teacher_trainings_column_header_notes')}</th>
			</tr>
			{#each trainings as t (t.id)}
				<tr on:click={() => onSelectTraining(t.id)}>
					<td>{t.username}</td>
					<td><ProgressBar value={t.trainedDays} max={t.trainedDaysMax} suffix="" decimals={0} color={t.isDaysTrainedLow ? '#ff5959' : '#c7a0fc'} /></td>
					<td><ProgressBar value={t.effectiveTime * 100} max={100} suffix="%" decimals={0} color={t.isEffectiveTimeLow ? '#ff5959' : '#49e280'} /></td>
					<td><ProgressBar value={t.accuracy * 100} max={100} suffix="%" decimals={0} color={t.isAccuracyLow ? '#ff5959' : '#52cad8'} /></td>
					<td>
						{#each t.comments as c}
							{c.description}
						{/each}</td>
				</tr>
			{/each}
		</table>
	{/if}
</div>

<style>
	.teacher-view {
		padding: 20px;
	}
	.training-header {
		display: flex;
		align-items: center;
		gap: 20px;
	}
	.training-header label {
		display: flex;
		align-items: center;
		gap: 5px;
	}
	.activeLabel {
		font-weight: bold;
	}
	table {
		height: 100%;
		text-align: left;
		width: 100%;
		border-spacing: 0;
		border-collapse: collapse;
	}
	.user-column {
		width: 120px;
	}
	.days-trained-column {
		width: 120px;
	}
	.effective-time-column {
		width: 160px;
	}
	.accuracy-column {
		width: 160px;
	}
	.notes-column {
		width: auto;
	}

	tr {
		height: 32px;
	}
	tr:hover {
		background-color: #e1f1ff;
	}
	th,
	td {
		margin-right: 10px;
		padding: 6px 10px 6px 0;
	}
</style>
