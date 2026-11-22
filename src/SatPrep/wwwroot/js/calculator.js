// =============================================================================
// GraphingCalculator: offline replacement for the Desmos calculator used on
// the digital SAT (Desmos needs internet). Built on the bundled math.js.
//
// Each expression line can be:
//   y = 2x + 3   or   x^2 - 4      -> plotted as a function of x
//   a = 5                          -> defines a variable usable in other lines
//   sqrt(50) / 2                   -> evaluated, result shown at the right
// Graph: drag to pan, mouse wheel / + - to zoom, hover to trace the nearest curve.
// =============================================================================

class GraphingCalculator {
    /** Colors for successive expressions. */
    static COLORS = ["#c74440", "#2d70b3", "#388c46", "#6042a6", "#fa7e19", "#000000"];

    /** @param {string} panelId - Id of the .calc-panel element. */
    constructor(panelId) {
        this.panel = document.getElementById(panelId);          // Floating panel
        this.canvas = document.getElementById("calcCanvas");     // Plot surface
        this.ctx = this.canvas.getContext("2d");                 // 2D drawing context
        this.list = document.getElementById("calcExprs");        // Expression rows container
        this.view = { cx: 0, cy: 0, scale: 40 };                 // Center (math units) and pixels per unit
        this.exprs = [];                                         // [{text, input, out, fn, color}]
        this.hover = null;                                       // Mouse position for tracing
        this.wire();
        this.addRow("");                                         // Start with one empty line
    }

    /** Opens / closes the panel. */
    toggle() {
        this.panel.classList.toggle("open");
        if (this.panel.classList.contains("open")) {
            this.resize();
            this.exprs[this.exprs.length - 1].input.focus();
        }
    }

    /** Hooks up dragging, zooming, resizing and buttons. */
    wire() {
        // Drag the panel by its header
        const head = document.getElementById("calcHead");
        head.addEventListener("pointerdown", e => {
            if (e.target.closest("button")) return;
            const start = { x: e.clientX, y: e.clientY, left: this.panel.offsetLeft, top: this.panel.offsetTop };
            const move = ev => {
                this.panel.style.left = `${start.left + ev.clientX - start.x}px`;
                this.panel.style.top = `${Math.max(0, start.top + ev.clientY - start.y)}px`;
            };
            const up = () => { window.removeEventListener("pointermove", move); window.removeEventListener("pointerup", up); };
            window.addEventListener("pointermove", move);
            window.addEventListener("pointerup", up);
        });

        // Pan the graph
        this.canvas.addEventListener("pointerdown", e => {
            const start = { x: e.clientX, y: e.clientY, cx: this.view.cx, cy: this.view.cy };
            this.canvas.setPointerCapture(e.pointerId);
            const move = ev => {
                this.view.cx = start.cx - (ev.clientX - start.x) / this.view.scale;
                this.view.cy = start.cy + (ev.clientY - start.y) / this.view.scale;
                this.draw();
            };
            const up = () => { this.canvas.removeEventListener("pointermove", move); this.canvas.removeEventListener("pointerup", up); };
            this.canvas.addEventListener("pointermove", move);
            this.canvas.addEventListener("pointerup", up);
        });

        // Zoom with the wheel (around the mouse) and buttons
        this.canvas.addEventListener("wheel", e => { e.preventDefault(); this.zoom(e.deltaY < 0 ? 1.2 : 1 / 1.2); }, { passive: false });
        document.getElementById("calcZoomIn").addEventListener("click", () => this.zoom(1.5));
        document.getElementById("calcZoomOut").addEventListener("click", () => this.zoom(1 / 1.5));
        document.getElementById("calcReset").addEventListener("click", () => { this.view = { cx: 0, cy: 0, scale: 40 }; this.draw(); });
        document.getElementById("calcClose").addEventListener("click", () => this.toggle());

        // Trace
        this.canvas.addEventListener("mousemove", e => { const r = this.canvas.getBoundingClientRect(); this.hover = { x: e.clientX - r.left, y: e.clientY - r.top }; this.draw(); });
        this.canvas.addEventListener("mouseleave", () => { this.hover = null; this.draw(); });

        new ResizeObserver(() => this.resize()).observe(this.panel);   // Panel is user-resizable
    }

