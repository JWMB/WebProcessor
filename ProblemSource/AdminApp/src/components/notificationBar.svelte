<script lang="ts">
	import { SeverityLevel, type NotificationItem } from 'src/types.js';
	import { onDestroy } from 'svelte';
	import { notificationsStore } from '../globalStore.js';

	let _notifications: { notification: NotificationItem; opacity: number }[] = [];

	function updateFromStore() {
		const fadeMs = 10000;
		const calcOpacity = (createdAt: Date) => Math.max(0, fadeMs - (Date.now().valueOf() - createdAt.valueOf())) / fadeMs;

		const withOpacity = $notificationsStore.map((o) => ({
			notification: o,
			opacity: calcOpacity(o.createdAt)
		}));

		_notifications = withOpacity.filter((o) => o.opacity > 0);
	}

	const iv = setInterval(updateFromStore, 10);

	function onClickRemove(index: number) {
		notificationsStore.removeAt(index);
		updateFromStore();
	}

	function getColor(item: NotificationItem) {
		if (!!item.color) return item.color;
		if (!!item.severity) {
			if (item.severity === SeverityLevel.warning) return 'yellow';
			if (item.severity === SeverityLevel.error) return 'orange';
			if (item.severity === SeverityLevel.critical) return 'red';
		}
		return 'lightblue';
	}

	onDestroy(() => {
		clearInterval(iv);
	});
</script>

<div class="notification-bottom">
	{#each _notifications as n, i}
		<div class="notification-item" style="opacity:{n.opacity || 1}; background-color:{getColor(n.notification)}">
			<a href="" data-index={i} class="notification-close" on:click={(e) => onClickRemove(i)}>X</a>
			<span>{n.notification.text}</span>
		</div>
	{/each}
</div>

<style>
	.notification-bottom {
		position: fixed;
		width: 100%;
		bottom: 0;
		left: 0;
	}

	.notification-item {
		width: 100%;
		left: 10px;
		margin: 2px;
		border: thin solid gray;
		background-color: lightblue;
		padding: 5px;
	}

	.notification-close {
		left: 5px;
		top: 5px;
		/* This are approximated values, and the rest of your styling goes here */
	}
</style>
