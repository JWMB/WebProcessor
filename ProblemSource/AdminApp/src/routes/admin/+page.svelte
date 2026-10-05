<script lang="ts">
	import { goto } from '$app/navigation';
import type { CreatedUserInfo, CreateUserDto, GetUserDto, PatchUserDto, TelemetryItem } from '../../apiClient';
	import type { ApiFacade } from '../../apiFacade';
	import { getApi, userStore } from '../../globalStore';
	import { onMount } from 'svelte';
	import { DateUtils } from '../../utilities/DateUtils';

	const apiFacade = getApi() as ApiFacade;

	let created: CreatedUserInfo[]; // { email: string, password: string }[]
	const currentRole = $userStore?.role || "";
	let errors: TelemetryItem[] = [];
	let errorsByPeriod: { when: string; items: TelemetryItem[] }[];

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
		apiFacade.users.patch(email, <PatchUserDto>{ mfaSecretKey: "" }).then((r) => console.log('reset MFA', r));
	}

	const getElementValue = (id: string) => (<HTMLInputElement>document.getElementById(id)).value;

	let users: GetUserDto[] = [];
	onMount(async () => {
		users = await apiFacade.users.getAll();
		apiFacade.telemetry.get(null).then(result => {
			errors = result;
			const now  = Date.now();
			const grouped = Map.groupBy(errors, (item) => {
                return DateUtils.getTimeDiffCategory(now - new Date(item.created).valueOf()).msRounded;
            });
            errorsByPeriod = [...grouped]
                .sort((a, b) => a[0] - b[0])
                .map(o => ({ when: DateUtils.getTimeDiffCategory(o[0]).name, items: o[1] }));
			console.log(errorsByPeriod);
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
</div>

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
	{#if currentRole != "SuperAdmin"}
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
		<input type="button" title="Reset MFA code" value="MFA" on:click={() => changePassword(user.username)} />
	</td>
</tr>
{/each}
	</tbody>
</table>


<button on:click={async () => await apiFacade.testing.throwException()}>Error</button>