    /** Multiplies the zoom level. @param {number} factor */
    zoom(factor) {
        this.view.scale = Math.min(2000, Math.max(2, this.view.scale * factor));
        this.draw();
    }

    /** Adds an expression row. @param {string} text - Initial text. */
    addRow(text) {
        const color = GraphingCalculator.COLORS[this.exprs.length % GraphingCalculator.COLORS.length];
        const row = document.createElement("div");
        row.className = "calc-expr";
        row.innerHTML = `<span class="swatch" style="background:${color}"></span>
            <input class="form-control form-control-sm" placeholder="e.g. y = x^2 - 3x, or 3*sqrt(12)" spellcheck="false" />
            <span class="out"></span>`;
        this.list.appendChild(row);
        const e = { text, input: row.querySelector("input"), out: row.querySelector(".out"), fn: null, color };
        e.input.value = text;
        e.input.addEventListener("input", () => { e.text = e.input.value; this.recompute(); });
        e.input.addEventListener("keydown", ev => {
            if (ev.key === "Enter") {                       // Enter opens a new line
                ev.preventDefault();
                if (this.exprs[this.exprs.length - 1] === e) this.addRow("");
                this.exprs[this.exprs.indexOf(e) + 1].input.focus();
            }
            ev.stopPropagation();                           // Don't trigger test shortcuts while typing
        });
        this.exprs.push(e);
    }

    /** Re-parses every line: variables first, then functions / values. */
    recompute() {
        const scope = {};                                   // Variables defined by "a = 5" lines
        for (const e of this.exprs) {
            e.fn = null;
            e.out.textContent = "";
            const t = e.text.trim();
            if (!t) continue;
            try {
                const m = t.match(/^([a-wzA-WZ]\w*)\s*=\s*(.+)$/);           // Variable assignment (not x / y)
                const yEq = t.match(/^y\s*=\s*(.+)$/);                        // y = f(x)
                const body = yEq ? yEq[1] : (m ? null : t);
                if (m) {
                    scope[m[1]] = math.evaluate(m[2], { ...scope });
                    e.out.textContent = this.fmt(scope[m[1]]);
                    continue;
                }
                const node = math.parse(body);                                  // math.js understands implicit multiplication ("2x")
                const usesX = node.filter(n => n.isSymbolNode && n.name === "x").length > 0;
                const code = node.compile();
                if (usesX) {
                    const s = { ...scope };
                    e.fn = x => { s.x = x; return code.evaluate(s); };
                    e.out.textContent = "graph";
                } else {
                    e.out.textContent = "= " + this.fmt(code.evaluate({ ...scope }));
                }
            } catch {
                e.out.textContent = "?";                    // Incomplete / invalid expression
            }
        }
        this.draw();
    }

    /** Formats a number result. @param {*} v */
    fmt(v) {
        if (typeof v === "number") return Number.isInteger(v) ? String(v) : String(+v.toPrecision(10));
        return math.format(v, { precision: 10 });
    }

    /** Matches the canvas resolution to its displayed size. */
    resize() {
        const r = this.canvas.getBoundingClientRect();
        const dpr = window.devicePixelRatio || 1;
        this.canvas.width = Math.max(1, r.width * dpr);
        this.canvas.height = Math.max(1, r.height * dpr);
        this.ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
        this.w = r.width;
        this.h = r.height;
        this.draw();
    }

    /** Math x -> pixel x. */ toPx(x) { return this.w / 2 + (x - this.view.cx) * this.view.scale; }
    /** Math y -> pixel y. */ toPy(y) { return this.h / 2 - (y - this.view.cy) * this.view.scale; }
    /** Pixel x -> math x. */ fromPx(px) { return this.view.cx + (px - this.w / 2) / this.view.scale; }

