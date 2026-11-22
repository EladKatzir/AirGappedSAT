// =============================================================================
// TestPlayer: Bluebook-style practice test module player.
//
// Server provides: the module's questions (pre-rendered HTML), saved answers,
// and seconds left. This class handles:
//   - one question at a time with Back / Next and a question navigator
//   - multiple choice + student-produced answers
//   - mark for review, answer eliminator, passage highlighter
//   - countdown timer (hideable) with auto-submit at 0:00
//   - autosave of every change (so a refresh / power cut loses nothing)
// =============================================================================

class TestPlayer {
    /**
     * Creates and starts the player.
     * @param {{testId:number, secondsLeft:number, isMath:boolean, items:Array}} cfg - Module data from the server.
     */
    static start(cfg) {
        const p = new TestPlayer(cfg); // Single player instance for the page
        p.init();
        window.testPlayer = p;         // Handy for debugging / automated tests
    }

    /** @param {Object} cfg - See start(). */
    constructor(cfg) {
        this.testId = cfg.testId;                // Test id (for save / submit URLs)
        this.items = cfg.items;                  // Questions in this module
        this.isMath = cfg.isMath;                // Math modules show calculator, single column
        this.secondsLeft = cfg.secondsLeft;      // Countdown value
        this.index = 0;                          // Current question index
        this.shownAt = Date.now();               // When the current question was shown (time tracking)
        this.eliminating = false;                // Eliminator mode on/off
        this.submitted = false;                  // Guard against double submit
        this.navModal = null;                    // Bootstrap modal for the navigator
    }

    /** Wires all controls and shows the first unanswered question. */
    init() {
        this.$ = id => document.getElementById(id); // Short DOM lookup helper
        this.navModal = new bootstrap.Modal(this.$("navModal"));
        this.$("nextBtn").addEventListener("click", () => this.next());
        this.$("backBtn").addEventListener("click", () => this.go(this.index - 1));
        this.$("navBtn").addEventListener("click", () => this.openNavigator());
        this.$("markBtn").addEventListener("click", () => this.toggleMark());
        this.$("elimToggle").addEventListener("click", () => this.toggleEliminator());
        this.$("timerToggle").addEventListener("click", () => this.toggleTimer());
        this.$("submitForm").addEventListener("submit", () => { this.submitted = true; this.flushTime(); });
        if (this.$("hlBtn")) this.$("hlBtn").addEventListener("click", () => this.highlightSelection());
        if (this.isMath && typeof GraphingCalculator !== "undefined") {
            this.calculator = new GraphingCalculator("calcPanel");       // Offline Desmos replacement
            this.$("calcBtn").addEventListener("click", () => this.calculator.toggle());
        }
        window.addEventListener("beforeunload", () => this.flushTime()); // Save time on the last question
        document.addEventListener("keydown", e => this.onKey(e));

        const firstOpen = this.items.findIndex(i => !i.answer);           // Resume where the learner left off
        this.render(firstOpen >= 0 ? firstOpen : 0);
        this.startTimer();
    }

    // ------------------------------------------------------------------ rendering

    /**
     * Shows question number i.
     * @param {number} i - Index into this.items.
     */
    render(i) {
        this.index = i;
        const item = this.items[i];                                        // Current question
        this.shownAt = Date.now();

        this.$("qNum").textContent = item.order;
        this.$("navLabel").textContent = item.order;
        this.$("stem").innerHTML = item.stem;

        const passageEl = this.isMath ? this.$("passageInline") : this.$("passage"); // Where the passage goes
        passageEl.innerHTML = item.passage || (this.isMath ? "" : '<p class="text-muted">No passage for this question.</p>');
        if (this.isMath) passageEl.classList.toggle("d-none", !item.passage);

        this.renderAnswers(item);
        this.renderMark(item);
        this.$("backBtn").disabled = i === 0;
        this.$("nextBtn").textContent = i === this.items.length - 1 ? "Review" : "Next";
        SatPrepPage.renderMath(document.getElementById("tpBody"));         // Typeset math in the new content
    }

