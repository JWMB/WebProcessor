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
	import { Assistant } from 'src/services/assistant';

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
		const groupsData = await apiFacade.trainings.getGroups(null);
		groups = Object.entries(groupsData).map((o) => ({ group: o[0], summaries: o[1] }));
	}

	async function onSelectGroup(groupId: string) {
		detailedTrainingsData = await apiFacade.trainings.getSummaries(groupId, null);
	}

	function calculateTrainingStats(data: TrainingSummaryWithDaysDto[], numberOfDays = 7) {
		const average = (arr: number[]) => {
			return arr.reduce((p, c) => p + c, 0) / arr.length;
		};
		return data.map((t) => {
			const dateRange = t.days.slice(-numberOfDays);
			const accuracy = average(dateRange.map((d) => d.numCorrectAnswers / (d.numQuestions || 1))) || 0;
			const effectiveTime = Math.min(average(dateRange.map((d) => (d.responseMinutes + d.remainingMinutes) / (t.targetMinutesPerDay || 32))) || 0, 1);
			return {
				id: t.id,
				username: t.username,
				trainedDays: t.days.length,
				trainedDaysMax: t.targetDays || 30,
				accuracy,
				effectiveTime,
				isAccuracyLow: accuracy < 0.4, // TODO: get value from server
				isEffectiveTimeLow: effectiveTime < 0.5, // TODO: get value from server
				isDaysTrainedLow: false, // TODO: get value from server
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
			>

			<button on:click={onCreateGroup}>
				{getString('teacher_create_group_label')}
			</button>
			<!-- svelte-ignore a11y-invalid-attribute -->
			<span class="tooltip">
				<a href="#" on:click={() => Assistant.openWidgetOnGuide(100, "g100-why-is-there-a-limit-on-the-number-of-trainings-i-can-create")}>?</a>
			</span>
	</Tabs>
	{/if}
	{#if trainings && trainings.length > 0}
		<div class="training-header">
			<h2>{getString('teacher_training_header')}</h2>
			<div class="range-widget-container">
				<div class="range-widget-label">{getString('teacher_stats_range_label')}</div>
				<div class="range-switch-container">
					<span class="switch-label" class:activeLabel={!showStatsForLast7days}>{getString('teacher_stats_range_all_days')}</span>
					<Switch name="range" color="#52cad8" inactiveColor="#52cad8" bind:checked={showStatsForLast7days} />
					<span class="switch-label" class:activeLabel={showStatsForLast7days}>{getString('teacher_stats_range_last_week')}</span>
				</div>
			</div>
		</div>
		<table>
			<tr>
				<th class="user-column">
					{getString('teacher_trainings_column_header_user')}
				</th>
				<th class="days-trained-column">
					{getString('teacher_trainings_column_header_days_trained')}
					<!-- svelte-ignore a11y-click-events-have-key-events -->
					<span class="tooltip" data-tooltip={getString('teacher_trainings_column_tooltip_days_trained')} on:click={() => Assistant.openWidgetOnGuide(83, "g83-what-do-the-bars-in-the-training-view-mean")}>?</span>
				</th>
				<th class="effective-time-column">
					{getString('teacher_trainings_column_header_effective_time')}
					<!-- svelte-ignore a11y-click-events-have-key-events -->
					<span class="tooltip" data-tooltip={getString('teacher_trainings_column_tooltip_effective_time')} on:click={() => Assistant.openWidgetOnGuide(83, "g83-what-do-the-bars-in-the-training-view-mean")}>?</span>
				</th>
				<th class="accuracy-column">
					{getString('teacher_trainings_column_header_accuracy')}
					<!-- svelte-ignore a11y-click-events-have-key-events -->
					<span class="tooltip" data-tooltip={getString('teacher_trainings_column_tooltip_accuracy')} on:click={() => Assistant.openWidgetOnGuide(83, "g83-what-do-the-bars-in-the-training-view-mean")}>?</span>
				</th>
				<th class="notes-column">
					{getString('teacher_trainings_column_header_notes')}
				</th>
			</tr>
			{#each trainings as t (t.id)}
				<tr on:click={() => onSelectTraining(t.id)} class="training-row">
					<td class="user-column">{t.username}</td>
					<td><ProgressBar value={t.trainedDays} max={t.trainedDaysMax} suffix="" decimals={0} color={t.isDaysTrainedLow ? '#ff5959' : '#c7a0fc'} /></td>
					<td><ProgressBar value={t.effectiveTime * 100} showValueAs="OnlyValue" max={100} suffix="%" decimals={0} color={t.isEffectiveTimeLow ? '#ff5959' : '#49e280'} /></td>
					<td><ProgressBar value={t.accuracy * 100} showValueAs="OnlyValue" max={100} suffix="%" decimals={0} color={t.isAccuracyLow ? '#ff5959' : '#52cad8'} /></td>
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
		justify-content: space-between;
	}
	.tooltip {
		border: 1px solid #4ba7b2;
		background: white;
		border-radius: 50%;
		width: 20px;
		height: 20px;
		display: inline-flex;
		color: #4ba7b2;
		justify-content: center;
		align-items: center;
		font-weight: normal;
	}
	.range-widget-label {
		display: block;
		font-weight: bold;
		font-size: 12px;
		margin-bottom: 6px;
		margin-top: 4px;
	}
	.range-switch-container {
		display: flex;
		align-items: center;
		gap: 6px;
	}
	.switch-label {
		color: rgb(183 182 182);
	}
	.switch-label.activeLabel {
		color: black;
	}
	table {
		height: 100%;
		text-align: left;
		width: calc(100% + 20px);
		border-spacing: 0;
		border-collapse: collapse;
		margin-left: -10px;
		margin-right: -10px;
	}
	.training-row:hover {
		background-color: #e1f1ff;
	}
	.user-column {
		width: 120px;
		padding-left: 10px;
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
	th {
		font-size: 12px;
	}
	th,
	td {
		white-space: nowrap;
		margin-right: 10px;
		padding: 4px 10px 4px 0;
	}
</style>
