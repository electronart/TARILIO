
console.log('postload highliter run');
let currentHit = -1;
let eSearchHitID = -1;

function go_to_hit(hit) {
    let numHits = eSearchHitID + 1;
    console.log('Going to hit ' + hit + ' num hits ' + numHits);
    if (numHits == 0) return;
    currentHit = hit;
    if (currentHit < 1) currentHit = 1;
    if (currentHit > numHits) currentHit = numHits;
    update_highlights();
}

function update_highlights() {
    let selectedElements = document.getElementsByClassName("selected-hit");
    for (let i = 0; i < selectedElements.length; i++) {
        selectedElements[i].classList.remove("selected-hit");
    }
    let value = currentHit;


    let highlightElements = document.getElementsByClassName("hit-" + (currentHit - 1));
    for (let i = 0; i < highlightElements.length; i++) {
        highlightElements[i].classList.add("selected-hit");
    }
    highlightElements[0].scrollIntoView({ behavior: "smooth", block: "nearest" });
}

function eSearchHighlightTextInDOM(startIndex, endIndex) {
    try {
        highlightText2(startIndex, endIndex);
    } finally {
        if (currentHit == -1) {
            console.log('Scroll to first hit now');
            go_to_hit(1);
        }
        eSearchUpdateHitCount();
    }
}




function highlightText2(startIndex, endIndex) {
    if (startIndex < 0 || endIndex < startIndex) {
        console.error('Invalid range:', startIndex, endIndex);
        return;
    }

    let treeWalkerHighlighter = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
    let highlighterCurrentPos = 0;

    ++eSearchHitID;

    console.info('highlight startindex', startIndex, 'endIndex', endIndex, 'highlighterCurrentPos', highlighterCurrentPos);

    let found = false;

    // Use TreeWalker to traverse text nodes only
    

    try {
        while (treeWalkerHighlighter.nextNode()) {
            const node = treeWalkerHighlighter.currentNode;
            const text = node.nodeValue;
            const textLength = text.length;

            // Check if this node contains part of the highlight range
            if (highlighterCurrentPos + textLength > startIndex && highlighterCurrentPos < endIndex) {
                found = true;

                const startOffset = Math.max(0, startIndex - highlighterCurrentPos);
                const endOffset = Math.min(textLength, endIndex - highlighterCurrentPos);

                // Split text into before, highlighted, and after parts
                const beforeText = text.slice(0, startOffset);
                const highlightedText = text.slice(startOffset, endOffset);
                const afterText = text.slice(endOffset);

                // Create highlight span
                const highlightSpan = document.createElement('span');
                highlightSpan.classList.add('doc-hit', `hit-${eSearchHitID}`);
                highlightSpan.textContent = highlightedText;

                // Replace node with fragment
                const fragment = document.createDocumentFragment();
                if (beforeText) fragment.appendChild(document.createTextNode(beforeText));
                fragment.appendChild(highlightSpan);
                if (afterText) fragment.appendChild(document.createTextNode(afterText));

                node.parentNode.replaceChild(fragment, node);

                highlighterCurrentPos += textLength;
            } else {
                highlighterCurrentPos += textLength;
            }

            // Early termination
            if (highlighterCurrentPos >= endIndex) {
                break;
            }
        }

        if (!found) {
            console.error('Could not find range to highlight:', startIndex, endIndex);
        }
    } catch (e) {
        console.error('Error during highlighting:', e);
    }
}



function getBodyTextForHighlighting() {
    let txt = "";
    try
    {
        const treeWalker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
        while (treeWalker.nextNode()) {
            let textNode = treeWalker.currentNode;
            txt += textNode.nodeValue;
        }
    } catch (e) {
        console.error('Error whilst traversing DOM to build body text', e);
    } finally {
        return txt;
    }
}



function eSearchUpdateHitCount() {
    var currentHit = 1;
    window.Search.receiveData("numhits," + (eSearchHitID + 1)); // Send data to eSearch UI.
    update_highlights();
}

// sleep time expects milliseconds
function sleep(time) {
    return new Promise((resolve) => setTimeout(resolve, time));
}

function eSearchRequestHighlights() {
    console.log('window', window);
    console.log('Postload highlighter3 run', window.Search);
    window.Search.receiveData("curhit,1"); // Reset the displayed current hit.
    window.Search.receiveData("numhits,0"); // Until loaded.

    window.Search.requestHighlightAreas(getBodyTextForHighlighting());
}

eSearchRequestHighlights();