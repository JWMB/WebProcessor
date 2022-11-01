<script lang="ts">
	export const prerender = true;

    import { apiFacade } from '../globalStore.js';
    import { ApiFacade } from '../apiFacade';

    const apiBaseUrl = "https://localhost:7090";
    const apiFacadeInstance = new ApiFacade(apiBaseUrl);
    apiFacade.set(apiFacadeInstance);
</script>

<nav>
	<a href="./">Home</a>
	<a href="./teacher">Teacher</a>
</nav>
<div class="page-container">
	<slot />
</div>

<style>
	nav {
		position: absolute;
		top: 0;
		left: 0;
		right: 0;
		z-index: 1;
		width: 100%;
		height: 40px;
		display: flex;
		background: white;
		align-items: center;
		gap: 10px;
	}
	.page-container {
		position: absolute;
		top: 40px;
		left: 0;
		right: 0;
		bottom: 0;
	}
</style>