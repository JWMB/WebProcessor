class Loader {
	static load(url, options) {
		//console.log("load", url);
		if (options == null) options = {};

		return new Promise(res => {

			const resolver = () => {
				if (options.delay > 0) {
					setTimeout(() => res(), options.delay);
				} else {
					res();
				}
			};

			if (options.skipIfTruthy != null && !!options.skipIfTruthy) {
				//console.log("skipIfTruthy", options.skipIfTruthy);
				resolver();
			} else {
				const singleUrl = Array.isArray(url) ? url[0] : url;
				if (singleUrl === undefined) {
					resolver();
					return;
				}

				const element = document.createElement(singleUrl.indexOf(".css") > 0 ? 'style' : 'script');

				element.onload = function () {
					if (Array.isArray(url) && url.length > 0) {
						const opt2 = { ...options };
						delete opt2.delay;
						Loader.load(url.slice(1), opt2)
							.then(() => {
								resolver();
							});
					} else {
						resolver();
					}
				};
				element.src = singleUrl;

				document.head.appendChild(element);
			}
		});
	}
}