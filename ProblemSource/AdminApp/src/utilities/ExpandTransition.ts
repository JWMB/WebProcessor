import { cubicInOut } from 'svelte/easing';

export const ExpandTransition = (el: HTMLElement, data?: any) => {
	el.style.overflow = 'hidden';
	const elementHeight = el.offsetHeight;
	return {
		css: (t: number) => {
			return `
        opacity: ${t};
        max-height: ${elementHeight * t}px;
      `;
		},
		easing: cubicInOut,
		duration: 300
	};
};
