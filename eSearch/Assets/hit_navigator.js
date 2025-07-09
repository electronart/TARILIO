
var currentHit = 1;

function go_to_hit(hit) {
    let highlightElements = document.getElementsByClassName("doc-hit");
    let numHits = highlightElements.length;
    console.log('Going to hit ' + hit);
    currentHit = hit;
    if (currentHit < 1) currentHit = 1;
    if (currentHit > numHits) currentHit = numHits;
    update_highlights();
}

function update_highlights() {
    let highlightElements = document.getElementsByClassName("doc-hit");
    
    console.log('Update highlights..');
    let selectedElements = document.getElementsByClassName("selected-hit");
    for (let i = 0; i < selectedElements.length; i++) {
        selectedElements[i].classList.remove("selected-hit");
    }
    let value = currentHit;
    highlightElements[value - 1].scrollIntoView({ behavior: "smooth", block: "nearest" });
    highlightElements[value - 1].classList.add("selected-hit");
}

function init() {
    // Called from C# HtmlDocumentControl
    var currentHit = 1;
    var highlightElements = document.getElementsByClassName("doc-hit");
    var numHits = highlightElements.length;



    window.Search.receiveData("numhits," + numHits); // Send data to eSearch UI.
    window.Search.receiveData("curhit,1"); // Reset the displayed current hit.



    // Add a numbered class for each hit.
    for (let i = 0; i < highlightElements.length; i++) {
        highlightElements[i].classList.add('hit-' + i);
    }

    if (highlightElements.length > 0) {
        // Scroll to the first hit.
        highlightElements[0].scrollIntoView({ behavior: "smooth", block: "nearest" });
        highlightElements[0].classList.add("selected-hit");
    }
}

init(); // Since we're now adding the script after the page has loaded.

window.addEventListener('load', function () {
    init();
});