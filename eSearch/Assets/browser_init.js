if (!window.hasOwnProperty('eSearchBrowserAlreadyInitialized')) {   // Ensures it only runs once.
    window.eSearchBrowserAlreadyInitialized = true;
    if (window.hasOwnProperty('ExtrasProvider')) {

        // SO 2592092 - Setting .innerHTML scripts do not normally execute. This drop in will run the scripts.
        function setInnerHTML(elm, html) {
            elm.innerHTML = html;

            Array.from(elm.querySelectorAll("script"))
                .forEach(oldScriptEl => {
                    const newScriptEl = document.createElement("script");

                    Array.from(oldScriptEl.attributes).forEach(attr => {
                        newScriptEl.setAttribute(attr.name, attr.value)
                    });

                    const scriptText = document.createTextNode(oldScriptEl.innerHTML);
                    newScriptEl.appendChild(scriptText);

                    oldScriptEl.parentNode.replaceChild(newScriptEl, oldScriptEl);
                });
        }

        window.ExtrasProvider.getCustomRenderHtml().then(function (customRenderHtml) {
            if (customRenderHtml !== "") {
                console.log('Rendering custom html');
                setInnerHTML(document.documentElement, customRenderHtml);
            } else {
                console.log('Custom html not found');
            }
        });

        window.ExtrasProvider.getTotalCSSRules().then(function (totalCSSRules) {
            let i = 0;
            while (i < totalCSSRules) {
                window.ExtrasProvider.getCSS(i).then(function (css) {
                    var style = document.createElement('style');
                    style.type = 'text/css';
                    style.appendChild(document.createTextNode(css));
                    document.head.appendChild(style);
                });
                ++i;
            }
        });

        window.ExtrasProvider.getTotalJSRules().then(function (totalJSRules) {
            let i = 0;
            while (i < totalJSRules) {
                window.ExtrasProvider.getJS(i).then(function (js) {
                    var script = document.createElement('script');
                    script.appendChild(document.createTextNode(js));
                    document.head.appendChild(script);
                });
                ++i;
            }
        });

        window.ExtrasProvider.getExtraCSSVariables().then(function (variables) {
            let i = 0;
            while (i < variables.length) {
                let css_var_name    = variables[i];
                let css_var_value = variables[i + 1];
                document.documentElement.style.setProperty(css_var_name, css_var_value);
                i += 2;
            }
        });
    }
}