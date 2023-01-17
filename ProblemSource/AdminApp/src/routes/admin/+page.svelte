<script lang="ts">
	import type { CreateUserDto, GetUserDto } from 'src/apiClient';
	import { onMount } from 'svelte';
    import { get } from 'svelte/store';
    import { apiFacade as apiFacadeStore } from '../../globalStore';

    const apiFacade = get(apiFacadeStore);

    function createUser(email: string, password: string) {
        apiFacade.accounts.post(<CreateUserDto>{ username: email, password: password }).then(() => console.log("user created"));
    }

    const getElementValue = (id: string) => (<HTMLInputElement>document.getElementById(id)).value;

    let users: GetUserDto[] = [];
    onMount(async () => {
        users = await apiFacade.accounts.getAll();
    });
</script>

<div>
    <h2>Create user</h2>
    Email: <input id="email" type="text" value="">
    Password: <input id="password" style="width:40px;" type="text">
    <input type="button" value="Create" on:click={() => createUser(getElementValue("email"), getElementValue("password"))}>
</div>

{#each users as user}
    <li>{user.username} {user.role} {JSON.stringify(user.trainings)}</li>
{/each}

<button on:click={async() => await apiFacade.testing.throwException()}></button>