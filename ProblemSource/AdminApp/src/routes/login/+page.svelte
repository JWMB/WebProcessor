<script lang="ts">
	import { getApi, userStore } from 'src/globalStore';

	let email = '';
	let password = '';

	let isLoading = false;
	let isSuccess = false;

	let errors = { email: '', password: '', server: '' };

	const handleSubmit = () => {
		Object.keys(errors).forEach((k) => ((<any>errors)[k] = null));

		if (email.length === 0) {
			errors.email = 'Field should not be empty';
		}
		if (password.length === 0) {
			errors.password = 'Field should not be empty';
		}

		if (Object.keys(errors).filter((k) => !!(<any>errors)[k]).length === 0) {
			isLoading = true;
			userStore
				.login({ username: email, password: password })
				.then((r) => {
					isSuccess = true;
					isLoading = false;
				})
				.catch((err) => {
					errors.server = err;
					isLoading = false;
				});
		}
	};
</script>

<div class="page-area">
	<form on:submit|preventDefault={handleSubmit}>
		{#if isSuccess}
			<div class="success">
				🔓
				<br />
				You've been successfully logged in.
			</div>
		{:else}
			<label
				>Email
				<input name="email" placeholder="name@example.com" bind:value={email} />
			</label>
			<label
				>Password
				<input name="password" type="password" bind:value={password} />
			</label>
			<button type="submit">
				{#if isLoading}Logging in...{:else}Log in{/if}
			</button>

			{#if Object.keys(errors).length > 0}
				<ul class="errors">
					{#each Object.entries(errors) as [field, value]}
						<li>{field}: {value}</li>
					{/each}
				</ul>
			{/if}
		{/if}
	</form>
</div>

<style>
	.page-area {
		position: absolute;
		top: 0;
		bottom: 0;
		left: 0;
		right: 0;
		display: flex;
		justify-content: center;
		align-items: center;
		background: white;
	}
	form {
		background: #fff;
		width: 250px;
		min-height: 300px;
		margin: auto 0;
	}

	input {
		display: block;
		width: 100%;
		margin-bottom: 10px;
	}
	button {
		margin-bottom: 10px;
	}

	.errors {
		list-style-type: none;
		padding: 10px;
		margin: 0;
		border: 2px solid #be6283;
		color: #be6283;
		background: rgba(190, 98, 131, 0.3);
	}

	.success {
		font-size: 24px;
		text-align: center;
	}
</style>
