<script lang="ts">
	import { goto } from '$app/navigation';
	import type { CreatedUserInfo, CreateUserDto, FileEntry, FileResponse, GetUserDto, PatchUserDto, TelemetryItem } from '../../apiClient';
	import type { ApiFacade } from '../../apiFacade';
	import { getApi, userStore } from '../../globalStore';
	import { onMount } from 'svelte';
	import { DateUtils } from '../../utilities/DateUtils';

	const apiFacade = getApi() as ApiFacade;

	let created: CreatedUserInfo[]; // { email: string, password: string }[]
	const currentRole = $userStore?.role || "";
	console.log("currentRole", currentRole);
	let errors: TelemetryItem[] = [];
	let errorsByPeriod: { when: string; items: TelemetryItem[] }[] = [];
	let errorsByUser: { user: string; items: TelemetryItem[] }[] = [];
	let errorsByType: { type: string; items: TelemetryItem[] }[] = [];

	let cloneTrainingsConfig: { ids: string, targetUser?: string, targetGroup?: string } = { ids: "" };
	// function createUser(email: string, password: string) {
	// 	apiFacade.users.post(<CreateUserDto>{ username: email, password: password }).then(() => console.log('user created'));
	// }
	async function createUsers(emails: string) {
		const emailArray = emails.split(/(\s|;|,)/).map(o => o.trim()).filter(o => o.length > 3);
		const result = await apiFacade.users.postCreateUsers({ emails: emailArray });
		created = result.emails;
	}
	function changePassword(email: string, password?: string | null) {
		if (!password) {
			password = prompt('Password', '');
		}
		if (!password || password.length <= 5) {
			alert('too short');
		} else {
			patchUser(email, { password: password })
				.then((r) => console.log('pwd changed', r));
		}
	}
	async function patchUser(email: string, dto: PatchUserDto) {
		return await apiFacade.users.patch(email, dto);
	}

	function resetMFA(email: string) {
		if (!confirm("Reset MFA code?"))
			return;
		patchUser(email, { mfaSecretKey: "" })
		//apiFacade.users.patch(email, <PatchUserDto>{ mfaSecretKey: "" })
			.then((r) => console.log('reset MFA', r));
	}
	function setMFAEnabled(email: string, enabled: boolean) {
		patchUser(email, { mfaEnabled: enabled })
			.then((r) => console.log('MFA enabled changed', r));
	}

	async function cloneTraining() {
		console.log("clone trainings", cloneTrainingsConfig);
		const result = await apiFacade.trainings.postClone(cloneTrainingsConfig.ids, cloneTrainingsConfig.targetUser, cloneTrainingsConfig.targetGroup);
		console.log("created", result);
	}

	async function exportTrainingData(user?: GetUserDto) {
		const trainingIds = user == null ? null : Object.keys(user.trainings).map(o => user.trainings[o]).flat();
		const url = apiFacade.getUrl("/api/Trainings/trialdata", { trainingIds: trainingIds?.join(",") });
		console.log("url", url);
		// 	https://localhost:7174/api/Trainings/trialdata?trainingIds=420,421
		window.open(url, "_blank");
		// const result = await apiFacade.trainings.getTrialData(trainingIds?.join(","), null);
		// console.log("Result", result);
	}

	async function listGeneratedFiles() {
		return await apiFacade.files.list("");
	}
	async function downloadFile(file: string) {
		const url = apiFacade.getUrl("/api/Files", { subPath: file });
		window.open(url, "_blank");
	}

	const getElementValue = (id: string) => (<HTMLInputElement>document.getElementById(id)).value;

	let users: GetUserDto[] = [];
	let files: FileEntry[] = []; 
	onMount(async () => {
		files = await listGeneratedFiles();
		users = await apiFacade.users.getAll();
		apiFacade.telemetry.get(null, new Date(Date.now() - 24 * 60 * 60 * 1000)).then(result => {
			errors = result;
			const now  = Date.now();
			const grouped = Map.groupBy(errors, (item) => {
                return DateUtils.getTimeDiffCategory(now - new Date(item.created).valueOf()).msRounded;
            });
            errorsByPeriod = [...grouped]
                .sort((a, b) => a[0] - b[0])
                .map(o => ({ when: DateUtils.getTimeDiffCategory(o[0]).name, items: o[1] }));

			errorsByUser = [...Map.groupBy(errors, (item) => item.username?.trim() || "")]
				.map(o => ({ user: o[0], items: o[1]})); // .sort((a, b) => a.modified)

			errorsByType = [...Map.groupBy(errors, (item) => item.error?.message || "")]
				.map(o => ({ type: o[0], items: o[1]})); // .sort((a, b) => a.modified)

		});
	});
