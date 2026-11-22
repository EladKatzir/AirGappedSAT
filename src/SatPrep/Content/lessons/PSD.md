# Problem-Solving and Data Analysis

About **15% of the Math section**. These are real-world questions: rates, percents, data sets, scatterplots, probability, and judging what a study can actually prove.

> **Big idea:** most mistakes here come from reading too fast. Underline what is being compared, what the total is, and what the question really asks for.

## Ratios, rates, proportions, and units {#PSD.RAT}

**Strategy**
- Write rates as fractions and let the units cancel: $\frac{\text{miles}}{\text{hour}} \times \text{hours} = \text{miles}$.
- For a ratio $a : b$, the parts are $\frac{a}{a+b}$ and $\frac{b}{a+b}$ of the whole.
- Set up a proportion $\frac{a}{b} = \frac{c}{d}$ and cross-multiply.

**Worked example.** 72 km/h in m/s: $72 \times \frac{1000}{3600} = 20$ m/s.

**Traps:** using the ratio's parts as if they were totals; flipping a conversion factor (check that the units cancel).

## Percentages {#PSD.PCT}

**Strategy**
- $p\%$ of $n$ is $\frac{p}{100} \cdot n$.
- Percent change $= \frac{\text{new} - \text{old}}{\text{old}} \times 100$.
- Increase by $p\%$: multiply by $1 + \frac{p}{100}$. Decrease: multiply by $1 - \frac{p}{100}$.
- **Successive changes multiply.** They do not add.

**Worked example.** Up 20% then down 20%: $1.2 \times 0.8 = 0.96$, which is 96% of the original (a 4% net decrease).

**Traps:** dividing by the new value instead of the old; "reverse" discount problems (if \$60 is after a 25% discount, the original is $60 \div 0.75 = 80$, **not** $60 \times 1.25$).

## One-variable data {#PSD.OVD}

**Key ideas**
- **Mean** = sum ÷ count. **Median** = middle value in order (average the two middle values if the count is even).
- **Range** = max − min. **Standard deviation** measures spread: more spread out means a larger SD.
- An **outlier** moves the mean a lot but the median very little.

**Worked example.** Mean of 5 tests is 82. To make the 6-test mean 84, the total must be $6 \times 84 = 504$, and the current total is $410$, so the next score must be $94$.

**Traps:** forgetting to sort before finding the median; reading a frequency table as raw values.

## Two-variable data {#PSD.TVD}

**Key ideas**
- A **line of best fit** predicts $y$ from $x$: plug in $x$.
- **Slope** in context: predicted change in $y$ for each 1-unit increase in $x$.
- **Residual** $=$ actual $-$ predicted. Positive means the point is above the line.
- Linear vs exponential: equal *differences* per step mean linear; equal *ratios* per step mean exponential.

**Traps:** treating the model's prediction as the actual value; interpreting the intercept when $x = 0$ makes no sense in context.

## Probability and conditional probability {#PSD.PRB}

**Strategy**
- Probability $= \frac{\text{favorable outcomes}}{\text{total outcomes}}$.
- **Conditional:** "given that…" shrinks the total to only that group. In a two-way table, use the row or column total for the given group.

**Worked example.** If 40 students play a sport and 15 of them are juniors, $P(\text{junior} \mid \text{plays}) = \frac{15}{40}$.

**Traps:** using the grand total when the question says "given"; mixing up rows and columns.

## Inference from sample statistics and margin of error {#PSD.INF}

**Key ideas**
- A **random sample** can be used to estimate the whole population: sample proportion × population size.
- A **margin of error** gives a range of plausible values: estimate $\pm$ margin.
- **Larger** random samples give **smaller** margins of error.

**Traps:** saying the population value is *exactly* the sample value; applying results to a group that was not sampled.

## Evaluating statistical claims {#PSD.ESC}

**The two questions to ask about any study**

| Design feature | What it allows |
|---|---|
| **Random sampling** from a population | Generalize results to that population |
| **Random assignment** to treatment groups | Conclude cause and effect |

- Both: causal conclusion about the population.
- Only random sampling: association in the population, but no causation.
- Only random assignment: causation, but only for the participants.
- Neither (volunteers, no assignment): only an association among participants.

**Traps:** choices that say "proves", or that generalize to "all people" when only volunteers were studied.
