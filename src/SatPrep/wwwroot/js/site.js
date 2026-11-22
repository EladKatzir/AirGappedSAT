// =============================================================================
// SatPrep client bootstrap.
// Renders every $...$ and $$...$$ math expression on the page with the
// locally bundled KaTeX (wwwroot/lib/katex) - no internet required.
// =============================================================================

/** Wraps page-level startup logic. */
class SatPrepPage {
    /** Runs all start-up steps once the DOM is ready. */
    static init() {
        SatPrepPage.renderMath(document.body); // Render math in the whole page
    }

    /**
     * Renders LaTeX math inside an element.
     * @param {HTMLElement} root - Element whose text is scanned for math delimiters.
     */
    static renderMath(root) {
        if (typeof renderMathInElement !== "function") return; // KaTeX not loaded on this page
        // Converts $..$ (inline) and $$..$$ (display) blocks into rendered math
        renderMathInElement(root, {
            delimiters: [
                { left: "$$", right: "$$", display: true },
                { left: "$", right: "$", display: false }
            ],
            throwOnError: false // Show bad LaTeX as plain text instead of breaking the page
        });
    }
}

// Runs SatPrepPage.init once the HTML has been parsed
document.addEventListener("DOMContentLoaded", SatPrepPage.init);
