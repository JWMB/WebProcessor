<script lang="ts">
	import { apiFacade as apiFacadeStore, loggedInUser } from '../../../globalStore';
	import { get } from 'svelte/store';
	import { onMount } from 'svelte';
	import type { TrainingSummaryWithDaysDto, TrainingSummaryDto, TrainingCreateDto, TrainingTemplateDto } from 'src/apiClient';
	import TrainingsTable from '../../../components/trainingsTable.svelte';
	import TrainingGroupsTable from '../../../components/trainingGroupsTable.svelte';
	import { goto } from '$app/navigation';
	import { base } from '$app/paths';

	// let trainingSummaries: TrainingSummary[] = [];

	const apiFacade = get(apiFacadeStore);

	const loggedInUserInfo = get(loggedInUser);

	let templates: TrainingTemplateDto[] = [];
	let trainingsPromise: Promise<TrainingSummaryWithDaysDto[]>;
	// let trainingGroupsPromise: Promise<{[key: string]: TrainingSummaryDto[] }>;
	let trainingGroupsPromise2: Promise<{ group: string; summaries: TrainingSummaryDto[] }[]>;

	let trainingGroups: { group: string; summaries: TrainingSummaryDto[] }[] = [];
	let createdTrainingUsernames: string[] = [];

	const clickedGroupRow = (e: CustomEvent<any>) => {
		trainingsPromise = apiFacade.trainings.getSummaries(e.detail.group);
	};
	const clickedTrainingRow = (e: CustomEvent<any>) => {
		goto(`${base}/training?id=${e.detail.id}`);
	};

	async function createTrainings(num: number, groupName: string, numMinutes: number, templateId: number, forUser?: string | null) {
		if (!groupName) {
			alert('A name is required');
			return;
		}
		console.log("")
		const chosenTemplate = templates.filter(o => o.id == templateId)[0];
		if (confirm(`Create class '${groupName}' with ${num} trainings using template '${chosenTemplate.name}' (${chosenTemplate.trainingPlanName})?`)) {
			//const templates = await apiFacade.trainings.getTemplates();
			if (!chosenTemplate.settings) {
				chosenTemplate.settings = { timeLimits: [33], cultureCode: 'sv-SE' };
			}
			chosenTemplate.settings.timeLimits = [numMinutes];
			const dto = <TrainingCreateDto>{ baseTemplateId: chosenTemplate.id, trainingPlan: chosenTemplate.trainingPlanName, trainingSettings: chosenTemplate.settings };
			createdTrainingUsernames = await apiFacade.trainings.postGroup(dto, groupName, num, forUser);
			await getTrainings();
		}
	}

	function getElementValue(id: string) {
		const el = document.getElementById(id);
		if (el == null) throw `Element not found: ${id}`;
		return (<HTMLInputElement>el).value;
	}

	async function getTrainings() {
		if (apiFacade == null) {
			console.error('apiFacade null');
			return;
		}
		//trainingsPromise = apiFacade.trainings.getSummaries();

		trainingGroupsPromise2 = new Promise((res) => {
			apiFacade.trainings.getGroups().then((r) => {
				const asList = Object.entries(r).map((o) => ({ group: o[0], summaries: o[1] }));
				trainingGroups = asList;
				res(asList);
			});
		});
		// trainingGroupsPromise = apiFacade.trainings.getGroups();
		//trainingSummaries = await apiFacade.trainings.getSummaries();
		//console.log("OK", trainingSummaries.length); // why is TrainingsTable not always updated?
	}

	async function init() {
		await getTrainings();
		templates = await apiFacade.trainings.getTemplates();
	}

	onMount(() => init());
</script>

<div>
	<h2>Classes</h2>
	<div>
		Create class: <input id="className" type="text" value="Fsk A" />
		num trainings: <input id="numTrainings" style="width:40px;" type="number" min="1" max="30" value="10" />
		template: <select id="template">
			{#each templates as template}
				<option value="{template.id}">{template.name}</option>
			{/each}
		</select>
		time per day: <input id="timePerDay" style="width:40px;" type="number" min="15" max="45" value="33" />
		{#if loggedInUserInfo?.role == 'Admin'}
			create for user: <input id="forUser" type="text" />
		{/if}
		<input
			type="button"
			value="Create"
			on:click={() => createTrainings(
				parseFloat(getElementValue('numTrainings')),
				getElementValue('className'),
				parseFloat(getElementValue('timePerDay')),
				parseFloat(getElementValue('template')),
				loggedInUserInfo?.role == 'Admin' ? getElementValue('forUser') : null)} />

		{#if createdTrainingUsernames}
			Created users:
			{#each createdTrainingUsernames as username}
				<li>{username}</li>
			{/each}
		{/if}
	</div>

	{#await trainingGroupsPromise2}
		<div>Loading...</div>
	{:then trainings}
		{#if !!trainings && trainings.length > 0}
			<TrainingGroupsTable trainingSummaries={trainings} on:clickedRow={clickedGroupRow} />
		{/if}
	{:catch error}
		{error}
	{/await}

	<!-- <TrainingsTable trainingSummaries={trainingSummaries} numDays={5}></TrainingsTable> -->
	{#await trainingsPromise}
		<div>Loading...</div>
	{:then trainings}
		{#if !!trainings && trainings.length > 0}
			<h2>Trainings</h2>
			<TrainingsTable trainingSummaries={trainings} numDays={14} on:clickedRow={clickedTrainingRow} />
		{/if}
	{:catch error}
		{error}
	{/await}
</div>
