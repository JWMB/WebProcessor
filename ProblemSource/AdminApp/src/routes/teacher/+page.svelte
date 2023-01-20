<script lang="ts">
	import { apiFacade as apiFacadeStore, loggedInUser } from '../../globalStore';
	import { get } from 'svelte/store';
	import { onMount } from 'svelte';
	import type { TrainingSummaryWithDaysDto, TrainingSummaryDto, TrainingCreateDto } from 'src/apiClient';
	import TrainingsTable from '../../components/trainingsTable.svelte';
	import { goto } from '$app/navigation';
	import { base } from '$app/paths';
	import Tabs from 'src/components/tabs.svelte';
	import ProgressBar from './progress-bar.svelte';

	const apiFacade = get(apiFacadeStore);
	const loggedInUserInfo = get(loggedInUser);

	let detailedTrainingsData: TrainingSummaryWithDaysDto[] = [];
	let noOfDays = 7;
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
		detailedTrainingsData = await apiFacade.trainings.getSummaries(groupId);
	}

	function calculateTrainingStats(data: TrainingSummaryWithDaysDto[], numberOfDays = 7) {
		return data.map((t) => {
			return {
				id: t.id,
				username: t.username,
				trainedDays: t.days.length,
				trainedDaysMax: 30,
				accuracy: 10,
				effectiveTime: 22,
				totalTime: 32,
				comments: [] as Array<{ type: 'Critical' | 'Warning' | 'Info'; description: string }>
			};
		});
	}

	const onSelectTraining = (trainingId: number) => {
		goto(`${base}/training?id=${trainingId}`);
	};

	let newGroupData = {
		name: 'Fsk A',
		noOfTrainings: 10,
		timePerDay: 10
	};
	let createdTrainingUsernames: string[] = [];
	async function createTrainings(num: number, groupName: string, numMinutes: number, forUser?: string | null) {
		if (!groupName) {
			alert('A name is required');
			return;
		}
		if (confirm(`Create class '${groupName}' with ${num} trainings?`)) {
			const templates = await apiFacade.trainings.getTemplates();
			const chosenTemplate = templates[0];
			if (!chosenTemplate.settings) {
				chosenTemplate.settings = { timeLimits: [33], cultureCode: 'sv-SE' };
			}
			chosenTemplate.settings.timeLimits = [numMinutes];
			const dto = <TrainingCreateDto>{ trainingPlan: chosenTemplate.trainingPlanName, trainingSettings: chosenTemplate.settings };
			createdTrainingUsernames = await apiFacade.trainings.postGroup(dto, groupName, num, forUser);
			await getData();
		}
	}

	function getElementValue(id: string) {
		return (<HTMLInputElement>document.getElementById(id)).value;
	}
</script>

<div class="teacher-view">
	<h2>Your groups/classes</h2>
	<div>
		Create class: <input id="className" type="text" bind:value={newGroupData.name} />
		Number of trainings: <input id="numTrainings" bind:value={newGroupData.noOfTrainings} min="1" max="40" />
		Time per day: <input id="timePerDay" type="number" bind:value={newGroupData.timePerDay} min="15" max="45" />
		<button value="Create" on:click={() => createTrainings(newGroupData.noOfTrainings, newGroupData.name, newGroupData.timePerDay, '')}>Create</button>

		{#if createdTrainingUsernames}
			Created users:
			{#each createdTrainingUsernames as username}
				<li>{username}</li>
			{/each}
		{/if}
	</div>

	{#if groups && groups.length > 0}
		<Tabs
			urlParam="group"
			tabs={groups.map((g) => {
				return { id: g.group };
			})}
			on:selected={(e) => onSelectGroup(e.detail)} />
	{/if}

	{#if trainings && trainings.length > 0}
		<h2>Trainings</h2>
		<table>
			<tr>
				<th>User</th>
				<th>Days trained</th>
				<th>Effective time/day</th><th>Accuracy</th><th>Notes</th>
			</tr>
			{#each trainings as t (t.id)}
				<tr on:click={() => onSelectTraining(t.id)}>
					<td>{t.username}</td>
					<td><ProgressBar value={t.trainedDays} max={t.trainedDaysMax} suffix="" color="#00ff00" /></td>
					<td>{t.accuracy}/100</td>
					<td>
						{#each t.comments as c}
							{c.description}
						{/each}</td>
				</tr>
			{/each}
		</table>
		<!-- <TrainingsTable trainingSummaries={trainings} numDays={14} on:clickedRow={clickedTrainingRow} /> -->
	{/if}
</div>

<style>
	.teacher-view {
		padding: 20px;
	}
	table {
		height: 100%;
		text-align: left;
		width: 100%;
		border-spacing: 0;
		border-collapse: collapse;
	}
	tr {
		height: 25px;
	}
	tr:nth-child(even) {
		background: #eeeeee;
	}
	th,
	td {
		margin-right: 10px;
		padding: 3px 0;
	}
</style>
