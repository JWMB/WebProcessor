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
		<h4>{question}</h4>
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
	h4 {
		margin-top: 10px;
		margin-bottom: 10px;
	}
	.icon {
		font-size: 20px;
		border-radius: 50%;
		background: #9fb8df;
		display: flex;
		width: 30px;
		height: 30px;
		min-width: 30px;
		min-height: 30px;
		align-items: center;
		justify-content: center;
		color: white;
		margin-top: 4px;
	}

	h3 {
		margin-top: 8px;
	}
</style>
