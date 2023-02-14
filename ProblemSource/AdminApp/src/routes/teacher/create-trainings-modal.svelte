<script lang="ts">
	import { closeModal } from 'svelte-modals';
	import type { TrainingCreateDto } from 'src/apiClient';
	import { get } from 'svelte/store';
	import { getApi } from 'src/globalStore';
	import type { ApiFacade } from 'src/apiFacade';

	export let isOpen: boolean; // provided by Modals
	export let onCreateGroup: (id: string) => void;

	const apiFacade = getApi() as ApiFacade;

	let newGroupData = {
		name: 'Fsk A',
		noOfTrainings: 10,
		timePerDay: 33
	};

	let createdTrainingUsernames: string[] = [];
	async function createTrainings(num: number, groupName: string, numMinutes: number, forUser?: string | null) {
		const templates = await apiFacade.trainings.getTemplates();
		const chosenTemplate = templates[0];
		if (!chosenTemplate.settings) {
			chosenTemplate.settings = { timeLimits: [33], cultureCode: 'sv-SE' };
		}
		chosenTemplate.settings.timeLimits = [numMinutes];
		const dto = <TrainingCreateDto>{ trainingPlan: chosenTemplate.trainingPlanName, trainingSettings: chosenTemplate.settings };
		createdTrainingUsernames = await apiFacade.trainings.postGroup(dto, groupName, num, forUser);
		closeModal;
		onCreateGroup(groupName);
	}
</script>

{#if isOpen}
	<div role="dialog" class="modal">
		<div class="contents">
			{#if createdTrainingUsernames.length === 0}
				<h2>Create group</h2>
				<p style="color:darkorange">Creating class trainings will be available shortly - <br/>we are currently finalizing the design of the training plan!</p>
				<form action="javascript:void(0);">
					<label>
						Class/group name
						<input id="className" type="text" required bind:value={newGroupData.name} />
					</label>
					<label>
						Number of trainings
						<input id="numTrainings" required bind:value={newGroupData.noOfTrainings} min="1" max="40" />
					</label>
					<!-- <label>
						Time per day
						<input id="timePerDay" required type="number" bind:value={newGroupData.timePerDay} min="15" max="45" />
					</label> -->
					<div class="actions">
						<button class="secondary" on:click={closeModal}>Cancel</button>
						<button class="primary" disabled style="opacity:0.5" type="submit" value="Create" on:click={() => createTrainings(newGroupData.noOfTrainings, newGroupData.name, newGroupData.timePerDay, '')}>Create</button>
					</div>
				</form>
			{:else}
				<h2>Created users</h2>
				<p>The following user names has been created, copy and paste them fo later reference.</p>
				{#each createdTrainingUsernames as username}
					<li>{username}</li>
				{/each}
				<div class="actions">
					<button type="submit" on:click={closeModal}>Close</button>
				</div>
			{/if}
		</div>
	</div>
{/if}

<style>
	h2 {
		margin-top: 0;
	}

	input {
		display: block;
		margin-top: 2px;
		margin-bottom: 6px;
		width: 100%;
		border-radius: 0;
		border: 1px solid #bebebe;
		height: 30px;
	}

	label {
		font-size: 12px;
		font-weight: bold;
	}

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

	.actions {
		margin-top: 32px;
		display: flex;
		gap: 5px;
		justify-content: flex-end;
	}
</style>
