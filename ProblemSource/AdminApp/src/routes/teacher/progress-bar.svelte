<script lang="ts">
	import { onMount, tick } from 'svelte';

	export let value = 0;
	export let max = 100;
	export let prefix = '';
	export let suffix = '%';
	export let decimals = 1;
	export let color = '#ff0000';
	export let initialAnimation = true;
	export let showValueAs: 'All' | 'Percentage' | 'OnlyValue' | 'None' = 'All';

	onMount(async () => {
		if (initialAnimation) {
			const realValue = value;
			value = 0;
			setTimeout(() => {
				value = realValue;
			}, 50);
		}
	});

	$: valueString = getString(value, max, prefix, suffix, decimals, showValueAs);
	function getString(value: number, max: number, prefix: string, suffix: string, decimals: number, showValueAs: string) {
		switch (showValueAs) {
			case 'All':
				return prefix + value.toFixed(decimals) + '/' + max.toFixed(decimals) + suffix;
			case 'Percentage':
				return prefix + getPercentage(value, max).toFixed(decimals) + suffix;
			case 'OnlyValue':
				return prefix + value.toFixed(decimals) + suffix;
			case 'None':
				return '';
		}
	}

	$: percentage = getPercentage(value, max);
	function getPercentage(value: number, max: number) {
		return (value / max) * 100 || 0;
	}
</script>

<div class="progress-bar">
	<div class="background" />
	<div class="progress" style:--progress-color={color} style:width={percentage + '%'} />
	<div class="value">{valueString}</div>
</div>

<style>
	.progress-bar {
		height: 100%;
		border: 2px solid #ffffff;
		border-radius: 10px;
		overflow: hidden;
		position: relative;
	}
	.background {
		background-color: #eeeeee;
		position: absolute;
		top: 0;
		bottom: 0;
		height: 100%;
		width: 100%;
	}
	.progress {
		background: var(--progress-color);
		position: absolute;
		top: 0;
		bottom: 0;
		height: 100%;
		transition: width 0.3s;
	}
	.value {
		display: flex;
		align-items: center;
		position: absolute;
		top: 0;
		bottom: 0;
		left: 8px;
		font-size: 12px;
		color: rgba(0, 0, 0, 0.51);
	}
</style>
