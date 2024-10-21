class MultiLineEditor {
	ignoreEvent = false;
	editorContainer = null;
	editor = null;

	constructor(containerElementOrId) {
		this.editorContainer = typeof containerElementOrId == "string" ? document.getElementById(containerElementOrId) : containerElementOrId;

		const options = {
			// automaticLayout: true,
			scrollBeyondLastLine: false,
			lineHeight: 40,
			fontSize: 18,
			minimap: {
				enabled: false
			}
		};
		this.editor = monaco.editor.create(this.editorContainer, options);
		this.editor.onDidContentSizeChange(() => this.updateHeight());
	}

	updateHeight() {
		if (this.ignoreEvent) return;
		const width = 400;

		const contentHeight = Math.min(1000, this.editor.getContentHeight());
		this.editorContainer.style.width = `${width}px`;
		this.editorContainer.style.height = `${contentHeight}px`;
		try {
			this.ignoreEvent = true;
			this.editor.layout({ width, height: contentHeight });
		} finally {
			this.ignoreEvent = false;
		}
	}
}
