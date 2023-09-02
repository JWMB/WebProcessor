<script lang="ts">
	import { onMount, tick } from 'svelte';
	import type { TrainingSummaryWithDaysDto, TrainingSummaryDto } from 'src/apiClient';
	import Tabs from 'src/components/tabs.svelte';
	import ProgressBar from './progress-bar.svelte';
	import CreateTrainingsModal from './create-trainings-modal.svelte';
	import { openModal } from 'svelte-modals';
	import Switch from 'src/components/switch.svelte';
	import { assistanStore, getApi } from 'src/globalStore';
	import type { ApiFacade } from 'src/apiFacade';
	import { getString } from 'src/utilities/LanguageService';
	import { TrainingDayTools } from 'src/services/trainingDayTools';
	import { DateUtils } from 'src/utilities/DateUtils';

	const apiFacade = getApi() as ApiFacade;

	let detailedTrainingsData: TrainingSummaryWithDaysDto[] = [];
	let showStatsForLast7days = false;

	const trainingDayDetailsNumDaysBack = 7;
	let trainingDayDetails = TrainingDayTools.getLatestNumDaysStats(0, []);

	$: noOfDays = showStatsForLast7days ? 7 : 9999;

	$: trainings = calculateTrainingStats(detailedTrainingsData, noOfDays);
	let groups: { group: string; summaries: TrainingSummaryDto[] }[];
	let lastTrainingOccasionInGroup: Date | null = null;

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
		detailedTrainingsData = await apiFacade.trainings.getSummaries(groupId);
	}

	function getAccuracyWarningLevel(training: { accuracyWarningThreshold: number }, accuracy: number) {
		return accuracy < training.accuracyWarningThreshold ? 1 : 0;
	}
	function getTimeWarningLevel(training: { effectiveTimeWarningThreshold: number }, minutes: number) {
		return minutes < training.effectiveTimeWarningThreshold ? 1 : 0;
	}
	function gettimeTotalOfTargetPercent(training: {latestDays?: { timeTotalOfTargetPercent: number}[]}, dayOffset: number) {
		return ((training.latestDays || [])[dayOffset] ||{}).timeTotalOfTargetPercent || 0;
	}

	function calculateTrainingStats(data: TrainingSummaryWithDaysDto[], numberOfDays = 7) {
		trainingDayDetails = TrainingDayTools.getLatestNumDaysStats(7, detailedTrainingsData);

		// Math.max.apply(null, data.map(o => o.lastLogin));
		const average = (arr: number[]) => {
			return arr.reduce((p, c) => p + c, 0) / arr.length;
		};
		const result = data.map((t) => {
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
				accuracyWarningThreshold: 0.4, // TODO: part of TrainingSummaryWithDaysDto
				effectiveTimeWarningThreshold: 0.5, // TODO: part of TrainingSummaryWithDaysDto
				targetMinutesPerDay: t.targetMinutesPerDay,
				isDaysTrainedLow: false, // TODO: get value from server
				latestDays: trainingDayDetails.trainings.find(o => o.id == t.id)?.days,
				comments: [] as Array<{ type: 'Critical' | 'Warning' | 'Info'; description: string }>
			};
		});

		try {
			const allLastDays = result
				.map(o => o.latestDays)
				.filter(o => o != null && o.length > 0)
				.map(o => o ? o[o.length - 1] : null)
				.filter(o => o != null)
				.map(o => (<Date>(<any>o)["startTime"]))
				.filter(o => o != null)
				.map(o => o.valueOf());
			lastTrainingOccasionInGroup = allLastDays.length ? new Date(Math.max.apply(null, allLastDays)) : null;
		} catch {}
		return result;
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
				<a href="#" on:click={() => assistanStore.openWidgetWithFirstSearchHit("limit")}>?</a>
			</span>
	</Tabs>
	{/if}
	{#if trainings && trainings.length > 0}
		<div class="training-header">
			<h2>{getString('teacher_training_header')}</h2>
			<p>Num trainings:{trainings.length} - Started:{trainings.filter(o => o.trainedDays > 0).length} - Last training occasion:{lastTrainingOccasionInGroup == null ? 'N/A' : DateUtils.toIsoDate(lastTrainingOccasionInGroup)}</p>
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
					<span class="tooltip" data-tooltip={getString('teacher_trainings_column_tooltip_days_trained')} on:click={() => assistanStore.openWidgetWithFirstSearchHit("statistics")}>?</span>
				</th>
				<th class="effective-time-column">
					{getString('teacher_trainings_column_header_effective_time')}
					<!-- svelte-ignore a11y-click-events-have-key-events -->
					<span class="tooltip" data-tooltip={getString('teacher_trainings_column_tooltip_effective_time')} on:click={() => assistanStore.openWidgetWithFirstSearchHit("statistics")}>?</span>
				</th>
				<th class="accuracy-column">
					{getString('teacher_trainings_column_header_accuracy')}
					<!-- svelte-ignore a11y-click-events-have-key-events -->
					<span class="tooltip" data-tooltip={getString('teacher_trainings_column_tooltip_accuracy')} on:click={() => assistanStore.openWidgetWithFirstSearchHit("statistics")}>?</span>
				</th>
				{#each Array.from(Array(trainingDayDetailsNumDaysBack).keys()) as dayOffset}
				<th class="training-day-column" title="{DateUtils.toIsoDate(DateUtils.addDays(trainingDayDetails.startDate, dayOffset))}">{DateUtils.getWeekDayName(DateUtils.addDays(trainingDayDetails.startDate, dayOffset))[0]}</th>
				{/each}
				<th class="notes-column">
					{getString('teacher_trainings_column_header_notes')}
				</th>
			</tr>
			{#each trainings as t (t.id)}
				<tr on:click={() => onSelectTraining(t.id)} class="training-row">
					<td class="user-column">{t.username}&nbsp;<a href="/admin/teacher/training?id={t.id.toString()}" title="id={t.id.toString()}" target="_blank">^</a></td>
					<td>
						<ProgressBar value={t.trainedDays} max={t.trainedDaysMax} suffix="" decimals={0} color={t.isDaysTrainedLow ? '#ff5959' : '#c7a0fc'} />
					</td>
					<td title="Target: {t.targetMinutesPerDay} minutes">
						<ProgressBar value={t.effectiveTime * 100} showValueAs="OnlyValue" max={100} suffix="%" decimals={0} color={getTimeWarningLevel(t, t.effectiveTime) == 1 ? '#ff5959' : '#49e280'}/>
					</td>
					<td>
						<ProgressBar value={t.accuracy * 100} showValueAs="OnlyValue" max={100} suffix="%" decimals={0} color={getAccuracyWarningLevel(t, t.accuracy) == 1 ? '#ff5959' : '#52cad8'} />
					</td>
					{#each Array.from(Array(trainingDayDetailsNumDaysBack).keys()) as dayOffset}
					<td title="{gettimeTotalOfTargetPercent(t, dayOffset)}% of {t.targetMinutesPerDay} minutes">
						<ProgressBar value={gettimeTotalOfTargetPercent(t, dayOffset)} showValueAs="None" max={100} suffix="%" decimals={0} color={getTimeWarningLevel(t, 0.01 * gettimeTotalOfTargetPercent(t, dayOffset)) == 1 ? '#fc9a9a' : '#77eda1'}/>
					</td>
					{/each}
					<td>
					{#each t.comments as c}
						{c.description}
					{/each}
					</td>
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
	.training-day-column {
		width: 55px;
		padding-left: 5px;
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