    /**
     * Builds the answer area (choices or input box).
     * @param {Object} item - Current question.
     */
    renderAnswers(item) {
        const box = this.$("answers");
        box.innerHTML = "";
        if (item.format === "spr") {
            // Student-produced response: free numeric entry, SAT allows up to 5 characters (6 with a minus sign)
            box.innerHTML = `<label class="form-label small text-muted">Enter your answer (number, fraction like 3/2, or decimal)</label>
                <input class="form-control spr-input" id="sprInput" maxlength="6" autocomplete="off" value="${this.escape(item.answer)}" />
                <div class="small text-muted mt-2">Answer preview: <strong id="sprPreview">${this.escape(item.answer) || "–"}</strong></div>`;
            const input = this.$("sprInput");
            input.addEventListener("input", () => {
                input.value = input.value.replace(/[^0-9./-]/g, "");          // Allow only number characters
                item.answer = input.value;
                this.$("sprPreview").textContent = item.answer || "–";
                this.saveSoon(item);
            });
            return;
        }
        const letters = ["A", "B", "C", "D"];                              // Choice labels
        item.choices.forEach((html, k) => {
            const letter = letters[k];
            const wrap = document.createElement("div");
            wrap.className = "choice-wrap";
            const btn = document.createElement("button");
            btn.type = "button";
            btn.className = "choice" + (item.answer === letter ? " selected" : "") + (item.eliminated.includes(letter) ? " eliminated" : "");
            btn.innerHTML = `<span class="letter">${letter}</span><span class="ctext">${html}</span>`;
            btn.addEventListener("click", () => this.choose(item, letter));
            wrap.appendChild(btn);
            if (this.eliminating) {
                const x = document.createElement("button");               // Cross-out button beside the choice
                x.type = "button";
                x.className = "elim-btn";
                x.title = item.eliminated.includes(letter) ? "Undo" : "Cross out";
                x.textContent = item.eliminated.includes(letter) ? "↺" : letter;
                x.style.textDecoration = item.eliminated.includes(letter) ? "none" : "line-through";
                x.addEventListener("click", () => this.toggleEliminated(item, letter));
                wrap.appendChild(x);
            }
            box.appendChild(wrap);
        });
    }

    /** Updates the mark-for-review button. @param {Object} item */
    renderMark(item) {
        const b = this.$("markBtn");
        b.textContent = item.marked ? "★ Marked for review" : "☆ Mark for review";
        b.classList.toggle("on", item.marked);
    }

    // ------------------------------------------------------------------ actions

    /**
     * Selects (or clears) a choice.
     * @param {Object} item - Question.
     * @param {string} letter - Choice letter.
     */
    choose(item, letter) {
        item.answer = item.answer === letter ? "" : letter;               // Clicking the selected choice clears it
        item.eliminated = item.eliminated.filter(l => l !== letter);      // Choosing un-eliminates
        this.renderAnswers(item);
        SatPrepPage.renderMath(this.$("answers"));
        this.save(item);
    }

    /** Toggles mark for review on the current question. */
    toggleMark() {
        const item = this.items[this.index];
        item.marked = !item.marked;
        this.renderMark(item);
        this.save(item);
    }

    /** Turns eliminator mode on / off. */
    toggleEliminator() {
        this.eliminating = !this.eliminating;
        this.$("elimToggle").classList.toggle("text-primary", this.eliminating);
        this.renderAnswers(this.items[this.index]);
        SatPrepPage.renderMath(this.$("answers"));
    }

    /**
     * Crosses out / restores a choice.
     * @param {Object} item - Question.
     * @param {string} letter - Choice letter.
     */
    toggleEliminated(item, letter) {
        item.eliminated = item.eliminated.includes(letter) ? item.eliminated.filter(l => l !== letter) : [...item.eliminated, letter];
        if (item.answer === letter) item.answer = "";                      // Can't keep a crossed-out answer
        this.renderAnswers(item);
        SatPrepPage.renderMath(this.$("answers"));
        this.save(item);
    }

    /** Wraps the selected passage text in a yellow highlight. */
    highlightSelection() {
        const sel = window.getSelection();
        if (!sel || sel.isCollapsed) return;
        const range = sel.getRangeAt(0);
        if (!this.$("passage").contains(range.commonAncestorContainer)) return; // Only inside the passage
        const mark = document.createElement("mark");
        mark.className = "hl";
        try { range.surroundContents(mark); } catch { /* selection crosses elements - ignore */ }
        sel.removeAllRanges();
    }

    /** Next question, or the review screen after the last one. */
    next() {
        if (this.index === this.items.length - 1) this.openNavigator();
        else this.go(this.index + 1);
    }