</script>

<div>
	<h2>Create users</h2>
	Emails:<textarea id="email" value=""></textarea>
	<input type="button" value="Create" on:click={() => createUsers(getElementValue('email'))} />
	{#if created}
		<ul>
		{#each created as c}
		<li>{c.email}: {c.password}</li>
		{/each}
		</ul>
	{/if}
</div>

<div>
<details>
	<summary>Server files</summary>
	{#each files as file}
		<div><a href={file.downloadPath} target="_blank">{file.name}</a> ({Math.ceil(file.length / 1024)} kB)</div>
	{/each}
</details>
</div>

{#if currentRole == "SuperAdmin"}
<div>
	<h2>Clone trainings</h2>
	Ids: <input type="text" bind:value={cloneTrainingsConfig.ids}/>
	Target user: <input type="text" bind:value={cloneTrainingsConfig.targetUser}/>
	Target group: <input type="text" bind:value={cloneTrainingsConfig.targetGroup} />
	<button on:click={() => cloneTraining()}>Clonse</button>
</div>

<div>
	<details>
		<summary>Errors</summary>
		{#each errorsByPeriod || [] as period}
		<details>
			<summary>{period.when} {period.items.length}</summary>
			<table>
			{#each period.items as item}
				<tr>
					<td>{item.created}</td>
					<td>{item.username}</td>
					<td>{item.error?.message}</td>
					<td>{JSON.stringify(item.error?.clientInfo)}</td>
				</tr>
			{/each}
			</table>
		</details>
		{/each}

		<details>
			<summary>Users: {errorsByUser.length}</summary>
			{#each errorsByUser as usr}
			<details>
				<summary>{usr.user} {usr.items.length}</summary>
				<table>
				{#each usr.items as item}
					<tr>
						<td>{item.created}</td>
						<td>{item.username}</td>
						<td>{item.error?.message}</td>
						<td>{JSON.stringify(item.error?.clientInfo)}</td>
					</tr>
				{/each}
				</table>
			</details>
			{/each}
		</details>

		<details>
			<summary>Messages: {errorsByType.length}</summary>
			{#each errorsByType as itemX}
			<details>
				<summary>{itemX.type} {itemX.items.length}</summary>
				<table>
				{#each itemX.items as item}
					<tr>
						<td>{item.created}</td>
						<td>{item.username}</td>
						<td>{item.error?.message}</td>
						<td>{JSON.stringify(item.error?.clientInfo)}</td>
					</tr>
				{/each}
				</table>
			</details>
			{/each}
		</details>
	</details>
</div>
{/if}

<table style="">
	<thead>
	<td>Username</td>
	<td>Role</td>
	<td>Default TP</td>
	<td>Trainings</td>
	<td></td>
	</thead>
	<tbody>
{#each users as user}
<tr>
	<!-- <td><a href={`teacher?impersonate=${encodeURIComponent(user.username)}`}>{user.username}</a></td> -->
	<td><a on:click={() => { apiFacade.impersonateUser = user.username; goto(`teacher?impersonate=${encodeURIComponent(user.username)}`)}}>{user.username}</a></td>
	<td>
	{#if currentRole == "SuperAdmin"}
		<select value={user.role} on:change={e => patchUser(user.username, { role: e.currentTarget.value})}>
			<option value="">Not set</option>
			<option value="Teacher">Teacher</option>
			<option value="Admin">Admin</option>
			<option value="SuperAdmin">SuperAdmin</option>
		</select>
	{:else}
		{user.role}
	{/if}
	</td>
	<td>{user.defaultTrainingPlanName}</td>
	<td style="word-wrap: break-word; max-width: 450px;">{JSON.stringify(user.trainings)}</td>
	<td>
		<input type="button" value="Pwd" on:click={() => changePassword(user.username)} />
		<input type="button" title="Reset MFA code" disabled={!user.hasMfaSecretKey} value="MFA" on:click={() => resetMFA(user.username)} />
		<input type="checkbox" title="MFA enabled" bind:checked={user.mfaEnabled} on:click={e => setMFAEnabled(user.username, e.currentTarget.checked)} />
		<input type="button" title="Export trainings" value="Xprt" on:click={() => exportTrainingData(user)} />
	</td>
</tr>
{/each}
	</tbody>
</table>


<button on:click={async () => await apiFacade.testing.throwException()}>Error</button>
