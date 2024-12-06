<script lang="ts">
	import { ExpandTransition } from '../../utilities/ExpandTransition';

	export let question: string;

	let showAnswer = false;
</script>

<div
	class="question"
	tabindex="0"
	class:showAnswer
	on:click={() => {
		showAnswer = !showAnswer;
	}}>
	<div class="icon">
		{#if showAnswer}
			!
		{:else}
			?
		{/if}
	</div>
	<div class="content">
		<h3>{question}</h3>
		{#if showAnswer}
			<div transition:ExpandTransition|local>
				<div class="answer"><slot /></div>
			</div>
		{/if}
	</div>
</div>

<style>
	.question {
		margin-bottom: 10px;
		display: flex;
		column-gap: 10px;
		margin-top: 10px;
		margin-bottom: 10px;
		background-color: rgb(217, 231, 252);
		padding: 10px;
		border-radius: 8px;
	}
	.icon {
		font-size: 24px;
		border-radius: 50%;
		background: gray;
		display: flex;
		width: 40px;
		height: 40px;
		min-width: 40px;
		min-height: 40px;
		align-items: center;
		justify-content: center;
		color: white;
	}

	h3 {
		margin-top: 8px;
	}
</style>
