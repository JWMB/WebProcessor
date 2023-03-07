<script lang="ts">
	import { closeModal } from 'svelte-modals';
	import type { TrainingCreateDto, TrainingTemplateDto } from 'src/apiClient';
	import { getApi } from 'src/globalStore';
	import type { ApiFacade } from 'src/apiFacade';
	import { onMount } from 'svelte';
	import { ErrorHandling } from 'src/errorHandling';

	export let isOpen: boolean; // provided by Modals
	export let onCreateGroup: (id: string) => void;

	const apiFacade = getApi() as ApiFacade;

	let templates: TrainingTemplateDto[] = [];
	let error: string | null = null;

	const timePerDayValues = [
		20,
		33
	];
	const ageBrackets = [
		"",
		"-4",
		"4-5",
		"5-6",
		"6-7",
		"7-8",
		"8-9",
		"9-10",
		"10-11",
		"11-",
	];
	let newGroupData = {
		name: 'Fsk A',
		noOfTrainings: 10,
		timePerDay: 33,
		ageBracket: ""
	};

	let createdTrainingUsernames: string[] = [];
	async function createTrainings(num: number, groupName: string, numMinutes: number, ageBracket: string, forUser?: string | null) {
		error = null;
		try {
			if (!ageBracket) throw "Age span must be set";
			const chosenTemplate = templates[0];
			// if (!chosenTemplate.settings) {
			// 	chosenTemplate.settings = { timeLimits: [33], cultureCode: 'sv-SE' };
			// }
			// TODO: server-side serializiation of TrainingSettings.trainingPlanOverrides is incorrect, so we can't use it here
			chosenTemplate.settings.trainingPlanOverrides = null;
			chosenTemplate.settings.timeLimits = [numMinutes];
			const dto = <TrainingCreateDto>{ 
				baseTemplateId: chosenTemplate.id,
				trainingPlan: chosenTemplate.trainingPlanName,
				trainingSettings: chosenTemplate.settings,
				ageBracket: ageBracket
			};
			createdTrainingUsernames = await apiFacade.trainings.postGroup(dto, groupName, num, forUser);
		} catch (err) {
			error = ErrorHandling.getErrorObject(err).message;
			throw err;
		}
		closeModal;
		onCreateGroup(groupName);
	}

	onMount(async () => {
		templates = await apiFacade.trainings.getTemplates();
	});
</script>

{#if isOpen}
	<div role="dialog" class="modal">
		<div class="contents">
			{#if createdTrainingUsernames.length === 0}
				<h2>Create group</h2>
				<form action="javascript:void(0);">
					<label>
						Class/group name
						<input id="className" type="text" required bind:value={newGroupData.name} />
					</label>
					<label>
						Age span
						<br/>
						<select bind:value={newGroupData.ageBracket}>
							{#each ageBrackets as ageBracket}
							<option value={ageBracket}>{ageBracket}</option>
							{/each}
						</select>
						<br/>
						<br/>
					</label>
					<label>
						Number of trainings
						<input id="numTrainings" required bind:value={newGroupData.noOfTrainings} min="1" max="40" />
					</label>
					<label>
						Time per day
						<br/>
						<select id="timePerDay" bind:value={newGroupData.timePerDay}>
							{#each timePerDayValues as tpd}
							<option value="{tpd}">{tpd} minutes</option>
							{/each}
						</select>
						<!-- <input id="timePerDay" required type="number" bind:value={newGroupData.timePerDay} min="15" max="45" /> -->
					</label>
					{#if !!error}
						<div style="color:red">{error}</div>
					{/if}
					<div class="actions">
						<button class="secondary" on:click={closeModal}>Cancel</button>
						<button class="primary" style="opacity:1" type="submit" value="Create" on:click={() => createTrainings(newGroupData.noOfTrainings, newGroupData.name, newGroupData.timePerDay, newGroupData.ageBracket, '')}>Create</button>
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
