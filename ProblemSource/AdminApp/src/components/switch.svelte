<script>
	export let checked = false;
	export let color = '#2196F3';
	export let inactiveColor = '#ccc';
</script>

<label class="switch" style:--active-color={color} style:--inactive-color={inactiveColor}>
	<input type="checkbox" bind:checked />
	<span class="slider" />
</label>

<style>
	.switch {
		position: relative;
		display: inline-block;
		min-width: 38px;
		height: 26px;
	}

	.switch input {
		opacity: 0;
		width: 0;
		height: 0;
	}

	.slider {
		position: absolute;
		cursor: pointer;
		top: 0;
		left: 0;
		right: 0;
		bottom: 0;
		background-color: var(--inactive-color);
		-webkit-transition: 0.4s;
		transition: 0.4s;
		border-radius: 34px;
	}

	.slider:before {
		position: absolute;
		content: '';
		height: 20px;
		width: 20px;
		left: 3px;
		bottom: 3px;
		background-color: white;
		-webkit-transition: 0.4s;
		transition: 0.4s;
		border-radius: 50%;
	}

	input:checked + .slider {
		background-color: var(--active-color);
	}

	input:checked + .slider {
		box-shadow: 0 0 1px var(--active-color);
	}

	input:checked + .slider:before {
		-webkit-transform: translateX(12px);
		-ms-transform: translateX(12px);
		transform: translateX(12px);
	}
</style>
