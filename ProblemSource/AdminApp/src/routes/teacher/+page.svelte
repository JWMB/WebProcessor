<script lang="ts">
	import { onDestroy, onMount } from 'svelte';
	import Tabs from 'src/components/tabs.svelte';
	import ProgressBar from './progress-bar.svelte';
	import CreateTrainingsModal from './create-trainings-modal.svelte';
	import { openModal } from 'svelte-modals';
	import Switch from 'src/components/switch.svelte';
	import Realtimeline from 'src/components/realtimeline.svelte';
	import { type PatchTrainingDto, type Training, type TrainingSummaryWithDaysDto, type TrainingSummaryDto } from '../../apiClient';
	import { ApiFacade } from '../../apiFacade';
	import { getApi, userStore } from '../../globalStore';
	import { RealtimelineTools } from '../../services/realtimelineTools';
	import { TrainingDayTools } from '../../services/trainingDayTools';
	import { getString } from '../../utilities/LanguageService';
	import { DateUtils } from '../../utilities/DateUtils';
	import { Avatar } from '../../services/avatar';
	import DateInput from '../../components/DateInput.svelte';
	import showdown from 'showdown';

	const apiFacade = getApi() as ApiFacade;

	const clientUrl = "https://curricullm.org/vektor/";
	// const showRealtimeButton = false; // For now, don't show it at all...
	const showRealtimeButton = $userStore?.role == "Admin";
	const showAIButton = true; //$userStore?.role == "Admin";
	const showAIEditButton = $userStore?.role == "Admin";
	let aiDialogForTraining: { id: number, username: string, trainedDays: number } | null = null;
	const promptSettings = {
		template: "https://raw.githubusercontent.com/JWMB/WebProcessor/refs/heads/main/ProblemSource/ProblemSourceModule/Resources/AICoach/TeacherStudent.txt",
		model: "qwen3.1",
		prompt: "(Generated prompt goes here)",
		completion: "(Generated completion goes here)",
		completionHtml: "(Generated completion goes here)"
	};

	const rtlTools = new RealtimelineTools(2 * 60 * 1000);

	let realtimeConnected: boolean | null = rtlTools.isConnected;
	const connectionSignal = (status: boolean | null) => {
		realtimeConnected = status;
	}
	rtlTools.connectionSignal.on(connectionSignal);

	let allRealtimeData = rtlTools.getData();
	const getRealtimeData = () => {
		allRealtimeData = rtlTools.getData(detailedTrainingsData.map(o => o.id));
	}
	const getRealtimeDataForId = (trainingId: number) => {
		const forTraining = allRealtimeData.filter(o => o.id == trainingId);
		return forTraining.length ? forTraining[0].events : [];
	};

	let detailedTrainingsData: TrainingSummaryWithDaysDto[] = [];
	let showStatsForLast7days = false;

	const trainingDayDetailsNumDaysBack = 7;
	let trainingDayDetails = TrainingDayTools.getLatestNumDaysStats(0, []);

	$: noOfDays = showStatsForLast7days ? 7 : 9999;

	$: trainings = calculateTrainingStats(detailedTrainingsData, noOfDays);
	let groups: { group: string; summaries: TrainingSummaryDto[] }[];
	let lastTrainingOccasionInGroup: Date | null = null;

	onMount(() => { 
		getData();
        const interval = setInterval(() => {
            rtlTools.clearExpired();
			getRealtimeData();
        }, 100);
        return () => clearInterval(interval);
	});
	onDestroy(() => {
		rtlTools.connectionSignal.off(connectionSignal);
		rtlTools.disconnect();
	});

	async function getData() {
		if (apiFacade == null) {
			console.error('apiFacade null');
			return;
		}
		const groupsData = await apiFacade.trainings.getGroups();
		groups = Object.entries(groupsData).map((o) => ({ group: o[0], summaries: o[1] }));
	}

	let lastClick = 0;
	async function onSelectGroup(groupId: string) {
		if (Date.now() - lastClick < 50) return;
		lastClick = Date.now();
		detailedTrainingsData = await apiFacade.trainings.getSummaries(groupId);
		// RealtimelineTools.testData(detailedTrainingsData.map(o => o.id)).forEach(o => rtlTools.append(o));;
		getRealtimeData();
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


	// function xxx():
	//  { id: number, username: string, trainedDays: number, trainedDaysMax: number, accuracy: number, effectiveTime: number }[] {
	// }

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
				comments: [] as Array<{ type: 'Critical' | 'Warning' | 'Info'; description: string }>,
				gender: t.gender,
				consent: t.consent,
				birthDate: t.birthDate,
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
		// console.log('training id', trainingId);
		// goto(`${base}/training?id=${trainingId}`);
	}

	function onCreateGroup() {
		openModal(CreateTrainingsModal, {
			onCreateGroup: (id) => {
				getData();
			}
		});
	}

	async function generatePrompt(id: number, templateSource: string, onlyPrompt: boolean) {
		if (apiFacade == null) {
			console.error('apiFacade null');
			return;
		}
		promptSettings.completion = "Working...";

		const analysis = await apiFacade.trainings.getAiAnalysis(id, templateSource, onlyPrompt);
// 		const test = `
// # Some data
// * 1 here
// * 2 and here
// 		`.trim();
// 		const analysis = { completion: test, prompt: "p" };

		promptSettings.completionHtml = analysis.completion;
		promptSettings.completion = analysis.completion;
		promptSettings.prompt = analysis.prompt;
		try {
			const converter = new showdown.Converter();
			promptSettings.completionHtml = converter.makeHtml(analysis.completion);
			console.log("sd", promptSettings.completionHtml);
		} catch (err) { console.error(err); }
	}

	async function updateTraining(id: number, next: Partial<Training>) {
		const patch = <PatchTrainingDto>{ gender: next.gender, consent: next.consent?.toISOString(), birthDate: next.birthDate };
		// hmm, special for svelte reactivity
		const inMem = trainings.find(t => t.id == id);
		if (inMem) {
			Object.keys(next).filter(o => !!o).forEach(k => {
				(<any>inMem)[k] = (<any>next)[k];
			});
			const index = trainings.findIndex(t => t.id == id);
			if (index >= 0) {
				trainings[index] = inMem;
			}
		}
		await apiFacade.trainings.patch(id, patch);
	}

	function trainingIsEnabled(t: Partial<Training>) {
		return t.gender && t.birthDate && t.birthDate.year > 0 && t.consent;
	}