    /**
     * Navigates to question i (records time on the current one first).
     * @param {number} i - Target index.
     */
    go(i) {
        if (i < 0 || i >= this.items.length) return;
        this.flushTime();
        this.render(i);
        window.scrollTo(0, 0);
    }

    /** Opens the navigator / review grid. */
    openNavigator() {
        const grid = this.$("navGrid");
        grid.innerHTML = "";
        this.items.forEach((item, i) => {
            const b = document.createElement("button");
            b.type = "button";
            b.textContent = item.order;
            b.className = (item.answer ? "answered " : "") + (item.marked ? "marked " : "") + (i === this.index ? "current" : "");
            b.addEventListener("click", () => { this.navModal.hide(); this.go(i); });
            grid.appendChild(b);
        });
        const answered = this.items.filter(i => i.answer).length;
        this.$("answeredCount").textContent = `${answered} of ${this.items.length} answered`;
        this.navModal.show();
    }

    /** Keyboard shortcuts: ← / → navigate, A-D choose (when not typing). */
    onKey(e) {
        if (e.target.tagName === "INPUT" || document.querySelector(".modal.show")) return;
        if (e.key === "ArrowRight") this.next();
        else if (e.key === "ArrowLeft") this.go(this.index - 1);
        else {
            const k = e.key.toUpperCase();
            const item = this.items[this.index];
            if (item.format === "mc" && ["A", "B", "C", "D"].includes(k)) this.choose(item, k);
        }
    }

    // ------------------------------------------------------------------ timer

    /** Starts the 1-second countdown; auto-submits at 0. */
    startTimer() {
        this.drawTimer();
        this.timerId = setInterval(() => {
            this.secondsLeft = Math.max(0, this.secondsLeft - 1);
            this.drawTimer();
            if (this.secondsLeft === 0) this.timeUp();
        }, 1000);
    }

    /** Renders mm:ss and turns red in the last 5 minutes. */
    drawTimer() {
        const s = this.secondsLeft;
        const el = this.$("timer");
        el.textContent = `${Math.floor(s / 60)}:${String(s % 60).padStart(2, "0")}`;
        el.classList.toggle("low", s <= 300);
        if (s === 300 && el.style.visibility === "hidden") el.style.visibility = "visible"; // Force show at 5 minutes
    }

    /** Hides / shows the clock (as in Bluebook). */
    toggleTimer() {
        const el = this.$("timer");
        const hidden = el.style.visibility === "hidden";
        el.style.visibility = hidden ? "visible" : "hidden";
        this.$("timerToggle").textContent = hidden ? "Hide" : "Show";
    }

    /** Time is up: save everything and submit the module. */
    async timeUp() {
        if (this.submitted) return;
        clearInterval(this.timerId);
        this.submitted = true;
        await this.flushTime();
        this.$("submitForm").submit();
    }

    // ------------------------------------------------------------------ saving

    /** Adds time spent on the current question and saves it. */
    flushTime() {
        const item = this.items[this.index];
        const secs = Math.round((Date.now() - this.shownAt) / 1000);       // Seconds on this question since shown
        this.shownAt = Date.now();
        if (secs > 0) return this.save(item, secs);
        return Promise.resolve();
    }

    /**
     * Debounced save while typing.
     * @param {Object} item - Question.
     */
    saveSoon(item) {
        clearTimeout(this.saveTimer);
        this.saveTimer = setTimeout(() => this.save(item), 600);
    }

    /**
     * Persists one question's state to the server.
     * @param {Object} item - Question.
     * @param {number} [seconds=0] - Seconds to add to its time.
     */
    async save(item, seconds = 0) {
        try {
            const res = await SatPrepPage.post(`/Tests/Take/${this.testId}?handler=Save`, {
                itemId: item.id,
                answer: item.answer || "",
                marked: item.marked,
                eliminated: JSON.stringify(item.eliminated),
                seconds
            });
            if (res && typeof res.secondsLeft === "number") this.secondsLeft = res.secondsLeft; // Re-sync with server clock
        } catch { /* offline for a moment - the next save will carry the state */ }
    }

    /** HTML-escapes a value for use inside an attribute. @param {string} v */
    escape(v) {
        return (v || "").replace(/[&<>"]/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]));
    }
}
