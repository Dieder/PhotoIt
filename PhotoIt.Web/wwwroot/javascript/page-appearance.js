(() => {
    const supportedFonts = new Set([
        "Roboto", "Open Sans", "Lato", "Montserrat", "Oswald", "Source Sans 3", "Raleway", "Nunito",
        "Merriweather", "Playfair Display", "Poppins", "Ubuntu", "PT Sans", "Noto Sans", "Roboto Slab"
    ]);

    const isHexColor = value => typeof value === "string" && /^#[0-9a-f]{6}$/i.test(value);

    const loadFont = fontFamily => {
        if (!supportedFonts.has(fontFamily))
            return;

        const linkId = `page-font-${fontFamily.toLowerCase().replace(/[^a-z0-9]+/g, "-")}`;
        if (document.getElementById(linkId))
            return;

        const link = document.createElement("link");
        link.id = linkId;
        link.rel = "stylesheet";
        link.href = `https://fonts.googleapis.com/css2?family=${encodeURIComponent(fontFamily).replace(/%20/g, "+")}:wght@400;700&display=swap`;
        document.head.appendChild(link);
    };

    window.pageAppearance = {
        apply: (root, useGradient, startColor, endColor, fontFamily, hasTitle) => {
            if (!root || !isHexColor(startColor) || !isHexColor(endColor))
                return false;

            const fallback = useGradient
                ? `linear-gradient(to right, ${startColor}, ${endColor})`
                : startColor;
            root.style.setProperty("--page-background", fallback);

            if (hasTitle)
                loadFont(fontFamily);

            if (!useGradient)
                return true;

            try {
                const spectral = window.spectral;
                if (!spectral || typeof spectral.Color !== "function" || typeof spectral.gradient !== "function")
                    return false;

                const first = new spectral.Color(startColor);
                const last = new spectral.Color(endColor);
                const stopCount = 13;
                const stops = Array.from({ length: stopCount }, (_, index) => {
                    const position = index / (stopCount - 1);
                    const color = spectral.gradient(position, [first, 0], [last, 1]).toString();
                    if (!isHexColor(color))
                        throw new Error("Spectral.js returned an invalid gradient color.");
                    return `${color} ${position * 100}%`;
                });

                root.style.setProperty("--page-background", `linear-gradient(to right, ${stops.join(", ")})`);
                return true;
            } catch {
                return false;
            }
        }
    };
})();
