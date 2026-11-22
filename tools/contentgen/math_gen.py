"""
Math question generators: one or more templates per skill and difficulty.
Each template picks random numbers, COMPUTES the answer, builds distractors
from typical mistakes, and writes a worked explanation from the same numbers.

Function signature for every template: (f: QuestionFactory, r: Random, skill, diff) -> dict | None
"""
import math
from fractions import Fraction as F

from common import m, money, tex_num, plain_num

# =============================================================================
# Small helpers
# =============================================================================

def nz(r, lo, hi):
    """Random non-zero integer in [lo, hi]."""
    while True:
        v = r.randint(lo, hi)
        if v != 0:
            return v


def signed(v, first=False):
    """Formats a coefficient term sign: '+ 3' / '- 3' (or '-3' / '3' when first)."""
    if first:
        return f"{v}"
    return f"+ {v}" if v >= 0 else f"- {abs(v)}"


def lin(a, b, var="x"):
    """LaTeX for a*x + b with clean signs (a != 0)."""
    a_s = "" if a == 1 else "-" if a == -1 else str(a)
    if b == 0:
        return f"{a_s}{var}"
    return f"{a_s}{var} {signed(b)}"


def quad(a, b, c, var="x"):
    """LaTeX for a x^2 + b x + c with clean signs."""
    parts = []
    a_s = "" if a == 1 else "-" if a == -1 else str(a)
    parts.append(f"{a_s}{var}^2")
    if b:
        bs = "" if abs(b) == 1 else str(abs(b))
        parts.append(f"{'+' if b > 0 else '-'} {bs}{var}")
    if c:
        parts.append(f"{'+' if c > 0 else '-'} {abs(c)}")
    return " ".join(parts)


def nums(*vals):
    """Formats numbers as math strings for choices."""
    return [m(tex_num(v)) for v in vals]


# =============================================================================
# ALGEBRA
# =============================================================================

def le1_easy(f, r, skill, diff):
    """ax + b = c."""
    a, x = nz(r, 2, 9), r.randint(-9, 12)
    b = r.randint(-20, 20)
    c = a * x + b
    stem = f"If {m(f'{lin(a, b)} = {c}')}, what is the value of {m('x')}?"
    expl = (f"Subtract {m(str(b))} from both sides: {m(f'{a}x = {c - b}')}. "
            f"Divide both sides by {m(str(a))}: {m(f'x = {x}')}.")
    return f.mc(skill, diff, stem, m(str(x)), nums(x + 1 if b > 0 else x - 1, F(c + b, a), F(c, a) if c % a else -x, x * a), expl) \
        if r.random() < 0.6 else f.spr(skill, diff, stem, x, expl)


def le1_medium(f, r, skill, diff):
    """a(x + b) = cx + d with an integer solution."""
    a, c = r.randint(2, 7), r.randint(1, 9)
    if a == c:
        c += 1
    x, b = r.randint(-8, 10), r.randint(-6, 6)
    d = a * (x + b) - c * x
    stem = f"What value of {m('x')} satisfies the equation {m(f'{a}(x {signed(b)}) = {lin(c, d)}')}?"
    expl = (f"Distribute: {m(f'{a}x {signed(a * b)} = {lin(c, d)}')}. "
            f"Collect {m('x')} terms: {m(f'{a - c}x = {d - a * b}')}. "
            f"So {m(f'x = {tex_num(F(d - a * b, a - c))}')}.")
    wrong1 = F(d - b, a - c)                       # Forgot to distribute
    wrong2 = F(d + a * b, a - c)                   # Sign error moving the constant
    wrong3 = F(d - a * b, a + c)                   # Added instead of subtracting cx
    return f.mc(skill, diff, stem, m(str(x)), nums(wrong1, wrong2, wrong3, x + 2), expl) \
        if r.random() < 0.5 else f.spr(skill, diff, stem, x, expl)


def le1_hard(f, r, skill, diff):
    """Infinitely many / no solutions conditions."""
    c, p = r.randint(2, 6), r.randint(2, 9)
    k = c * 2
    if r.random() < 0.5:
        q = c * p
        stem = (f"In the equation {m(f'{c}(2x + p) = {k}x + {q}')}, {m('p')} is a constant. "
                f"If the equation has infinitely many solutions, what is the value of {m('p')}?")
        expl = (f"Distribute: {m(f'{k}x + {c}p = {k}x + {q}')}. The {m('x')}-terms already match, so the equation is true "
                f"for every {m('x')} exactly when the constants match: {m(f'{c}p = {q}')}, so {m(f'p = {p}')}.")
        return f.spr(skill, diff, stem, p, expl)
    a = r.randint(2, 9)
    b, d = r.randint(-9, 9), r.randint(-9, 9)
    if b == d:
        d += 3
    stem = (f"In the equation {m(f'ax {signed(b)} = {a}x {signed(d)}')}, {m('a')} is a constant. "
            f"For what value of {m('a')} does the equation have no solution?")
    expl = (f"An equation {m('ax + b = cx + d')} has no solution when the {m('x')}-coefficients are equal and the constants differ. "
            f"Here the constants {m(str(b))} and {m(str(d))} differ, so {m(f'a = {a}')} gives {m(f'{b} = {d}')}, which is false for every {m('x')}.")
    return f.mc(skill, diff, stem, m(str(a)), nums(-a, d - b, 0, a + (d - b)), expl)


def lf_easy(f, r, skill, diff):
    """Evaluate f(x) = mx + b, or read slope from context."""
    if r.random() < 0.5:
        mm, b, k = nz(r, -6, 8), r.randint(-10, 15), r.randint(-5, 8)
        val = mm * k + b
        stem = f"The function {m('f')} is defined by {m(f'f(x) = {lin(mm, b)}')}. What is the value of {m(f'f({k})')}?"
        expl = f"Substitute {m(f'x = {k}')}: {m(f'f({k}) = {mm}({k}) {signed(b)} = {mm * k} {signed(b)} = {val}')}."
        return f.mc(skill, diff, stem, m(str(val)), nums(val + 2 * b if b else val + 1, mm + k + b, mm * k - b if b else val - 1, k), expl)
    rate, fee = r.choice([1.5, 2, 2.5, 3, 4, 12, 15, 25]), r.choice([5, 8, 10, 20, 30, 40])
    thing = r.choice([("a bike rental", "hour"), ("a parking garage", "hour"), ("a gym membership", "month"), ("a streaming plan", "month")])
    stem = (f"The total cost {m('C')}, in dollars, of {thing[0]} for {m('t')} {thing[1]}s is given by "
            f"{m(f'C(t) = {rate:g}t + {fee}')}. What is the cost per {thing[1]}, in dollars?")
    expl = (f"In a linear function the coefficient of the variable is the rate of change. Each additional {thing[1]} adds "
            f"{m(f'{rate:g}')} dollars, so the cost per {thing[1]} is {money(rate)}. The {m(str(fee))} is the fixed starting cost.")
    return f.mc(skill, diff, stem, money(rate), [money(fee), money(rate + fee), money(fee / rate if rate else 1)], expl)


def lf_medium(f, r, skill, diff):
    """Linear f with two known values: find f(0) or another value."""
    mm, b = nz(r, -5, 7), r.randint(-12, 12)
    x1, x2 = r.sample(range(-4, 9), 2)
    y1, y2 = mm * x1 + b, mm * x2 + b
    ask = r.randint(-3, 10)
    target = mm * ask + b
    stem = (f"For the linear function {m('f')}, {m(f'f({x1}) = {y1}')} and {m(f'f({x2}) = {y2}')}. "
            f"What is the value of {m(f'f({ask})')}?")
    expl = (f"Slope: {m(f'\\frac{{{y2} - ({y1})}}{{{x2} - ({x1})}} = {mm}')}. Then {m(f'f(x) = {mm}x + b')}; using "
            f"{m(f'f({x1}) = {y1}')} gives {m(f'b = {b}')}. So {m(f'f({ask}) = {mm}({ask}) {signed(b)} = {target}')}.")
    return f.spr(skill, diff, stem, target, expl) if r.random() < 0.5 else \
        f.mc(skill, diff, stem, m(str(target)), nums(target + mm, mm * ask, target - 2 * b if b else target + 3, -target if target else 4), expl)


