# Advanced Math

Advanced Math is about **35% of the Math section**. It covers nonlinear relationships, mainly **quadratics**, **exponentials**, polynomials, radicals and rational expressions.

> **Big idea:** know the three forms of a quadratic and what each one shows instantly. Know how to read growth and decay from an exponential.

## Nonlinear functions {#ADV.NLF}

**What it tests:** evaluating and interpreting quadratic and exponential functions, their graphs, vertices, and growth/decay rates.

**Quadratic forms**

| Form | Shows you |
|---|---|
| Standard $f(x) = ax^2 + bx + c$ | $y$-intercept $c$; vertex at $x = -\frac{b}{2a}$ |
| Vertex $f(x) = a(x - h)^2 + k$ | vertex $(h, k)$: minimum if $a > 0$, maximum if $a < 0$ |
| Factored $f(x) = a(x - p)(x - q)$ | $x$-intercepts $p$ and $q$ |

**Exponential models.** In $P(t) = a(b)^t$, $a$ is the starting value. If $b = 1 + r$ the quantity **grows** by $r$ (as a percent) each period. If $b = 1 - r$ it **decays** by $r$.

**Worked example.** $P(t) = 500(1.06)^t$ grows 6% per year from 500. $P(t) = 800(0.85)^t$ decays 15% per year.

**Traps**
- Reading $1.06$ as "grows by 1.06%". It means **6%**.
- Forgetting that the vertex is the min/max *value* $k$, not the $x$-coordinate $h$.

## Nonlinear equations and systems {#ADV.NLE}

**What it tests:** solving quadratic equations, counting solutions, and intersections of a line and a parabola.

**Strategy**
- **Factor** when you can: $x^2 + 2x - 15 = 0 \Rightarrow (x + 5)(x - 3) = 0 \Rightarrow x = -5$ or $3$.
- **Quadratic formula** when you can't: $x = \dfrac{-b \pm \sqrt{b^2 - 4ac}}{2a}$.
- **Discriminant** $D = b^2 - 4ac$: $D > 0$ gives two real solutions, $D = 0$ one, $D < 0$ none.
- For $ax^2 + bx + c = 0$, sum of roots $= -\frac{b}{a}$ and product $= \frac{c}{a}$.

**Worked example.** For what $c$ does $x^2 - 8x + c = 0$ have exactly one solution? Set $D = 64 - 4c = 0$, so $c = 16$.

**Line and parabola.** Set the two expressions equal, move everything to one side, and solve the quadratic. Its solutions are the $x$-coordinates of the intersection points.

**Traps**
- Dividing both sides by $x$ (you lose the solution $x = 0$).
- Sign errors in $-b$ in the quadratic formula.

## Equivalent expressions {#ADV.EQX}

**What it tests:** expanding, factoring, combining polynomials, exponent rules, radicals and rational expressions.

**Key rules**
- FOIL: $(x + a)(x + b) = x^2 + (a + b)x + ab$.
- Special products: $(a + b)^2 = a^2 + 2ab + b^2$; $a^2 - b^2 = (a - b)(a + b)$.
- Exponents: $x^a \cdot x^b = x^{a+b}$, $\frac{x^a}{x^b} = x^{a-b}$, $(x^a)^b = x^{ab}$, $x^{-a} = \frac{1}{x^a}$.
- Rational exponents: $x^{\frac{p}{n}} = \sqrt[n]{x^p}$ (the denominator is the root).

**Worked example.** $(3x^2 + 2x - 5) - (x^2 - 4x + 1)$: distribute the minus sign, giving $3x^2 + 2x - 5 - x^2 + 4x - 1 = 2x^2 + 6x - 6$.

**Traps**
- Subtracting only the first term of the second polynomial.
- $(a + b)^2 \ne a^2 + b^2$.
- **Check fast:** plug in a number like $x = 2$ into the original and each choice.
