document.addEventListener("click", function (event) {
    let target = event.target.closest("a"); // Find the closest <a> element

    if (target && target.tagName === "A") {
        event.preventDefault(); // Prevent default navigation
        console.log("Link clicked:", target.href); // Handle the URL in your own way
        window.Search.openExternalBrowser(target.href);
        // Example: Custom handling
        customNavigationHandler(target.href);
    }
});

function customNavigationHandler(url) {
    
    // You can do anything here, like fetching content via AJAX, updating the URL, etc.
}