def lf_hard(f, r, skill, diff):
    """Context: interpret / build a linear model from two data points."""
    start = r.choice([120, 150, 200, 240, 300, 360, 480])
    rate = r.choice([4, 5, 6, 8, 10, 12, 15])
    t1, t2 = r.sample(range(2, 12), 2)
    t1, t2 = min(t1, t2), max(t1, t2)
    v1, v2 = start - rate * t1, start - rate * t2
    if v2 <= 0:
        return None
    stem = (f"A water tank is being drained at a constant rate. After {t1} minutes it holds {v1} liters, and after {t2} minutes "
            f"it holds {v2} liters. Which function gives the volume {m('V')}, in liters, after {m('t')} minutes?")
    correct = m(f"V(t) = {start} - {rate}t")
    expl = (f"Rate: {m(f'\\frac{{{v2} - {v1}}}{{{t2} - {t1}}} = -{rate}')} liters per minute. "
            f"Starting volume: {m(f'{v1} + {rate}({t1}) = {start}')}. So {correct}.")
    return f.mc(skill, diff, stem, correct,
                [m(f"V(t) = {v1} - {rate}t"), m(f"V(t) = {start} + {rate}t"), m(f"V(t) = {start} - {round((v1 - v2) / (t2 - t1) * 2)}t")],
                expl)


def le2_easy(f, r, skill, diff):
    """Intercept / plug-in for ax + by = c."""
    a, b = r.randint(2, 9), r.randint(2, 9)
    y0 = r.randint(1, 9)
    c = b * y0
    stem = f"The graph of {m(f'{a}x + {b}y = {c}')} in the {m('xy')}-plane crosses the {m('y')}-axis at {m('(0, k)')}. What is {m('k')}?"
    expl = f"On the {m('y')}-axis, {m('x = 0')}, so {m(f'{b}y = {c}')} and {m(f'y = {y0}')}."
    return f.mc(skill, diff, stem, m(str(y0)), nums(F(c, a), c, a, y0 + b), expl)


def le2_medium(f, r, skill, diff):
    """Two-item purchase context."""
    p1, p2 = r.choice([2, 3, 4, 5]), r.choice([6, 7, 8, 9, 12])
    n1, n2 = r.randint(3, 12), r.randint(2, 9)
    total = p1 * n1 + p2 * n2
    item = r.choice([("pens", "notebooks"), ("bagels", "muffins"), ("stickers", "posters"), ("apples", "melons")])
    stem = (f"A store sells {item[0]} for {money(p1)} each and {item[1]} for {money(p2)} each. Sam spent exactly {money(total)} "
            f"and bought {n1} {item[0]}. How many {item[1]} did Sam buy?")
    expl = (f"Let {m('n')} be the number of {item[1]}: {m(f'{p1}({n1}) + {p2}n = {total}')}, so "
            f"{m(f'{p2}n = {total - p1 * n1}')} and {m(f'n = {n2}')}.")
    return f.spr(skill, diff, stem, n2, expl) if r.random() < 0.5 else \
        f.mc(skill, diff, stem, m(str(n2)), nums(n2 + 1, n1, F(total, p2) if total % p2 else n2 + 2, n2 - 1 if n2 > 1 else n2 + 3), expl)


def le2_hard(f, r, skill, diff):
    """Slope of parallel / perpendicular line to ax + by = c."""
    a, b = nz(r, -7, 7), r.randint(2, 9)
    if math.gcd(abs(a), b) != 1:
        return None
    slope = F(-a, b)
    perp = r.random() < 0.5
    ans = -1 / slope if perp else slope
    stem = (f"Line {m('\\ell')} is {'perpendicular' if perp else 'parallel'} to the line {m(f'{a}x + {b}y = 7')} in the "
            f"{m('xy')}-plane. What is the slope of line {m('\\ell')}?")
    expl = (f"Rewrite as {m(f'y = {tex_num(slope)}x + \\frac{{7}}{{{b}}}')}: the slope is {m(tex_num(slope))}. "
            + (f"Perpendicular slopes are negative reciprocals, so the slope of {m('\\ell')} is {m(tex_num(ans))}."
               if perp else f"Parallel lines have equal slopes, so the slope of {m('\\ell')} is {m(tex_num(ans))}."))
    other = slope if perp else -1 / slope                       # The slope for the other relationship (common mix-up)
    return f.mc(skill, diff, stem, m(tex_num(ans)), nums(-ans, 1 / ans, other if other not in (-ans, 1 / ans) else ans + 1), expl)


def sys_easy(f, r, skill, diff):
    """x + y = s, x - y = d."""
    x, y = r.randint(-5, 15), r.randint(-5, 12)
    stem = f"If {m(f'x + y = {x + y}')} and {m(f'x - y = {x - y}')}, what is the value of {m('x')}?"
    expl = f"Add the equations: {m(f'2x = {2 * x}')}, so {m(f'x = {x}')}. (Then {m(f'y = {y}')}.)"
    return f.mc(skill, diff, stem, m(str(x)), nums(y, 2 * x, x + y, x - y if x - y != x else x + 1), expl)


def sys_medium(f, r, skill, diff):
    """General 2x2 with integer solution; ask x + y or a single variable."""
    x, y = r.randint(-6, 9), r.randint(-6, 9)
    a, b, c, d = nz(r, -5, 6), nz(r, -5, 6), nz(r, -5, 6), nz(r, -5, 6)
    if a * d - b * c == 0:
        return None
    e1, e2 = a * x + b * y, c * x + d * y
    stem = (f"The solution to the system of equations below is {m('(x, y)')}. What is the value of {m('x + y')}?\n\n"
            f"$${lin(a, 0)} {signed(b)}y = {e1}$$\n\n$${lin(c, 0)} {signed(d)}y = {e2}$$")
    expl = (f"Solve by elimination or substitution: {m(f'x = {x}')} and {m(f'y = {y}')}. "
            f"Check: {m(f'{a}({x}) + ({b})({y}) = {e1}')} and {m(f'{c}({x}) + ({d})({y}) = {e2}')}. So {m(f'x + y = {x + y}')}.")
    return f.spr(skill, diff, stem.replace("$$" + lin(a, 0), "$$" + lin(a, 0)), x + y, expl)


def sys_hard(f, r, skill, diff):
    """No-solution / infinitely-many system with a constant."""
    a, b = r.randint(1, 6), r.randint(2, 7)
    k = r.randint(2, 4)
    c1 = r.randint(1, 12)
    stem = (f"In the system below, {m('p')} is a constant. For what value of {m('p')} does the system have no solution?\n\n"
            f"$${a}x + {b}y = {c1}$$\n\n$$px + {b * k}y = {c1 * k + r.choice([1, 2, 3, 5])}$$")
    ans = a * k
    expl = (f"Two linear equations have no solution when their lines are parallel but different: the coefficients must be proportional "
            f"while the constants are not. Multiplying the first equation by {m(str(k))} gives {m(f'{a * k}x + {b * k}y = {c1 * k}')}, "
            f"so {m(f'p = {ans}')} makes the left sides match while the right sides differ.")
    return f.mc(skill, diff, stem, m(str(ans)), nums(a, b * k, -ans, ans + k), expl)


def ine_easy(f, r, skill, diff):
    """Solve ax + b < c, greatest integer."""
    a, b = r.randint(2, 8), r.randint(-15, 15)
    c = r.randint(-10, 40)
    bound = F(c - b, a)
    ans = math.ceil(bound) - 1 if bound.denominator == 1 else math.floor(bound)
    stem = f"What is the greatest integer {m('x')} that satisfies {m(f'{lin(a, b)} < {c}')}?"
    expl = (f"Subtract {m(str(b))}: {m(f'{a}x < {c - b}')}. Divide by {m(str(a))}: {m(f'x < {tex_num(bound)}')}. "
            f"The greatest integer less than {m(tex_num(bound))} is {m(str(ans))}.")
    return f.spr(skill, diff, stem, ans, expl)