    /** Picks a "nice" grid step (1, 2, 5 × 10^n) for the current zoom. */
    gridStep() {
        const raw = 80 / this.view.scale;                   // About 80 px between labels
        const p = Math.pow(10, Math.floor(Math.log10(raw)));
        return [1, 2, 5, 10].map(m => m * p).find(s => s >= raw);
    }

    /** Draws grid, axes, curves and the trace point. */
    draw() {
        if (!this.w) return;
        const c = this.ctx;
        c.clearRect(0, 0, this.w, this.h);
        c.fillStyle = "#fff";
        c.fillRect(0, 0, this.w, this.h);

        // Grid + labels
        const step = this.gridStep();
        const x0 = Math.floor(this.fromPx(0) / step) * step, x1 = this.fromPx(this.w);
        const yTop = this.view.cy + this.h / 2 / this.view.scale, yBot = this.view.cy - this.h / 2 / this.view.scale;
        c.lineWidth = 1;
        c.font = "11px system-ui, sans-serif";
        for (let x = x0; x <= x1; x += step) {
            const px = this.toPx(x);
            c.strokeStyle = Math.abs(x) < step / 2 ? "#333" : "#e5e7eb";
            c.beginPath(); c.moveTo(px, 0); c.lineTo(px, this.h); c.stroke();
            if (Math.abs(x) > step / 2) { c.fillStyle = "#666"; c.fillText(+x.toPrecision(6), px + 2, Math.min(this.h - 4, Math.max(12, this.toPy(0) + 12))); }
        }
        for (let y = Math.floor(yBot / step) * step; y <= yTop; y += step) {
            const py = this.toPy(y);
            c.strokeStyle = Math.abs(y) < step / 2 ? "#333" : "#e5e7eb";
            c.beginPath(); c.moveTo(0, py); c.lineTo(this.w, py); c.stroke();
            if (Math.abs(y) > step / 2) { c.fillStyle = "#666"; c.fillText(+y.toPrecision(6), Math.min(this.w - 30, Math.max(2, this.toPx(0) + 3)), py - 2); }
        }

        // Curves: sample every pixel; break the line at gaps / jumps (asymptotes)
        let best = null;                                    // Nearest curve point to the mouse (trace)
        for (const e of this.exprs) {
            if (!e.fn) continue;
            c.strokeStyle = e.color;
            c.lineWidth = 2.2;
            c.beginPath();
            let drawing = false, prevY = null;
            for (let px = 0; px <= this.w; px += 1) {
                let y;
                try { y = e.fn(this.fromPx(px)); } catch { y = NaN; }
                if (typeof y !== "number" || !isFinite(y)) { drawing = false; prevY = null; continue; }
                const py = this.toPy(y);
                if (drawing && prevY !== null && Math.abs(py - prevY) > this.h) drawing = false; // Vertical asymptote
                if (drawing) c.lineTo(px, py); else c.moveTo(px, py);
                drawing = true;
                prevY = py;
                if (this.hover && Math.abs(px - this.hover.x) < 0.5) {
                    const d = Math.abs(py - this.hover.y);
                    if (!best || d < best.d) best = { d, px, py, x: this.fromPx(px), y, color: e.color };
                }
            }
            c.stroke();
        }

        // Trace label
        if (best && best.d < 40) {
            c.fillStyle = best.color;
            c.beginPath(); c.arc(best.px, best.py, 4, 0, Math.PI * 2); c.fill();
            const label = `(${+best.x.toPrecision(4)}, ${+best.y.toPrecision(4)})`;
            c.font = "12px system-ui, sans-serif";
            const tw = c.measureText(label).width;
            c.fillStyle = "rgba(255,255,255,.9)";
            c.fillRect(best.px + 8, best.py - 22, tw + 8, 18);
            c.fillStyle = "#111";
            c.fillText(label, best.px + 12, best.py - 9);
        }
    }
}
