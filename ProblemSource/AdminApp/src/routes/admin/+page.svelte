<script lang="ts">
	import type { CreateUserDto, GetUserDto, PatchUserDto } from '../../apiClient';
	import type { ApiFacade } from '../../apiFacade';
	import { getApi } from '../../globalStore';
	import { onMount } from 'svelte';

	const apiFacade = getApi() as ApiFacade;

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
			apiFacade.users.patch(email, <PatchUserDto>{ password: password }).then((r) => console.log('pwd changed', r));
		}
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

{#each users as user}
	<li>{user.username} {user.role} {JSON.stringify(user.trainings)}</li>
	<input type="button" value="Pwd" on:click={() => changePassword(user.username)} />
{/each}

<button on:click={async () => await apiFacade.testing.throwException()}>Error</button>
