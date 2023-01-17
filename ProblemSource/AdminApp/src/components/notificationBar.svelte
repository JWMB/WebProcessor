<script lang="ts">
	import type { NotificationItem } from "src/types.js";
	import { onDestroy } from "svelte";
    import { notificationsStore } from '../globalStore.js';

    let _notifications: { notification: NotificationItem, opacity: number }[] = [];

    const fadeMs = 3000;
    const iv = setInterval(() => {
		if ($notificationsStore) {
            const withOpacity = $notificationsStore
				.map(o => ({ notification: o, opacity: Math.max(0, fadeMs - (Date.now().valueOf() - o.createdAt.valueOf())) / fadeMs}));
            
            _notifications = withOpacity.filter(o => o.opacity > 0);
		}
	}, 10);

    onDestroy(() => {
        clearInterval(iv);
    });
</script>

<div>
{#each _notifications as n}
    <div style="opacity:{n.opacity || 1}">{n.notification.text}</div>
{/each}
</div>
