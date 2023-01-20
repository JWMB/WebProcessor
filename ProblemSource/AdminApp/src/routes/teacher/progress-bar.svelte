<script lang="ts">
	export let value = 0;
	export let max = 100;
	export let prefix = '';
	export let suffix = '%';
	export let decimals = 1;
	export let color = '#ff0000';
	export let showValueAs: 'All' | 'Percentage' | 'OnlyValue' | 'None' = 'All';

	function getString() {
		switch (showValueAs) {
			case 'All':
				return prefix + value.toFixed(decimals) + '/' + max.toFixed(decimals) + suffix;
			case 'Percentage':
				return prefix + getPercentage().toFixed(decimals) + suffix;
			case 'OnlyValue':
				return prefix + value.toFixed(decimals) + suffix;
			case 'None':
				return '';
		}
	}

	function getPercentage() {
		return (value / max) * 100 || 0;
	}
</script>

<div class="progress-bar">
	<div class="background" />
	<div class="progress" style:--progress-color={color} style:width={getPercentage() + '%'} />
	<div class="value">{getString()}</div>
</div>

<style>
	.progress-bar {
		height: 100%;
		border: 1px solid #dddddd;
		border-radius: 10px;
		overflow: hidden;
		position: relative;
	}
	.background {
		background-color: white;
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
	}
	.value {
		position: absolute;
		top: 0;
		bottom: 0;
	}
</style>