</script>

<div class="teacher-view">
	<h2>{getString('teacher_groups_header')}</h2>
	{#if showRealtimeButton}
		<button disabled={realtimeConnected == null} on:click={() => rtlTools.toggleConnect()}>{realtimeConnected  == true ? 'Disconnect' : 'Connect'}</button>
	{/if}
	<div style="padding:5px; background-color:#cef; margin: 10px">
	<div>
		The training app is available at <b>{clientUrl}</b> <button on:click={() => navigator.clipboard.writeText(clientUrl)}>Copy link</button> <a href={clientUrl} target="_blank">Open in new window</a>
	</div>
	<div>
		For questions and bug reports, email us at <a href="mailto:vektorproject2026@gmail.com">vektorproject2026@gmail.com</a>
	</div>
	</div>
	{#if groups && groups.length > 0}
		(Total: {groups.map(o => o.summaries.length).reduce((p, c) => p + c)} created, {groups.map(o => o.summaries.filter(p => p.trainedDays > 0).length).reduce((p, c) => p + c)} started)
		<Tabs
			urlParam="group"
			tabs={groups.map((g) => {
				return { id: g.group };
			})}
			on:selected={(e) => { console.log(e); onSelectGroup(e.detail); }}
			>

			<button on:click={onCreateGroup}>
				{getString('teacher_create_group_label')}
			</button>
			<!-- svelte-ignore a11y-invalid-attribute -->
			<!-- <span class="tooltip">
				<a href="#" on:click={() => assistanStore.openWidgetWithFirstSearchHit("limit")}>?</a>
			</span> -->
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
					<!-- <span class="tooltip" data-tooltip={getString('teacher_trainings_column_tooltip_days_trained')} on:click={() => assistanStore.openWidgetWithFirstSearchHit("statistics")}>?</span> -->
				</th>
				<th class="effective-time-column">
					{getString('teacher_trainings_column_header_effective_time')}
					<!-- svelte-ignore a11y-click-events-have-key-events -->
					<!-- <span class="tooltip" data-tooltip={getString('teacher_trainings_column_tooltip_effective_time')} on:click={() => assistanStore.openWidgetWithFirstSearchHit("statistics")}>?</span> -->
				</th>
				<th class="accuracy-column">
					{getString('teacher_trainings_column_header_accuracy')}
					<!-- svelte-ignore a11y-click-events-have-key-events -->
					<!-- <span class="tooltip" data-tooltip={getString('teacher_trainings_column_tooltip_accuracy')} on:click={() => assistanStore.openWidgetWithFirstSearchHit("statistics")}>?</span> -->
				</th>
				{#each Array.from(Array(trainingDayDetailsNumDaysBack).keys()) as dayOffset}
				<th class="training-day-column" title="{DateUtils.toIsoDate(DateUtils.addDays(trainingDayDetails.startDate, dayOffset))}">{DateUtils.getWeekDayName(DateUtils.addDays(trainingDayDetails.startDate, dayOffset))[0]}</th>
				{/each}
				<th>
					Gender
				</th>
				<th>
					Consent
				</th>
				<th>
					Born
				</th>
				<th class="notes-column">
					{getString('teacher_trainings_column_header_notes')}
				</th>
			</tr>
			{#each trainings as t (t.id)}
				<tr on:click={() => onSelectTraining(t.id)} class="training-row">
					<td class="user-column">
						<div style="display: flex">
							{#if trainingIsEnabled(t)}
								{@html Avatar.create(t.username)}
							{:else}
								<span title={`Training settings not filled out! (${t.id})`}>⚠️</span>
							{/if}
							<span title={`{t.id}`} >&nbsp;{t.username}&nbsp;</span>
							{#if showAIEditButton}
							<a rel="noreferrer" href="/admin/teacher/training?id={t.id.toString()}" title="id={t.id.toString()}" target="_blank">^</a>
							{/if}
						</div>
					</td>
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
						<select value={t.gender} on:change={e => updateTraining(t.id, { gender: e.currentTarget.value})}>
							<option value="">Not set</option>
							<option value="m">Male</option>
							<option value="f">Female</option>
							<option value="o">Other</option>
						</select>
					</td>
					<td>
						<input type="checkbox" checked={t.consent != null} on:change={e => updateTraining(t.id, { consent: e.currentTarget.checked ? new Date(Date.now()) : undefined })} />
					</td>
					<td>
						<select value={t.birthDate?.year} on:change={e => updateTraining(t.id, { birthDate: { year: parseInt(e.currentTarget.value) }})}>
						{#each Array(8).fill(0).map((_, i) => i + 4).map(o => new Date().getFullYear() - o) as year}
						<option value={year}>{year}</option>
						{/each}
						</select>
						<select value={t.birthDate?.month} on:change={e => updateTraining(t.id, { birthDate: { year: t.birthDate?.year || 0, month: parseInt(e.currentTarget.value) }})}>
						{#each Array(12).fill(0).map((_, i) => i) as month}
						<option value={month}>{`${month.toString().padStart(2, '0')} ${new Date(2000, month, 1).toLocaleString('default', { month: 'long' })}`}</option>
						{/each}
						</select>
					</td>
					<td>
						{#if showAIButton}
						<button on:click={() => aiDialogForTraining = t}>🤖</button>
						{/if}
						{#if getRealtimeDataForId(t.id).length}
						<Realtimeline history={getRealtimeDataForId(t.id)} getPositioning={RealtimelineTools.createPositioningFunction(5 * 60 * 1000)} ></Realtimeline>
						{/if}
					<!-- {#each t.comments as c}
						{c.description}
					{/each} -->
					</td>
				</tr>
			{/each}
		</table>
	{/if}
	{#if aiDialogForTraining}
	<div role="dialog" class="modal">
		<div class="contents" style="min-height: 40%; min-width: 70%; border-style: solid;">
			<h2>{aiDialogForTraining.username}</h2>
			{#if showAIEditButton && false}
			<label for="template">Template</label>
			<input name="template" type="text" bind:value={promptSettings.template}/>

			------
			<input type="button" on:click={() => { generatePrompt(aiDialogForTraining?.id || 0, promptSettings.template, true) }} value="Generate prompt ⬇️"/>
			<textarea rows="8" cols="100">{promptSettings.prompt}</textarea>

			------

			<input type="button" on:click={() => { generatePrompt(aiDialogForTraining?.id || 0, promptSettings.template, false) }} value="Send to LLM ⬇️"/>
			<label for="model">Model</label>
			<input name="model" type="text"/>
			<textarea rows="8" cols="100">{promptSettings.completion}</textarea>
			------
			{/if}
			{#if aiDialogForTraining.trainedDays < 3}
			<div>Analysis tool available after 3 training days</div>
			{:else}
			<button type="button" on:click={() => { generatePrompt(aiDialogForTraining?.id || 0, promptSettings.template, false) }}>🤖 Analyze</button>
			<div>
				{#if promptSettings.completionHtml}
				{@html promptSettings.completionHtml}
				{:else}
				(click above to analyze training)
				{/if}
			</div>
			{/if}

			<input type="button" on:click={() => { aiDialogForTraining = null; }} value="Close"/>
		</div>
	</div>
	{/if}
</div>

<style>
	.modal {
		z-index: 10;
		position: fixed;
		top: 0;
		bottom: 0;
		right: 0;
		left: 0;
		display: flex;
		justify-content: center;
		align-items: center;
		/* allow click-through to backdrop */
		pointer-events: none;
	}
	.contents {
		min-width: 240px;
		border-radius: 6px;
		padding: 16px;
		background: white;
		display: flex;
		flex-direction: column;
		justify-content: space-between;
		pointer-events: auto;
	}


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
