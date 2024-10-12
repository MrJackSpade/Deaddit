document.addEventListener('click', function (event) {
    // Check if the clicked element or any of its parents has the class "md-spoiler-text"
    if (event.target.classList.contains('md-spoiler-text')) {
        // Toggle the background to transparent on click
        event.target.style.backgroundColor = 'transparent';
    }
});

//# sourceURL=site.js