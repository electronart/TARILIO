
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



function eSearch_highlight_text() {
    console.log('Postload highlighter run');
    window.Search.receiveData("curhit,1"); // Reset the displayed current hit.
    window.Search.receiveData("numhits,0"); // Until loaded.

    let promises = [];

    // 1.  Walk the tree for text nodes and highlight as necessary.
    const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT)
    while (walker.nextNode()) {
        const node = walker.currentNode;
        promises.push(
            window.Search.hitHighlight(node.data).then(highlightedText => {
                let replacementNode = document.createElement('span');
                replacementNode.innerHTML = highlightedText;
                node.parentNode.insertBefore(replacementNode, node);
                node.parentNode.removeChild(node);
            })
        );

        //node.data = window.Search.hitHighlight(node.data) + ' ';
    }

    Promise.all(promises).then(function () {


        // 2. Update the hit navigation UI in eSearch.

        var currentHit = 1;
        var highlightElements = document.getElementsByClassName("doc-hit");
        var numHits = highlightElements.length;
        window.Search.receiveData("numhits," + numHits); // Send data to eSearch UI.




        // Add a numbered class for each hit.
        for (let i = 0; i < highlightElements.length; i++) {
            highlightElements[i].classList.add('hit-' + i);
        }

        if (highlightElements.length > 0) {
            // Scroll to the first hit.
            highlightElements[0].scrollIntoView({ behavior: "smooth", block: "nearest" });
            highlightElements[0].classList.add("selected-hit");
        }

    })


    




}


document.addEventListener("DOMContentLoaded", eSearch_highlight_text);