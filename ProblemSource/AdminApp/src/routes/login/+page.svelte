<script lang="ts">
	import { goto } from '$app/navigation';
	import { ErrorHandling } from '../../errorHandling';
	import { userStore } from '../../globalStore';
	import { initWidgetImplementationScript } from '../../humany-embed';
	import { getString } from '../../utilities/LanguageService';

	let email = '';
	let password = '';

	let isLoading = false;
	let isSuccess = false;

	let errors: { email?: string; password?: string; server?: string } = {};

	const handleSubmit = () => {
		Object.keys(errors).forEach((k) => ((<any>errors)[k] = null));

		if (email.length === 0) {
			errors.email = getString('login_error_email_missing');
		}
		if (password.length === 0) {
			errors.password = getString('login_error_password_missing');
		}

		if (Object.keys(errors).filter((k) => !!(<any>errors)[k]).length === 0) {
			isLoading = true;
			userStore
				.login({ username: email, password: password })
				.then(() => {
					isSuccess = true;
					isLoading = false;
					initWidgetImplementationScript(); // since we don't want to show help widget to non-authorized users
					// TODO: can't find a way to preserve url parameters (e.g. using ?returnUrl= to get back to the attempted page)
					// window.history.back();

					const url = new URL(location.href);
					const baseUrl = url.pathname.replace("/login", "");
					window.location.href = url.searchParams.get('returnUrl') != null
						? `${baseUrl}${url.searchParams.get('returnUrl')}`
						: `${baseUrl}/teacher`
					//handleRedirects('/login');
					//goto('/teacher');
				})
				.catch((err: any) => {
					isLoading = false;
					errors.server = ErrorHandling.getErrorObject(err).message || "Unknown error";
				});
		}
	};
</script>

<div class="page-area">
	<form on:submit|preventDefault={handleSubmit}>
		{#if isSuccess}
			<div class="success">
				{getString('login_success_text')}
			</div>
		{:else}
			<label
				>{getString('login_label_email')}
				<input name="email" placeholder="name@example.com" bind:value={email} />
			</label>
			<label
				>{getString('login_label_password')}
				<input name="password" type="password" bind:value={password} />
			</label>
			<button type="submit">
				{#if isLoading}
					{getString('login_button_loading')}
				{:else}
					{getString('login_button_label')}
				{/if}
			</button>

			{#if Object.keys(errors).length > 0}
				<ul class="errors">
					{#each Object.entries(errors) as [field, value]}
						{#if value}
							<li>{value}</li>
						{/if}
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

	input,
	select {
		display: block;
		width: 100%;
		height: 30px;
		margin-bottom: 10px;
		margin-top: 4px;
		border-radius: 0;
		border: 1px solid #bebebe;
		height: 30px;
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
