function base64ToArrayBuffer(base64) {
	let binaryString = window.atob(base64);
	let len = binaryString.length;
	let bytes = new Uint8Array(len);
	for (let i = 0; i < len; i++) {
		bytes[i] = binaryString.charCodeAt(i);
	}
	return bytes.buffer; // This is your ArrayBuffer
}

console.log('This ran');

window.Search.getTiffAsBase64().then(function (result) {
	console.log('that ran');
	let buff = base64ToArrayBuffer(result);
	let tiff = new Tiff({ buffer: buff });
	let canvas = tiff.toCanvas();
	document.body.append(canvas);
});