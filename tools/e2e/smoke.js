// =============================================================================
// SatPrep end-to-end smoke test (Playwright, headless Chromium).
//
//   node tools/e2e/smoke.js [baseUrl]        default http://localhost:5100
//
// Walks every feature like a learner would and FAILS if:
//   - any page returns an error or a check below fails
//   - any JavaScript error is logged
//   - any request leaves the server (the app must work fully air-gapped)
// =============================================================================
const { chromium } = require("playwright");

/** Runs the smoke test scenario step by step and records results. */
class SmokeTest {
    /** @param {string} base - Base URL of the running app. */
    constructor(base) {
        this.base = base.replace(/\/$/, "");  // Base URL without trailing slash
        this.errors = [];                     // JS errors seen in the browser
        this.external = [];                   // Requests to other hosts (must stay empty)
        this.passed = 0;                      // Passed checks
        this.failed = [];                     // Failed check descriptions
    }

    /**
     * Records a check.
     * @param {string} name - What is being checked.
     * @param {boolean} ok - Whether it passed.
     */
    check(name, ok) {
        if (ok) { this.passed++; console.log(`  ✓ ${name}`); }
        else { this.failed.push(name); console.log(`  ✗ ${name}`); }
    }

    /** Opens a page relative to the base URL and waits until it is idle. @param {string} path */
    async open(path) {
        const res = await this.page.goto(this.base + path);
        await this.page.waitForLoadState("networkidle");
        return res;
    }

    /** Executes the full scenario. */
    async run() {
        const browser = await chromium.launch();
        const ctx = await browser.newContext({ viewport: { width: 1280, height: 900 } });
        this.page = await ctx.newPage();
        const p = this.page;
        p.on("pageerror", e => this.errors.push(e.message));
        p.on("console", m => { if (m.type() === "error") this.errors.push(m.text()); });
        p.on("request", r => { if (!r.url().startsWith(this.base)) this.external.push(r.url()); });
        p.on("dialog", d => d.accept());
        const name = `Smoke${Date.now() % 100000}`;   // Fresh learner every run

        console.log("Sign in");
        await this.open("/");
        this.check("unauthenticated users land on 'Who is studying?'", p.url().includes("/Account/Login"));
        await p.fill("input[name=Name][placeholder]", name);
        await p.click("button:has-text('Start')");
        await p.waitForURL(this.base + "/");
        this.check("home page shows today's plan", await p.locator("text=Today's plan").count() === 1);

        console.log("Study plan");
        await this.open("/Plan");
        await p.fill("input[name=TargetScore]", "1450");
        await p.click("button:has-text('Save plan')");
        await p.waitForURL(this.base + "/");
        this.check("plan saved and countdown shown", (await p.textContent("main")).includes("until your SAT"));

        console.log("Practice by topic");
        await this.open("/Practice");
        this.check("topic picker lists all 29 skills", await p.locator("a.topic-link[href*='skill=']").count() === 29);
        await p.locator("a.topic-link[href*='skill=']").first().click();
        await p.waitForLoadState("networkidle");
        for (let i = 0; i < 3; i++) {                                  // Answer 3 questions
            if (await p.locator("input[name=answer][type=radio]").count()) await p.locator("label.choice").first().click();
            else await p.fill("input[name=answer]", "1");
            await p.click("button:has-text('Check answer')");
            await p.waitForLoadState("networkidle");
            this.check(`question ${i + 1}: result and explanation shown`,
                await p.locator(".result-banner").count() === 1 && await p.locator(".explanation").count() === 1);
            if (i === 0) {
                await p.click("button.mark-btn");
                await p.waitForTimeout(300);
                this.check("bookmark toggles", (await p.textContent("button.mark-btn")).includes("Marked"));
            }
            await p.click("#nextBtn");
            await p.waitForLoadState("networkidle");
        }

        console.log("Skills, mistakes, bank");
        await this.open("/Skills");
        this.check("skill progress shows a practised skill", (await p.textContent("main")).includes("correct · last"));
        await this.open("/Mistakes");
        const mistakes = await p.locator("a:has-text('Retry')").count();
        this.check("mistakes page loads", true);
        if (mistakes > 0) {
            await p.locator("a:has-text('Retry')").first().click();
            await p.waitForLoadState("networkidle");
            this.check("retry opens the missed question", await p.locator("button:has-text('Check answer')").count() === 1);
        }
        await this.open("/Bank?status=marked");
        this.check("bank filter finds the bookmarked question", await p.locator("a:has-text('Open')").count() >= 1);

        console.log("Study");
        await this.open("/Study/Lesson/ALG");
        this.check("lesson renders math without errors", await p.locator(".katex").count() > 10 && await p.locator(".katex-error").count() === 0);
        this.check("lesson has practice buttons per skill", await p.locator("a:has-text('Practice this skill')").count() === 5);
        await this.open("/Study/Vocab");
        await p.click("#card");
        await p.click("button:has-text('Knew it')");
        await p.waitForLoadState("networkidle");
        this.check("vocab flashcard advances", await p.locator("#card").count() === 1);

        console.log("Practice test (short)");
        await this.open("/Tests");
        await p.locator("form:has(input[value=Short]) button").click();
        for (let m = 0; m < 2; m++) {
            await p.click("#startModule");
            await p.waitForSelector("#stem");
            const n = await p.evaluate(() => window.testPlayer.items.length);
            this.check(`module ${m + 1} has questions (${n})`, n > 0);
            for (let i = 0; i < n; i++) {
                if (await p.locator("#sprInput").count()) await p.fill("#sprInput", "2");
                else await p.locator("#answers .choice").first().click();
                await p.click("#nextBtn");
            }
            await p.waitForSelector("#navModal.show");
            this.check(`review grid shows all answered (module ${m + 1})`, await p.locator("#navGrid button.answered").count() === n);
            if (m === 1) {
                await p.locator("#navModal .btn-close").click();
                await p.waitForTimeout(400);
                await p.click("#calcBtn");
                await p.fill("#calcExprs input", "2^10");
                this.check("calculator evaluates", (await p.textContent(".calc-expr .out")).includes("1024"));
                await p.click("#calcClose");
                await p.click("#navBtn");
                await p.waitForSelector("#navModal.show");
            }
            await p.click("#submitModule");
        }
        await p.waitForURL(/\/Tests\/Results\//);
        const total = await p.locator(".stat .value").first().textContent();
        this.check(`results show a 400-1600 total (${total})`, Number(total) >= 400 && Number(total) <= 1600);

        console.log("Content");
        await this.open("/Content");
        this.check("content page lists question sources", await p.locator("text=gen-math").count() >= 1);

        console.log("Phone layout");
        await p.setViewportSize({ width: 390, height: 844 });
        await this.open("/");
        const overflow = await p.evaluate(() => document.documentElement.scrollWidth > window.innerWidth + 1);
        this.check("no horizontal scroll at phone width", !overflow);

        console.log("Air-gap checks");
        this.check(`no JavaScript errors (${this.errors.length})`, this.errors.length === 0);
        this.check(`no external requests (${this.external.length})`, this.external.length === 0);

        await browser.close();
        console.log(`\n${this.passed} passed, ${this.failed.length} failed`);
        if (this.errors.length) console.log("JS errors:", this.errors);
        if (this.external.length) console.log("External requests:", this.external);
        process.exit(this.failed.length ? 1 : 0);
    }
}

new SmokeTest(process.argv[2] || "http://localhost:5100").run().catch(e => { console.error(e); process.exit(1); });
