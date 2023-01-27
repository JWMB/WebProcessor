<script lang="ts">
	import { apiFacade as apiFacadeStore, loggedInUser } from '../../globalStore';
	import { get } from 'svelte/store';
	import { onMount } from 'svelte';
	import type { TrainingSummaryWithDaysDto, TrainingSummaryDto } from 'src/apiClient';
	import { goto } from '$app/navigation';
	import { base } from '$app/paths';
	import Tabs from 'src/components/tabs.svelte';
	import ProgressBar from './progress-bar.svelte';
	import CreateTrainingsModal from './create-trainings-modal.svelte';
	import { openModal } from 'svelte-modals';

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
		console.log('onSelectGroup', groupId);
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

	function onSelectTraining(trainingId: number) {
		goto(`${base}/training?id=${trainingId}`);
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
	<h2>Your groups/classes</h2>

	{#if groups}
		<Tabs
			urlParam="group"
			tabs={groups.map((g) => {
				return { id: g.group };
			})}
			on:selected={(e) => onSelectGroup(e.detail)}
			><button on:click={onCreateGroup}>Create new group</button>
		</Tabs>
	{/if}

	{#if trainings}
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
					<td><ProgressBar value={t.trainedDays} max={t.trainedDaysMax} suffix="" decimals={0} color="#49e280" /></td>
					<td><ProgressBar value={t.accuracy} max={100} suffix="%" decimals={0} color="#49e280" /></td>
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
	table {
		height: 100%;
		text-align: left;
		width: 100%;
		border-spacing: 0;
		border-collapse: collapse;
	}
	tr {
		height: 32px;
	}
	th,
	td {
		margin-right: 10px;
		padding: 6px 10px 6px 0;
	}
</style>
