
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
    ++eSearchHitID;

 

    let currentPos = 0;
    let nodeQueue = [];

    let debugNextTextNode = false;

    function enqueueNodes(node) {
        const stack = [node];
        while (stack.length > 0) {
            const current = stack.pop();
            if (current.nodeType === Node.ELEMENT_NODE) {
                stack.push(...current.childNodes);
            } else if (current.nodeType === Node.TEXT_NODE) {
                nodeQueue.push(current);
            }
        }
    }

    function traverseAndHighlight(node) {
        // Only process element and text nodes

        if (node.nodeType === Node.ELEMENT_NODE) {
            node.childNodes.forEach(traverseAndHighlight);
        } else if (node.nodeType === Node.TEXT_NODE) {
            const text = node.nodeValue;
            const textLength = text.length;

            if (debugNextTextNode) {
                console.log('next text node...', node);
                console.log('currentPos + textLength = ', currentPos + textLength);
                console.log('Which should be greater than or equal to ', startIndex);
                console.log('And less than ', endIndex);
                debugNextTextNode = false;
            }

            // Check if the current text node contains the highlight range
            if (currentPos + textLength > startIndex && currentPos <= endIndex) {

                console.log('Found valid range');
                // DEBUG
                if (eSearchHitID == 0) {
                    console.log('First highlight node on node', node, 'remaining nodes ', nodeQueue.length);
                    console.log((currentPos + textLength), '>=', startIndex, ' && ', currentPos, '<=', endIndex);
                    debugNextTextNode = true;
                }




                const startOffset = Math.max(0, startIndex - currentPos);
                const endOffset = Math.min(textLength, endIndex - currentPos);

                // Split the text node into three parts: before, highlighted, and after
                const beforeText = text.slice(0, startOffset);
                const highlightedText = text.slice(startOffset, endOffset);
                const afterText = text.slice(endOffset);

                // Create the highlighted span
                const highlightSpan = document.createElement('span');
                highlightSpan.classList.add('doc-hit');
                highlightSpan.classList.add('hit-' + eSearchHitID);
                highlightSpan.textContent = highlightedText;


                // Replace the current text node with new nodes
                const fragment = document.createDocumentFragment();
                if (beforeText) {
                    fragment.appendChild(document.createTextNode(beforeText));
                }
                fragment.appendChild(highlightSpan);
                if (afterText) {
                    fragment.appendChild(document.createTextNode(afterText));
                }

                node.parentNode.replaceChild(fragment, node);
                if (eSearchHitID == 0) {

                    //update_highlights();
                }
            }

            // Update the current position in the text
            currentPos += textLength;
        }
    }

    try {
        enqueueNodes(document.body);
        console.log('nodes enqueued');
    } catch (e) {
        console.error('Something went wrong enqueuing nodes', e);
    } finally {
        let i = nodeQueue.length;
        console.log('node queue length ', i);
        while (i-- > 0) {
            let textNode = nodeQueue[i];
            traverseAndHighlight(textNode);
        }
        console.log('???');
        //for (let i = 0; i < nodeQueue.length; i++) {
        //    let textNode = nodeQueue[i];
        //    traverseAndHighlight(textNode);
        //}
    }
}



function getBodyTextForHighlighting() {
    let txt = "";

    function traverseTxt(node, txt = "") {
        // Only process element and text nodes
        if (node.nodeType === Node.ELEMENT_NODE) {

            for (let i = 0; i < node.childNodes.length; i++)
            {
                const childNode = node.childNodes[i];
                txt = traverseTxt(childNode, txt);
            }

        } else if (node.nodeType === Node.TEXT_NODE) {
            const nodeTxt = node.nodeValue;
            txt += nodeTxt;
        }
        return txt;
    }

    return traverseTxt(document.body);
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
    console.log('Postload highlighter run', window.Search);
    window.Search.receiveData("curhit,1"); // Reset the displayed current hit.
    window.Search.receiveData("numhits,0"); // Until loaded.

    window.Search.requestHighlightAreas(getBodyTextForHighlighting());
}

sleep(1000 * 10).then(() => {
   eSearchRequestHighlights();
});