def ine_medium(f, r, skill, diff):
    """Budget inequality."""
    budget = r.choice([50, 60, 75, 80, 100, 120, 150])
    fee = r.choice([5, 8, 10, 12, 15])
    each = r.choice([3, 4, 6, 7, 9])
    n = (budget - fee) // each
    item = r.choice(["tickets", "books", "plants", "games"])
    stem = (f"A club has at most {money(budget)} to spend. It pays a one-time fee of {money(fee)} and {money(each)} for each of the "
            f"{item} it buys. What is the greatest number of {item} the club can buy?")
    expl = (f"{m(f'{fee} + {each}n \\le {budget}')}, so {m(f'{each}n \\le {budget - fee}')} and "
            f"{m(f'n \\le {tex_num(F(budget - fee, each))}')}. The greatest whole number is {m(str(n))}.")
    return f.mc(skill, diff, stem, m(str(n)), nums(n + 1, budget // each, (budget + fee) // each, n - 1), expl)


def ine_hard(f, r, skill, diff):
    """Which point satisfies a system of inequalities."""
    m1, b1 = nz(r, -3, 3), r.randint(-4, 6)
    m2, b2 = nz(r, -3, 3), r.randint(-6, 4)
    pts = [(r.randint(-5, 5), r.randint(-6, 8)) for _ in range(40)]
    good = [p for p in pts if p[1] > m1 * p[0] + b1 and p[1] < m2 * p[0] + b2]
    bad = [p for p in pts if not (p[1] > m1 * p[0] + b1 and p[1] < m2 * p[0] + b2)]
    if not good or len(set(bad)) < 3:
        return None
    g = good[0]
    wrong = list(dict.fromkeys(bad))[:3]
    stem = (f"Which point {m('(x, y)')} is a solution to the system of inequalities?\n\n"
            f"$$y > {lin(m1, b1)}$$\n\n$$y < {lin(m2, b2)}$$")
    expl = (f"Check {m(f'({g[0]}, {g[1]})')}: {m(f'{g[1]} > {m1 * g[0] + b1}')} and {m(f'{g[1]} < {m2 * g[0] + b2}')}, both true. "
            f"Each other choice fails at least one inequality.")
    return f.mc(skill, diff, stem, m(f"({g[0]}, {g[1]})"), [m(f"({p[0]}, {p[1]})") for p in wrong], expl)


# =============================================================================
# ADVANCED MATH
# =============================================================================

def nlf_easy(f, r, skill, diff):
    """Evaluate a quadratic / exponential."""
    if r.random() < 0.5:
        a, c, k = nz(r, -4, 5), r.randint(-10, 10), nz(r, -5, 5)
        val = a * k * k + c
        stem = f"If {m(f'g(x) = {quad(a, 0, c)}')}, what is the value of {m(f'g({k})')}?"
        expl = f"{m(f'g({k}) = {a}({k})^2 {signed(c)} = {a}({k * k}) {signed(c)} = {val}')}."
        return f.mc(skill, diff, stem, m(str(val)), nums(a * 2 * k + c, (a * k) ** 2 + c, -val if val else 1, val + a), expl)
    p, b, t = r.choice([2, 3, 5, 10, 100]), r.choice([2, 3]), r.randint(2, 4)
    val = p * b ** t
    stem = f"The function {m('h')} is defined by {m(f'h(x) = {p}({b})^x')}. What is the value of {m(f'h({t})')}?"
    expl = f"{m(f'h({t}) = {p} \\cdot {b}^{t} = {p} \\cdot {b ** t} = {val}')}."
    return f.mc(skill, diff, stem, m(str(val)), nums(p * b * t, (p * b) ** t, p + b ** t, val * b), expl)


def nlf_medium(f, r, skill, diff):
    """Interpret exponential growth/decay rate."""
    p = r.choice([200, 500, 800, 1200, 2500, 5000])
    pct = r.choice([3, 4, 5, 6, 8, 12, 15, 20])
    grow = r.random() < 0.5
    base = 1 + pct / 100 if grow else 1 - pct / 100
    thing = r.choice(["population of a town", "number of subscribers", "value of a machine (in dollars)", "number of bacteria"])
    stem = (f"The {thing} {m('t')} years after 2020 is modeled by {m(f'P(t) = {p}({base:g})^t')}. "
            f"Which statement best describes the model?")
    correct = f"It {'increases' if grow else 'decreases'} by {pct}% each year."
    expl = (f"In {m('P(t) = a(b)^t')}, {m('b = 1 + r')} for growth and {m('b = 1 - r')} for decay. Here {m(f'b = {base:g}')}, "
            f"so the quantity {'grows' if grow else 'shrinks'} by {pct}% per year; {m(str(p))} is the value in 2020.")
    return f.mc(skill, diff, stem, correct,
                [f"It {'decreases' if grow else 'increases'} by {pct}% each year.",
                 f"It {'increases' if grow else 'decreases'} by {base:g}% each year.",
                 f"It {'increases' if grow else 'decreases'} by {pct} each year."], expl)


def nlf_hard(f, r, skill, diff):
    """Minimum / maximum of a quadratic."""
    h, k = r.randint(-6, 8), r.randint(-15, 15)
    a = r.choice([1, 1, 2, 3, -1, -2])
    b, c = -2 * a * h, a * h * h + k
    word = "minimum" if a > 0 else "maximum"
    stem = f"The function {m('f')} is defined by {m(f'f(x) = {quad(a, b, c)}')}. What is the {word} value of {m('f(x)')}?"
    expl = (f"The vertex is at {m(f'x = -\\frac{{b}}{{2a}} = -\\frac{{{b}}}{{{2 * a}}} = {h}')}. "
            f"{m(f'f({h}) = {k}')}, so the {word} value is {m(str(k))} (the parabola opens {'up' if a > 0 else 'down'}).")
    return f.spr(skill, diff, stem, k, expl) if r.random() < 0.5 else \
        f.mc(skill, diff, stem, m(str(k)), nums(h, c, -k if k else 5, k + a * 2), expl)


def nle_easy(f, r, skill, diff):
    """Zero-product roots."""
    p, q = r.randint(1, 9), r.randint(1, 9)
    stem = f"What is the positive solution to {m(f'(x - {p})(x + {q}) = 0')}?"
    expl = f"A product is zero when a factor is zero: {m(f'x = {p}')} or {m(f'x = -{q}')}. The positive solution is {m(str(p))}."
    return f.mc(skill, diff, stem, m(str(p)), nums(q, -p, p + q, p * q), expl)


def nle_medium(f, r, skill, diff):
    """Factorable quadratic, ask for positive root or sum."""
    p, q = r.randint(1, 10), r.randint(1, 10)
    if p == q:
        q += 1
    b, c = q - p, -p * q                       # (x - p)(x + q) = x^2 + (q-p)x - pq
    stem = f"What is the positive solution to {m(f'{quad(1, b, c)} = 0')}?"
    expl = (f"Factor: {m(f'(x - {p})(x + {q}) = 0')} (check: {m(f'-{p} \\cdot {q} = {c}')} and {m(f'-{p} + {q} = {b}')}). "
            f"Solutions {m(f'x = {p}')} and {m(f'x = -{q}')}; the positive one is {m(str(p))}.")
    return f.spr(skill, diff, stem, p, expl)


def nle_hard(f, r, skill, diff):
    """Discriminant: exactly one solution, or intersections of line and parabola."""
    if r.random() < 0.5:
        b = r.choice([2, 4, 6, 8, 10, 12, 14])
        c = (b // 2) ** 2
        stem = f"In the equation {m(f'x^2 - {b}x + c = 0')}, {m('c')} is a constant. If the equation has exactly one real solution, what is {m('c')}?"
        expl = (f"A quadratic has exactly one real solution when its discriminant is zero: {m(f'b^2 - 4ac = {b}^2 - 4c = 0')}, "
                f"so {m(f'c = \\frac{{{b * b}}}{{4}} = {c}')}.")
        return f.spr(skill, diff, stem, c, expl)
    k = r.randint(1, 9)
    mm = r.randint(-4, 4)
    # y = x^2 - k and y = mm*x + n intersect where x^2 - mm x - (k + n) = 0; choose n for two integer roots
    x1, x2 = r.randint(-5, 5), r.randint(-5, 5)
    if x1 == x2 or x1 + x2 != mm:
        return None
    n = -x1 * x2 - k
    stem = (f"The graphs of {m(f'y = x^2 - {k}')} and {m(f'y = {lin(mm, n) if mm else str(n)}')} intersect at two points. "
            f"What is the sum of the {m('x')}-coordinates of the intersection points?")
    expl = (f"Set equal: {m(f'x^2 - {k} = {lin(mm, n) if mm else str(n)}')}, i.e. {m(f'{quad(1, -mm, -(k + n))} = 0')}. "
            f"The sum of the roots of {m('x^2 + bx + c = 0')} is {m('-b')}, which is {m(str(mm))}. (The roots are {m(str(x1))} and {m(str(x2))}.)")
    return f.mc(skill, diff, stem, m(str(mm)), nums(-mm if mm else 2, k + n, x1 * x2, mm + k), expl)


def eqx_easy(f, r, skill, diff):
    """Expand (x + a)(x + b)."""
    a, b = nz(r, -7, 7), nz(r, -7, 7)
    correct = m(quad(1, a + b, a * b))
    stem = f"Which expression is equivalent to {m(f'(x {signed(a)})(x {signed(b)})')}?"
    expl = f"FOIL: {m(f'x^2 {signed(b)}x {signed(a)}x {signed(a * b)} = {quad(1, a + b, a * b)}')}."
    return f.mc(skill, diff, stem, correct,
                [m(quad(1, 0, a * b) if a * b else quad(1, a + b, 1)), m(quad(1, a * b, a + b) if a * b != a + b else quad(1, a + b, a * b + 1)),
                 m(quad(1, a - b if a != b else a + b + 1, -a * b))], expl)


def eqx_medium(f, r, skill, diff):
    """Combine polynomials."""
    a, b, c, d = r.randint(2, 9), nz(r, -9, 9), r.randint(1, 8), nz(r, -9, 9)
    e, g = nz(r, -8, 8), nz(r, -8, 8)
    A, B, C = a - c, b - d, e - g
    if A == 0:
        return None
    stem = f"Which expression is equivalent to {m(f'({quad(a, b, e)}) - ({quad(c, d, g)})')}?"
    correct = m(quad(A, B, C)) if B or C else m(f"{A}x^2")
    expl = (f"Distribute the minus sign to every term of the second polynomial, then combine like terms: "
            f"{m(f'({a} - {c})x^2 + ({b} - ({d}))x + ({e} - ({g})) = {quad(A, B, C)}')}.")
    return f.mc(skill, diff, stem, correct,
                [m(quad(a - c, b + d, e + g)) if (b + d or e + g) else m(quad(A, 1, 1)),
                 m(quad(a + c, b - d if b - d else 1, e - g if e - g else 1)),
                 m(quad(A, B, e + g)) if e + g != C else m(quad(A, -B if B else 2, C))], expl)


def eqx_hard(f, r, skill, diff):
    """Rational exponents / radicals."""
    n = r.choice([2, 3, 4, 5])
    p = r.choice([k for k in range(1, 8) if math.gcd(k, n) == 1 and k != n])
    correct = m(f"\\sqrt[{n}]{{x^{{{p}}}}}") if n != 2 else m(f"\\sqrt{{x^{{{p}}}}}")
    stem = f"For {m('x > 0')}, which expression is equivalent to {m(f'x^{{\\frac{{{p}}}{{{n}}}}}')}?"
    expl = (f"{m('x^{\\frac{p}{n}} = \\sqrt[n]{x^p}')}: the denominator is the root and the numerator is the power. "
            f"So {m(f'x^{{\\frac{{{p}}}{{{n}}}}}')} {m('=')} {correct}.")
    root = lambda k, e: m(f"\\sqrt[{k}]{{x^{{{e}}}}}") if k != 2 else m(f"\\sqrt{{x^{{{e}}}}}")
    return f.mc(skill, diff, stem, correct, [root(p, n) if p > 1 else m(f"x^{{{n}}}"), m(f"\\frac{{x^{{{p}}}}}{{{n}}}"), m(f"x^{{{p * n}}}")], expl)


# =============================================================================
# PROBLEM-SOLVING AND DATA ANALYSIS
# =============================================================================

def rat_easy(f, r, skill, diff):
    """Unit rate."""
    n = r.randint(3, 9)
    each = r.choice([2, 3, 4, 5, 6, 8, 12])
    k = r.randint(10, 20)
    thing = r.choice([("pages", "minutes"), ("miles", "hours"), ("boxes", "hours"), ("liters", "minutes")])
    stem = (f"A machine processes {n * each} {thing[0]} in {n} {thing[1]}. At this rate, how many {thing[0]} does it process in {k} {thing[1]}?")
    expl = f"Rate: {m(f'\\frac{{{n * each}}}{{{n}}} = {each}')} {thing[0]} per {thing[1][:-1]}. In {k} {thing[1]}: {m(f'{each} \\times {k} = {each * k}')}."
    return f.mc(skill, diff, stem, m(str(each * k)), nums(n * each + k, each * n * k, each * k + each, k * n), expl)


def rat_medium(f, r, skill, diff):
    """Unit conversion."""
    feet = r.randint(3, 15)
    if r.random() < 0.5:
        stem = f"A board is {feet} feet long. How many inches long is the board? (1 foot = 12 inches)"
        expl = f"{m(f'{feet} \\times 12 = {feet * 12}')} inches."
        return f.spr(skill, diff, stem, feet * 12, expl)
    kmh = r.choice([18, 36, 54, 72, 90, 108])
    ms = F(kmh * 1000, 3600)
    stem = f"A car travels at {kmh} kilometers per hour. What is this speed in meters per second? (1 kilometer = 1,000 meters)"
    expl = f"{m(f'{kmh} \\frac{{\\text{{km}}}}{{\\text{{h}}}} \\times \\frac{{1000 \\text{{ m}}}}{{1 \\text{{ km}}}} \\times \\frac{{1 \\text{{ h}}}}{{3600 \\text{{ s}}}} = {tex_num(ms)}')} m/s."
    return f.spr(skill, diff, stem, ms, expl)


def rat_hard(f, r, skill, diff):
    """Proportional mixture / scale."""
    a, b = r.randint(2, 5), r.randint(3, 8)
    if math.gcd(a, b) != 1:
        return None
    total = (a + b) * r.randint(3, 12)
    part_b = total * b // (a + b)
    stem = (f"In a mixture, the ratio of sand to gravel by weight is {a} to {b}. If the mixture weighs {total} pounds, "
            f"how many pounds of gravel does it contain?")
    expl = f"Gravel is {m(f'\\frac{{{b}}}{{{a} + {b}}} = \\frac{{{b}}}{{{a + b}}}')} of the total: {m(f'\\frac{{{b}}}{{{a + b}}} \\times {total} = {part_b}')}."
    return f.mc(skill, diff, stem, m(str(part_b)), nums(total * a // (a + b), F(total * b, a), total - b, part_b + a), expl)


def pct_easy(f, r, skill, diff):
    """Percent of a number."""
    p = r.choice([5, 10, 15, 20, 25, 30, 40, 60, 75])
    n = r.choice([40, 60, 80, 120, 160, 200, 240, 300, 400])
    val = F(p * n, 100)
    stem = f"What is {p}% of {n}?"
    expl = f"{m(f'0.{p:02d} \\times {n} = {tex_num(val)}') if p < 100 else ''}"
    return f.mc(skill, diff, stem, m(tex_num(val)), nums(F(n * 100, p) if F(n * 100, p) != val else n, p * n, n - p, val + n), expl)


def pct_medium(f, r, skill, diff):
    """Percent change / original price."""
    if r.random() < 0.5:
        old = r.choice([40, 50, 80, 120, 150, 200, 250])
        p = r.choice([10, 15, 20, 25, 30, 40])
        up = r.random() < 0.5
        new = F(old * (100 + p if up else 100 - p), 100)
        stem = f"The price of an item changed from {money(old)} to {money(new)}. By what percent did the price {'increase' if up else 'decrease'}?"
        expl = f"Percent change {m('= \\frac{\\text{change}}{\\text{original}} \\times 100')}: {m(f'\\frac{{{tex_num(abs(new - old))}}}{{{old}}} \\times 100 = {p}')}%."
        return f.spr(skill, diff, stem, p, expl)
    sale = r.choice([36, 42, 48, 54, 60, 72, 84, 90])
    p = r.choice([10, 20, 25, 40])
    orig = F(sale * 100, 100 - p)
    if orig.denominator != 1:
        return None
    stem = f"After a {p}% discount, a jacket costs {money(sale)}. What was the original price, in dollars?"
    expl = f"The sale price is {100 - p}% of the original: {m(f'{F(100 - p, 100)} x = {sale}')}... so {m(f'x = \\frac{{{sale}}}{{{F(100 - p, 100)}}} = {orig}')}."
    expl = f"The sale price is {100 - p}% of the original: {m(f'{(100 - p) / 100:g}x = {sale}')}, so {m(f'x = {orig}')}."
    return f.mc(skill, diff, stem, money(orig), [money(sale * (100 + p) / 100), money(sale + p), money(sale * 100 / (100 + p))], expl)


def pct_hard(f, r, skill, diff):
    """Successive percent changes."""
    p, q = r.choice([10, 20, 25, 30, 40, 50]), r.choice([10, 20, 25, 30, 40, 50])
    up_first = r.random() < 0.5
    factor = F(100 + p, 100) * F(100 - q, 100) if up_first else F(100 - p, 100) * F(100 + q, 100)
    pct = factor * 100
    stem = (f"A quantity is {'increased' if up_first else 'decreased'} by {p}%, and then the result is "
            f"{'decreased' if up_first else 'increased'} by {q}%. The final quantity is what percent of the original?")
    f1, f2 = (F(100 + p, 100), F(100 - q, 100)) if up_first else (F(100 - p, 100), F(100 + q, 100))
    expl = (f"Multiply the factors: {m(f'{float(f1):g} \\times {float(f2):g} = {float(factor):g}')}, which is "
            f"{m(tex_num(pct))}% of the original. Percent changes do not simply add because the second change applies to a new amount.")
    naive = 100 + (p - q if up_first else q - p)
    return f.mc(skill, diff, stem, m(tex_num(pct)) + "%", [m(str(naive)) + "%", m(str(100 + p + q)) + "%", m(tex_num(pct + 5)) + "%"], expl)


def ovd_easy(f, r, skill, diff):
    """Mean / median of a list."""
    vals = sorted(r.sample(range(2, 40), r.choice([5, 7])))
    if r.random() < 0.5:
        med = vals[len(vals) // 2]
        shuffled = vals[:]
        r.shuffle(shuffled)
        stem = f"What is the median of the data set {m(', '.join(map(str, shuffled)))}?"
        expl = f"Order the values: {m(', '.join(map(str, vals)))}. The middle value is {m(str(med))}."
        mean = F(sum(vals), len(vals))
        return f.mc(skill, diff, stem, m(str(med)), nums(shuffled[len(vals) // 2] if shuffled[len(vals) // 2] != med else med + 1, mean if mean != med else med - 1, vals[-1] - vals[0]), expl)
    total = sum(vals)
    while total % len(vals):
        vals[-1] += 1
        total = sum(vals)
    mean = total // len(vals)
    stem = f"What is the mean of the data set {m(', '.join(map(str, vals)))}?"
    expl = f"Sum {m(f'= {total}')}; there are {len(vals)} values; mean {m(f'= \\frac{{{total}}}{{{len(vals)}}} = {mean}')}."
    return f.spr(skill, diff, stem, mean, expl)


def ovd_medium(f, r, skill, diff):
    """New value needed for a target mean."""
    n = r.randint(4, 7)
    mean = r.randint(70, 90)
    target = mean + r.randint(1, 4)
    need = target * (n + 1) - mean * n
    if need > 100:
        return None
    stem = (f"A student's mean score on {n} tests is {mean}. What score does the student need on the next test to raise the mean "
            f"of all {n + 1} tests to {target}?")
    expl = (f"Current total: {m(f'{n} \\times {mean} = {n * mean}')}. Needed total: {m(f'{n + 1} \\times {target} = {(n + 1) * target}')}. "
            f"Score needed: {m(f'{(n + 1) * target} - {n * mean} = {need}')}.")
    return f.spr(skill, diff, stem, need, expl)


def ovd_hard(f, r, skill, diff):
    """Effect of an outlier on mean vs median."""
    vals = sorted(r.sample(range(10, 30), 6))
    big = r.randint(80, 150)
    stem = (f"The data set {m(', '.join(map(str, vals)))} has a value of {m(str(big))} added to it. "
            f"Which statement about the new data set compared with the original is true?")
    correct = "The mean increases more than the median does."
    expl = (f"An extreme high value pulls the mean up a lot (the sum grows by {big}), but the median only shifts to the next middle "
            f"position. Original median {m(tex_num(F(vals[2] + vals[3], 2)))}, new median {m(str(vals[3]))}; original mean "
            f"{m(tex_num(F(sum(vals), 6)))}, new mean {m(tex_num(F(sum(vals) + big, 7)))}.")
    return f.mc(skill, diff, stem, correct,
                ["The median increases more than the mean does.", "The mean and median increase by the same amount.",
                 "The mean decreases and the median increases."], expl)


def tvd_easy(f, r, skill, diff):
    """Predict from line of best fit."""
    mm, b = r.choice([1.5, 2, 2.5, 3, 4.2, 0.8]), r.choice([5, 10, 12, 20, 30])
    x = r.randint(4, 15)
    y = round(mm * x + b, 1)
    stem = (f"A scatterplot of the hours studied, {m('x')}, and test score gain, {m('y')}, for a group of students has the line of best fit "
            f"{m(f'y = {mm:g}x + {b}')}. Based on the line, what is the predicted score gain for a student who studies {x} hours?")
    expl = f"Substitute {m(f'x = {x}')}: {m(f'y = {mm:g}({x}) + {b} = {y:g}')}."
    return f.mc(skill, diff, stem, m(f"{y:g}"), [m(f"{round(mm * x, 1):g}"), m(f"{round(x + b, 1):g}"), m(f"{round(mm * (x + 1) + b, 1):g}")], expl)


def tvd_medium(f, r, skill, diff):
    """Interpret slope of a model."""
    mm = r.choice([0.5, 1.2, 2, 3.5, 4, 15, 25])
    b = r.choice([10, 20, 40, 100])
    ctx = r.choice([("the temperature, in degrees Celsius,", "the number of ice creams sold", "degree"),
                    ("the number of years since 2010", "the average price of a home, in thousands of dollars", "year"),
                    ("the number of hours of sunlight", "the height of a plant, in centimeters", "hour")])
    stem = (f"A model for {ctx[1]}, {m('y')}, as a function of {ctx[0]} {m('x')}, is {m(f'y = {mm:g}x + {b}')}. "
            f"What is the best interpretation of {m(f'{mm:g}')} in this context?")
    correct = f"For each increase of one {ctx[2]} in {m('x')}, the predicted {ctx[1].split(',')[0]} increases by {mm:g}."
    expl = f"The slope of a linear model is the predicted change in {m('y')} per one-unit increase in {m('x')}. {m(str(b))} is the predicted value when {m('x = 0')}."
    return f.mc(skill, diff, stem, correct,
                [f"The predicted {ctx[1].split(',')[0]} when {m('x = 0')} is {mm:g}.",
                 f"For each increase of {mm:g} in {m('x')}, the predicted {ctx[1].split(',')[0]} increases by one.",
                 f"The predicted {ctx[1].split(',')[0]} is always {mm:g} times {m('x')}."], expl)


def tvd_hard(f, r, skill, diff):
    """Residual."""
    mm, b = r.choice([2, 3, 1.5, 4]), r.choice([10, 15, 20, 25])
    x = r.randint(3, 12)
    pred = mm * x + b
    actual = pred + r.choice([-6, -4, -3, 3, 5, 7])
    res = actual - pred
    stem = (f"The line of best fit for a data set is {m(f'y = {mm:g}x + {b}')}. One data point is {m(f'({x}, {actual:g})')}. "
            f"What is the residual for this point (actual minus predicted)?")
    expl = f"Predicted: {m(f'{mm:g}({x}) + {b} = {pred:g}')}. Residual {m(f'= {actual:g} - {pred:g} = {res:g}')}."
    return f.spr(skill, diff, stem, F(res).limit_denominator(10) if isinstance(res, float) else res, expl)


def prb_table(r):
    """Random two-way table: (row labels, col labels, counts[2][2])."""
    rows, cols = r.choice([
        (("Juniors", "Seniors"), ("Plays a sport", "Does not play a sport")),
        (("Morning shift", "Evening shift"), ("Prefers tea", "Prefers coffee")),
        (("Grade 9", "Grade 10"), ("Bikes to school", "Does not bike")),
        (("Group A", "Group B"), ("Improved", "Did not improve")),
    ])
    counts = [[r.randint(8, 45) for _ in range(2)] for _ in range(2)]
    return rows, cols, counts


def table_md(rows, cols, counts):
    """Markdown table with totals."""
    t = f"| | {cols[0]} | {cols[1]} | Total |\n|---|---|---|---|\n"
    for i in range(2):
        t += f"| {rows[i]} | {counts[i][0]} | {counts[i][1]} | {sum(counts[i])} |\n"
    t += f"| Total | {counts[0][0] + counts[1][0]} | {counts[0][1] + counts[1][1]} | {sum(map(sum, counts))} |"
    return t


def prb_easy(f, r, skill, diff):
    """Simple probability."""
    red, blue, green = r.randint(2, 12), r.randint(2, 12), r.randint(2, 12)
    total = red + blue + green
    ans = F(blue, total)
    stem = f"A bag contains {red} red, {blue} blue, and {green} green marbles. One marble is chosen at random. What is the probability that it is blue?"
    expl = f"Favorable outcomes: {blue}. Total: {m(f'{red} + {blue} + {green} = {total}')}. Probability {m(f'= {tex_num(ans)}')}."
    return f.mc(skill, diff, stem, m(tex_num(ans)), nums(F(blue, red + green), F(1, 3) if F(1, 3) != ans else F(1, 2), F(red + green, total)), expl)


def prb_medium(f, r, skill, diff):
    """Probability from a two-way table (row total)."""
    rows, cols, c = prb_table(r)
    i, j = r.randint(0, 1), r.randint(0, 1)
    total = sum(map(sum, c))
    ans = F(c[i][j], total)
    stem = (f"The table shows survey results.\n\n{table_md(rows, cols, c)}\n\nIf one person is chosen at random from all those surveyed, "
            f"what is the probability that the person is in \"{rows[i]}\" and \"{cols[j]}\"?")
    expl = f"There are {c[i][j]} such people out of {total} total: {m(tex_num(ans))}."
    return f.mc(skill, diff, stem, m(tex_num(ans)), nums(F(c[i][j], sum(c[i])), F(c[i][j], c[0][j] + c[1][j]), F(sum(c[i]), total)), expl)


def prb_hard(f, r, skill, diff):
    """Conditional probability from a two-way table."""
    rows, cols, c = prb_table(r)
    i, j = r.randint(0, 1), r.randint(0, 1)
    col_total = c[0][j] + c[1][j]
    ans = F(c[i][j], col_total)
    stem = (f"The table shows survey results.\n\n{table_md(rows, cols, c)}\n\nA person is chosen at random from those in \"{cols[j]}\". "
            f"What is the probability that the person is in \"{rows[i]}\"?")
    expl = (f"\"Given {cols[j]}\" restricts the sample to that column: {col_total} people. Of these, {c[i][j]} are in \"{rows[i]}\". "
            f"Probability {m(f'= \\frac{{{c[i][j]}}}{{{col_total}}}')}" + (f" {m(f'= {tex_num(ans)}')}." if ans.denominator != col_total else "."))
    return f.mc(skill, diff, stem, m(tex_num(ans)), nums(F(c[i][j], sum(c[i])), F(c[i][j], sum(map(sum, c))), F(col_total, sum(map(sum, c)))), expl)


def inf_easy(f, r, skill, diff):
    """Estimate population count from a sample proportion."""
    n = r.choice([50, 80, 100, 120, 200, 250])
    k = r.randint(5, n // 2)
    pop = r.choice([1000, 1200, 2000, 2500, 4000, 5000])
    est = F(k, n) * pop
    if est.denominator != 1:
        return None
    stem = (f"In a random sample of {n} students at a school of {pop:,} students, {k} said they walk to school. Based on the sample, "
            f"about how many students at the school walk to school?")
    expl = f"Sample proportion {m(f'\\frac{{{k}}}{{{n}}}')}. Estimate {m(f'\\frac{{{k}}}{{{n}}} \\times {pop} = {est}')}."
    return f.mc(skill, diff, stem, m(f"{int(est):,}".replace(",", "{,}")), [m(str(k)), m(f"{pop - int(est):,}".replace(",", "{,}")), m(f"{k * 10:,}".replace(",", "{,}"))], expl)


def inf_medium(f, r, skill, diff):
    """Interpret a margin of error."""
    est, moe = r.choice([42, 55, 61, 68, 37]), r.choice([2, 3, 4, 5])
    stem = (f"A random sample of voters found that {est}% support a proposal, with a margin of error of {moe} percentage points. "
            f"Which statement is the most appropriate conclusion?")
    correct = f"It is plausible that between {est - moe}% and {est + moe}% of all voters support the proposal."
    expl = (f"A margin of error gives a range of plausible values for the population: {m(f'{est} \\pm {moe}')}, i.e. "
            f"{est - moe}% to {est + moe}%. It does not guarantee the exact value or describe individual voters.")
    return f.mc(skill, diff, stem, correct,
                [f"Exactly {est}% of all voters support the proposal.",
                 f"Between {est - moe}% and {est + moe}% of the voters in the sample support the proposal.",
                 f"The proposal will be supported by at least {est + moe}% of voters."], expl)


def inf_hard(f, r, skill, diff):
    """Effect of sample size on margin of error, or estimate range from a sample."""
    if r.random() < 0.5:
        n, k, pop, moe = r.choice([200, 400, 500]), r.randint(20, 60), r.choice([8000, 12000, 20000, 30000]), r.choice([3, 4, 5])
        p = round(100 * k / n)
        lo, hi = pop * (p - moe) // 100, pop * (p + moe) // 100
        stem = (f"In a random sample of {n} households in a city of {pop:,} households, {p}% own a dog. The margin of error is {moe} percentage points. "
                f"Which is a plausible number of dog-owning households in the whole city?")
        good = (lo + hi) // 2
        expl = f"Plausible range {p - moe}% to {p + moe}% of {pop:,}: about {lo:,} to {hi:,} households. Only {good:,} falls in this range."
        fmt = lambda v: m(f"{v:,}".replace(",", "{,}"))
        return f.mc(skill, diff, stem, fmt(good), [fmt(round(n * p / 100)), fmt(hi + pop * 6 // 100), fmt(max(1, lo - pop * 6 // 100))], expl)
    stem = ("Two researchers estimate the mean height of the same population using random samples. Researcher A uses a sample of "
            f"{r.choice([50, 80, 100])} people and Researcher B uses a sample of {r.choice([400, 500, 800])} people. Which is most likely true?")
    correct = "Researcher B's estimate has a smaller margin of error."
    expl = ("Larger random samples vary less from the true population value, so their margin of error is smaller. "
            "Sample size does not change the population mean itself.")
    return f.mc(skill, diff, stem, correct,
                ["Researcher A's estimate has a smaller margin of error.", "Both estimates have the same margin of error.",
                 "Researcher B's sample mean must be larger."], expl)


ESC_CONTEXTS = [
    # (population, units, treatment, outcome)
    ("adults in a city", "adults", "drinking green tea daily", "better sleep"),
    ("students at a large high school", "students", "using a flashcard app", "higher quiz scores"),
    ("employees of a company", "employees", "a four-day work week", "lower reported stress"),
    ("tomato plants in a greenhouse", "plants", "a new fertilizer", "taller growth"),
    ("customers of a grocery chain", "customers", "receiving a weekly coupon email", "more store visits"),
    ("patients at a clinic", "patients", "a 10-minute daily walk", "lower blood pressure"),
    ("residents of a town", "residents", "a free bus pass", "fewer car trips"),
    ("members of a library", "members", "a reading-challenge program", "more books borrowed"),
]


def esc_any(f, r, skill, diff):
    """Which conclusion is supported by the study design (random sampling x random assignment)."""
    pop, units, treat, outcome = r.choice(ESC_CONTEXTS)
    sample, assign = r.choice([(True, True), (True, False), (False, True), (False, False)])
    hard = sample != assign                                     # Mixed designs are the subtle ones
    if (diff == "Hard") != hard:
        return None
    n = r.choice([60, 120, 200, 300, 500])
    how = f"randomly selected {n} {pop}" if sample else f"recruited {n} volunteers from the {pop}"
    if assign:
        desc = f"researchers {how} and randomly assigned half of them to {treat}; those assigned to {treat} showed {outcome}"
    else:
        desc = f"researchers {how} and asked whether they were already {treat.replace('a ', '').replace('using', 'using').strip()}; those who were showed {outcome}"
    cause = f"{treat} causes {outcome}"
    assoc = f"{treat} is associated with {outcome}"
    if sample and assign:
        correct = f"{cause.capitalize()} among {pop}."
        wrong = [f"{assoc.capitalize()} among {pop}, but a causal conclusion is not possible.",
                 f"{cause.capitalize()} only for the {units} in the study.", f"{cause.capitalize()} for everyone in the country."]
        why = "Random selection lets the result generalize to the population sampled, and random assignment supports a cause-and-effect conclusion."
    elif sample:
        correct = f"{assoc.capitalize()} among {pop}, but it cannot be concluded that one causes the other."
        wrong = [f"{cause.capitalize()} among {pop}.", f"{cause.capitalize()} only for the {units} in the study.",
                 f"No conclusion can be drawn about {pop}."]
        why = "Random selection allows generalizing to the population, but without random assignment only an association can be concluded."
    elif assign:
        correct = f"{cause.capitalize()} for the {units} in the study, but the result may not generalize to all {pop}."
        wrong = [f"{cause.capitalize()} among all {pop}.", f"{assoc.capitalize()}, but no causal conclusion is possible.",
                 f"{assoc.capitalize()} among all {pop}."]
        why = "Random assignment supports causation for the participants, but volunteers were not randomly selected, so the result cannot be generalized."
    else:
        correct = f"{assoc.capitalize()} for the {units} in the study only; no causal or general conclusion is supported."
        wrong = [f"{cause.capitalize()} among all {pop}.", f"{cause.capitalize()} for the {units} in the study.",
                 f"{assoc.capitalize()} among all {pop}."]
        why = "With volunteers and no random assignment, the study supports neither causation nor generalization beyond the participants."
    stem = f"In a study, {desc}. Which conclusion is best supported by the design of the study?"
    return f.mc(skill, diff, stem, correct, wrong, why)


# =============================================================================
# GEOMETRY AND TRIGONOMETRY
# =============================================================================

def av_easy(f, r, skill, diff):
    """Area of rectangle / triangle."""
    a, b = r.randint(3, 20), r.randint(3, 20)
    if r.random() < 0.5:
        stem = f"A rectangle has length {a} centimeters and width {b} centimeters. What is its area, in square centimeters?"
        expl = f"Area {m(f'= \\ell w = {a} \\times {b} = {a * b}')}."
        return f.mc(skill, diff, stem, m(str(a * b)), nums(2 * (a + b), a + b, a * b // 2 if a * b % 2 == 0 else a * b + 1), expl)
    b2 = b * 2
    stem = f"A triangle has a base of {b2} inches and a height of {a} inches. What is its area, in square inches?"
    expl = f"Area {m(f'= \\frac{{1}}{{2}}bh = \\frac{{1}}{{2}}({b2})({a}) = {a * b}')}."
    return f.spr(skill, diff, stem, a * b, expl)


def av_medium(f, r, skill, diff):
    """Volume of cylinder / box (answers in terms of pi)."""
    rad, h = r.randint(2, 9), r.randint(3, 12)
    if r.random() < 0.5:
        v = rad * rad * h
        stem = f"A right circular cylinder has radius {rad} and height {h}. What is its volume?"
        expl = f"{m(f'V = \\pi r^2 h = \\pi ({rad})^2 ({h}) = {v}\\pi')}."
        return f.mc(skill, diff, stem, m(f"{v}\\pi"), [m(f"{2 * rad * h}\\pi"), m(f"{rad * h}\\pi"), m(f"{(2 * rad) ** 2 * h}\\pi")], expl)
    a, b, c = r.randint(2, 9), r.randint(2, 9), r.randint(2, 9)
    stem = f"A rectangular box has dimensions {a} by {b} by {c}. What is its volume?"
    expl = f"{m(f'V = {a} \\times {b} \\times {c} = {a * b * c}')}."
    return f.spr(skill, diff, stem, a * b * c, expl)


def av_hard(f, r, skill, diff):
    """Scale factor effect on area / volume; sphere / cone."""
    if r.random() < 0.5:
        k = r.choice([2, 3, 4])
        kind = r.choice(["area", "volume"])
        factor = k ** 2 if kind == "area" else k ** 3
        stem = (f"Each dimension of a solid is multiplied by {k}. By what factor is its {'surface area' if kind == 'area' else 'volume'} multiplied?")
        expl = f"Lengths scale by {m(str(k))}, areas by {m(f'{k}^2 = {k ** 2}')}, and volumes by {m(f'{k}^3 = {k ** 3}')}."
        return f.spr(skill, diff, stem, factor, expl)
    rad = r.choice([3, 6, 9])
    h = r.choice([4, 5, 8, 10])
    v = F(1, 3) * rad * rad * h
    stem = f"A right circular cone has radius {rad} and height {h}. What is its volume?"
    expl = f"{m(f'V = \\frac{{1}}{{3}}\\pi r^2 h = \\frac{{1}}{{3}}\\pi({rad})^2({h}) = {tex_num(v)}\\pi')}."
    return f.mc(skill, diff, stem, m(f"{tex_num(v)}\\pi"), [m(f"{rad * rad * h}\\pi"), m(f"{tex_num(F(1, 3) * rad * h)}\\pi"), m(f"{tex_num(F(2, 3) * rad * rad * h)}\\pi")], expl)


def lat_easy(f, r, skill, diff):
    """Third angle of a triangle."""
    a, b = r.randint(25, 85), r.randint(25, 85)
    c = 180 - a - b
    if c <= 5:
        return None
    stem = f"In a triangle, two angles measure {a}° and {b}°. What is the measure, in degrees, of the third angle?"
    expl = f"Angles in a triangle sum to 180°: {m(f'180 - {a} - {b} = {c}')}."
    return f.spr(skill, diff, stem, c, expl)


def lat_medium(f, r, skill, diff):
    """Parallel lines and a transversal."""
    x = r.randint(35, 140)
    kind = r.choice(["alternate interior", "corresponding", "same-side interior"])
    ans = 180 - x if kind == "same-side interior" else x
    stem = (f"Two parallel lines are cut by a transversal. One angle measures {x}°. What is the measure of the angle that forms a pair of "
            f"{kind} angles with it?")
    expl = (f"{kind.capitalize()} angles formed by parallel lines are "
            + ("supplementary, so the angle is " + m(f'180 - {x} = {ans}') + "°." if kind == "same-side interior" else f"congruent, so the angle is {x}°."))
    return f.mc(skill, diff, stem, m(str(ans)) + "°", [m(str(180 - ans)) + "°" if 180 - ans != ans else m("90") + "°", m(str(90 - x if x < 90 else x - 90)) + "°", m(str(360 - x)) + "°"], expl)


def lat_hard(f, r, skill, diff):
    """Similar triangles."""
    a, b = r.randint(3, 12), r.randint(3, 12)
    k = r.choice([F(3, 2), 2, F(5, 2), 3, F(4, 3)])
    c = a * k
    ans = b * k
    if F(ans).denominator != 1 or F(c).denominator != 1:
        return None
    stem = (f"Triangle {m('ABC')} is similar to triangle {m('DEF')}, with {m('A')}, {m('B')}, {m('C')} corresponding to {m('D')}, {m('E')}, {m('F')}. "
            f"If {m(f'AB = {a}')}, {m(f'BC = {b}')}, and {m(f'DE = {tex_num(F(c))}')}, what is {m('EF')}?")
    expl = f"Scale factor {m(f'\\frac{{DE}}{{AB}} = \\frac{{{tex_num(F(c))}}}{{{a}}} = {tex_num(F(k))}')}. So {m(f'EF = {b} \\times {tex_num(F(k))} = {tex_num(F(ans))}')}."
    return f.spr(skill, diff, stem, F(ans), expl)


TRIPLES = [(3, 4, 5), (5, 12, 13), (8, 15, 17), (7, 24, 25), (6, 8, 10), (9, 12, 15), (20, 21, 29)]


def rtt_easy(f, r, skill, diff):
    """Pythagorean theorem (hypotenuse or missing leg)."""
    a, b, c = r.choice(TRIPLES)
    k = r.choice([1, 2, 3])
    a, b, c = a * k, b * k, c * k
    if r.random() < 0.5:
        stem = f"A right triangle has a hypotenuse of length {c} and one leg of length {a}. What is the length of the other leg?"
        expl = f"{m(f'b = \\sqrt{{{c}^2 - {a}^2}} = \\sqrt{{{c * c - a * a}}} = {b}')}."
        return f.spr(skill, diff, stem, b, expl)
    stem = f"A right triangle has legs of length {a} and {b}. What is the length of the hypotenuse?"
    expl = f"{m(f'c = \\sqrt{{{a}^2 + {b}^2}} = \\sqrt{{{a * a + b * b}}} = {c}')}."
    return f.mc(skill, diff, stem, m(str(c)), nums(a + b, c + 1, b + 1 if b + 1 != c else b + 2), expl)


def rtt_medium(f, r, skill, diff):
    """Trig ratio from sides."""
    a, b, c = r.choice(TRIPLES)
    fn = r.choice(["sin", "cos", "tan"])
    val = {"sin": F(a, c), "cos": F(b, c), "tan": F(a, b)}[fn]
    stem = (f"In right triangle {m('PQR')}, angle {m('R')} is the right angle, {m(f'PR = {b}')}, {m(f'QR = {a}')}, and {m(f'PQ = {c}')}. "
            f"What is {m(f'\\{fn} P')}?")
    expl = (f"From angle {m('P')}: opposite side {m(f'QR = {a}')}, adjacent side {m(f'PR = {b}')}, hypotenuse {m(f'PQ = {c}')}. "
            f"{m(f'\\{fn} P')} = " + {"sin": "opposite/hypotenuse", "cos": "adjacent/hypotenuse", "tan": "opposite/adjacent"}[fn] + f" {m(f'= {tex_num(val)}')}.")
    others = [F(a, c), F(b, c), F(a, b), F(b, a), F(c, a)]
    return f.mc(skill, diff, stem, m(tex_num(val)), nums(*[o for o in others if o != val][:3]), expl)


def rtt_hard(f, r, skill, diff):
    """Complementary angle identity or special right triangles."""
    if r.random() < 0.5:
        x = r.randint(10, 80)
        a, b, c = r.choice(TRIPLES)
        stem = f"In a right triangle, {m(f'\\sin(x^\\circ) = \\frac{{{a}}}{{{c}}}')}. What is {m(f'\\cos({90 - x}^\\circ)')} if {m(f'x = {x}')}?"
        stem = f"In a right triangle with acute angles {m('x^\\circ')} and {m('y^\\circ')}, {m(f'\\sin(x^\\circ) = \\frac{{{a}}}{{{c}}}')}. What is {m('\\cos(y^\\circ)')}?"
        expl = f"The acute angles are complementary ({m('x + y = 90')}), and {m('\\cos(90^\\circ - x) = \\sin(x)')}. So {m(f'\\cos(y^\\circ) = \\frac{{{a}}}{{{c}}}')}."
        return f.spr(skill, diff, stem, F(a, c), expl)
    s = r.randint(2, 12)
    stem = f"In a 30°-60°-90° triangle, the side opposite the 30° angle has length {s}. What is the length of the hypotenuse?"
    expl = f"In a 30°-60°-90° triangle the sides are in the ratio {m('1 : \\sqrt{3} : 2')}, so the hypotenuse is {m(f'2 \\times {s} = {2 * s}')}."
    return f.mc(skill, diff, stem, m(str(2 * s)), [m(f"{s}\\sqrt{{3}}"), m(f"{s}\\sqrt{{2}}"), m(str(s * 3))], expl)


def cir_easy(f, r, skill, diff):
    """Circumference / area."""
    rad = r.randint(2, 15)
    if r.random() < 0.5:
        stem = f"A circle has radius {rad}. What is its area?"
        expl = f"{m(f'A = \\pi r^2 = \\pi({rad})^2 = {rad * rad}\\pi')}."
        return f.mc(skill, diff, stem, m(f"{rad * rad}\\pi"), [m(f"{2 * rad}\\pi"), m(f"{rad}\\pi"), m(f"{4 * rad * rad}\\pi")], expl)
    stem = f"A circle has diameter {2 * rad}. What is its circumference?"
    expl = f"{m(f'C = \\pi d = {2 * rad}\\pi')}."
    return f.mc(skill, diff, stem, m(f"{2 * rad}\\pi"), [m(f"{rad * rad}\\pi"), m(f"{rad}\\pi"), m(f"{4 * rad * rad}\\pi")], expl)


def cir_medium(f, r, skill, diff):
    """Arc length / sector area."""
    rad = r.choice([3, 4, 6, 9, 10, 12])
    ang = r.choice([30, 45, 60, 90, 120, 150, 270])
    arc = F(ang, 360) * 2 * rad
    stem = f"In a circle with radius {rad}, a central angle measures {ang}°. What is the length of the arc it intercepts?"
    expl = f"Arc length {m(f'= \\frac{{{ang}}}{{360}} \\times 2\\pi({rad}) = {tex_num(arc)}\\pi')}."
    sector = F(ang, 360) * rad * rad
    return f.mc(skill, diff, stem, m(f"{tex_num(arc)}\\pi"), [m(f"{tex_num(sector)}\\pi"), m(f"{tex_num(arc * 2)}\\pi"), m(f"{tex_num(F(ang, 180))}\\pi")], expl)


def cir_hard(f, r, skill, diff):
    """Circle equation: complete the square for radius / center."""
    h, k, rad = r.randint(-6, 6), r.randint(-6, 6), r.randint(2, 9)
    D, E = -2 * h, -2 * k
    Fc = h * h + k * k - rad * rad
    eq = f"x^2 {signed(D)}x + y^2 {signed(E)}y {signed(Fc)} = 0".replace("+ 0x ", "").replace("+ 0y ", "")
    stem = f"The equation of a circle in the {m('xy')}-plane is {m(eq)}. What is the radius of the circle?"
    expl = (f"Complete the square: {m(f'(x {signed(-h)})^2 + (y {signed(-k)})^2 = {h * h} + {k * k} {signed(-Fc)} = {rad * rad}')}. "
            f"The radius is {m(f'\\sqrt{{{rad * rad}}} = {rad}')}.")
    return f.spr(skill, diff, stem, rad, expl)


# =============================================================================
# Registry: skill -> difficulty -> templates
# =============================================================================

GENERATORS = {
    "ALG.LE1": {"Easy": [le1_easy], "Medium": [le1_medium], "Hard": [le1_hard]},
    "ALG.LF": {"Easy": [lf_easy], "Medium": [lf_medium], "Hard": [lf_hard]},
    "ALG.LE2": {"Easy": [le2_easy], "Medium": [le2_medium], "Hard": [le2_hard]},
    "ALG.SYS": {"Easy": [sys_easy], "Medium": [sys_medium], "Hard": [sys_hard]},
    "ALG.INE": {"Easy": [ine_easy], "Medium": [ine_medium], "Hard": [ine_hard]},
    "ADV.NLF": {"Easy": [nlf_easy], "Medium": [nlf_medium], "Hard": [nlf_hard]},
    "ADV.NLE": {"Easy": [nle_easy], "Medium": [nle_medium], "Hard": [nle_hard]},
    "ADV.EQX": {"Easy": [eqx_easy], "Medium": [eqx_medium], "Hard": [eqx_hard]},
    "PSD.RAT": {"Easy": [rat_easy], "Medium": [rat_medium], "Hard": [rat_hard]},
    "PSD.PCT": {"Easy": [pct_easy], "Medium": [pct_medium], "Hard": [pct_hard]},
    "PSD.OVD": {"Easy": [ovd_easy], "Medium": [ovd_medium], "Hard": [ovd_hard]},
    "PSD.TVD": {"Easy": [tvd_easy], "Medium": [tvd_medium], "Hard": [tvd_hard]},
    "PSD.PRB": {"Easy": [prb_easy], "Medium": [prb_medium], "Hard": [prb_hard]},
    "PSD.INF": {"Easy": [inf_easy], "Medium": [inf_medium], "Hard": [inf_hard]},
    "PSD.ESC": {"Medium": [esc_any], "Hard": [esc_any]},
    "GEO.AV": {"Easy": [av_easy], "Medium": [av_medium], "Hard": [av_hard]},
    "GEO.LAT": {"Easy": [lat_easy], "Medium": [lat_medium], "Hard": [lat_hard]},
    "GEO.RTT": {"Easy": [rtt_easy], "Medium": [rtt_medium], "Hard": [rtt_hard]},
    "GEO.CIR": {"Easy": [cir_easy], "Medium": [cir_medium], "Hard": [cir_hard]},
}
