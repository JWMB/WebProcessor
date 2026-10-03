<script lang="ts">
	import type { CreateUserDto, GetUserDto, PatchUserDto } from '../../apiClient';
	import type { ApiFacade } from '../../apiFacade';
	import { getApi, userStore } from '../../globalStore';
	import { onMount } from 'svelte';

	const apiFacade = getApi() as ApiFacade;

	const currentRole = $userStore?.role || "";
	function createUser(email: string, password: string) {
		apiFacade.users.post(<CreateUserDto>{ username: email, password: password }).then(() => console.log('user created'));
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
	});
</script>

<div>
	<h2>Create user</h2>
	Email:<input id="email" type="text" value="" />
	Password: <input id="password" style="width:40px;" type="text" />
	<input type="button" value="Create" on:click={() => createUser(getElementValue('email'), getElementValue('password'))} />
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
	<td><a href={`teacher?impersonate=${encodeURIComponent(user.username)}`}>{user.username}</a></td>
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
