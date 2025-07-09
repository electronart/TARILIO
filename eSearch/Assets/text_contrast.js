function adjustTextColorForContrast() {
    // Function to calculate the luminance of a color
    function luminance(r, g, b) {
        const a = [r, g, b].map(v => {
            v /= 255;
            return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4);
        });
        return a[0] * 0.2126 + a[1] * 0.7152 + a[2] * 0.0722;
    }

    // Function to calculate contrast ratio between two colors
    function contrastRatio(rgb1, rgb2) {
        const lum1 = luminance(rgb1[0], rgb1[1], rgb1[2]) + 0.05;
        const lum2 = luminance(rgb2[0], rgb2[1], rgb2[2]) + 0.05;
        return lum1 > lum2 ? lum1 / lum2 : lum2 / lum1;
    }

    // Function to parse RGB color string to array
    function parseRGB(rgbString) {
        const match = rgbString.match(/\d+/g);
        return match ? [parseInt(match[0]), parseInt(match[1]), parseInt(match[2])] : [0, 0, 0];
    }

    function getElementTrueBackgroundColorRecursive(element) {
        if (element == null) {
            return "rgb(255, 255, 255)";
        }
        const computedStyle = window.getComputedStyle(element);
        const elementBGColor = computedStyle.backgroundColor;
        if (elementBGColor != 'rgba(0, 0, 0, 0)') return elementBGColor;
        else {
            return getElementTrueBackgroundColorRecursive(element.parentElement);
        }

    }



    // Loop through all elements in the document
    document.querySelectorAll("*").forEach(element => {

        const computedStyle = window.getComputedStyle(element);

        // Get background and text color

        const bgColor = getElementTrueBackgroundColorRecursive(element) || "rgb(255, 255, 255)";
        const textColor = computedStyle.color || "rgb(0, 0, 0)";

        if (element.tagName == 'PRE') {
            console.log('Pre computerBackgroundColor', computedStyle.backgroundColor)
        }

        const bgRGB = parseRGB(bgColor);
        const textRGB = parseRGB(textColor);

        // debugging...
        

        // Calculate the contrast ratio
        const contrast = contrastRatio(bgRGB, textRGB);

        // If contrast is poor (less than 4.5:1 for normal text), adjust text color
        if (contrast < 4.5) {
            // Adjust text color to white or black based on background brightness
            const bgLuminance = luminance(bgRGB[0], bgRGB[1], bgRGB[2]);

            if (element.tagName == 'BODY') {
                // debugging..
                console.log('Computed Styles for body', computedStyle);

                console.log('bgRGB', bgRGB);
                console.log('txtRGB', textRGB);
            }

            if (element.tagName != 'HTML') {
                element.style.color = bgLuminance > 0.5 ? "black" : "white";
            }
        }
    });

    console.log('text_contrast has run.');
}

adjustTextColorForContrast();

// Run the function when the document is ready
document.addEventListener("DOMContentLoaded", adjustTextColorForContrast);
