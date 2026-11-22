// =============================================================================
// SatPrep client bootstrap (every page).
//  - Renders math with the locally bundled KaTeX (no internet).
//    Server-side Markdown turns $..$ into \( .. \) and $$..$$ into \[ .. \].
//  - Light / dark theme toggle (remembered per browser).
//  - Small helpers shared by page scripts (antiforgery token, JSON POST).
// =============================================================================

/** Page-level helpers shared by all pages. */
class SatPrepPage {
    /** Runs all start-up steps once the DOM is ready. */
    static init() {
        SatPrepPage.renderMath(document.body); // Render math in the whole page
        SatPrepPage.wireThemeToggle();         // Hook up the ◐ button
        SatPrepPage.wireMarkButtons();         // ☆ Mark for review buttons
    }

    /** Bookmark buttons (data-mark="questionId") toggle via the Practice page handler. */
    static wireMarkButtons() {
        document.querySelectorAll("[data-mark]").forEach(btn => {
            btn.addEventListener("click", async () => {
                const res = await SatPrepPage.post("/Practice?handler=Mark", { questionId: btn.dataset.mark }); // Toggle server-side
                const on = res && res.marked;                                   // New state
                btn.textContent = on ? "★ Marked" : "☆ Mark for review";
                btn.classList.toggle("text-warning", on);
                btn.classList.toggle("text-muted", !on);
            });
        });
    }

    /**
     * Renders LaTeX math inside an element.
     * @param {HTMLElement} root - Element whose text is scanned for math delimiters.
     */
    static renderMath(root) {
        if (typeof renderMathInElement !== "function" || !root) return; // KaTeX not loaded on this page
        // Converts \( .. \) (inline) and \[ .. \] (display) into rendered math.
        // Plain "$" is deliberately NOT a delimiter so money amounts like $5 stay as text.
        renderMathInElement(root, {
            delimiters: [
                { left: "\\[", right: "\\]", display: true },
                { left: "\\(", right: "\\)", display: false }
            ],
            throwOnError: false // Show bad LaTeX as plain text instead of breaking the page
        });
    }

    /** Toggles light/dark mode and remembers the choice. */
    static wireThemeToggle() {
        const btn = document.getElementById("themeToggle"); // Toggle button in the nav
        if (!btn) return;
        btn.addEventListener("click", () => {
            const html = document.documentElement;
            const next = html.dataset.bsTheme === "dark" ? "light" : "dark"; // Flip theme
            html.dataset.bsTheme = next;
            try { localStorage.setItem("satprep-theme", next); } catch { /* storage blocked - ignore */ }
        });
    }

    /** Returns the antiforgery token rendered in the page (needed for POSTs). */
    static token() {
        const el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : "";
    }

    /**
     * POSTs form data to a Razor Page handler and parses the JSON reply.
     * @param {string} url - Target URL (e.g. "/Tests/Take/5?handler=Save").
     * @param {Object} data - Key/value pairs to send.
     */
    static async post(url, data) {
        const body = new URLSearchParams(data);              // Encode as a normal form post
        const res = await fetch(url, {
            method: "POST",
            headers: { "RequestVerificationToken": SatPrepPage.token() },
            body
        });
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const type = res.headers.get("content-type") || "";
        return type.includes("json") ? res.json() : null;
    }
}

// Runs SatPrepPage.init once the HTML has been parsed
document.addEventListener("DOMContentLoaded", SatPrepPage.init);
