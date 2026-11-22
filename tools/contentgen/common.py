"""
Shared helpers for SatPrep content generators.

Every generator produces question dicts in the content-pack format
(see src/SatPrep/Content/packs/README.md). Answers are always computed,
never typed by hand, so the answer key is correct by construction.
"""
import json
import random
from fractions import Fraction

LETTERS = ["A", "B", "C", "D"]  # Multiple-choice labels


def tex_num(x):
    """Formats a number (int / Fraction / float) as LaTeX: 3, -\\frac{2}{5}, 2.5."""
    if isinstance(x, Fraction):
        if x.denominator == 1:
            return str(x.numerator)
        sign = "-" if x < 0 else ""
        return f"{sign}\\frac{{{abs(x.numerator)}}}{{{x.denominator}}}"
    if isinstance(x, float):
        return f"{x:g}"
    return str(x)


def plain_num(x):
    """Formats a number for student-produced answers: 3, -2/5, 2.5."""
    if isinstance(x, Fraction):
        return str(x.numerator) if x.denominator == 1 else f"{x.numerator}/{x.denominator}"
    if isinstance(x, float):
        return f"{x:g}"
    return str(x)


def spr_answer(x):
    """Accepted answers for a student-produced response (fraction and exact decimal forms)."""
    forms = [plain_num(x)]
    if isinstance(x, Fraction) and x.denominator != 1:
        dec = x.numerator / x.denominator
        if len(f"{dec:.10g}") <= 7:          # Terminating decimal that fits the answer box
            forms.append(f"{dec:.10g}")
    return "|".join(dict.fromkeys(forms))   # Unique, order kept


def money(x):
    """Markdown-safe dollar amount (escaped so it is not read as math): \\$12.50"""
    if isinstance(x, Fraction):
        x = float(x)
    s = f"{x:,.2f}" if (isinstance(x, float) and x != int(x)) else f"{int(x):,}"
    return f"\\${s}"


def m(expr):
    """Wraps LaTeX in inline math delimiters."""
    return f"${expr}$"


class QuestionFactory:
    """
    Builds question dicts and keeps them unique.

    rng      - seeded random generator (stable output across runs)
    prefix   - id prefix, e.g. "gen-math"
    seen     - stems already produced (duplicates are rejected)
    counters - running number per skill for ids
    """

    def __init__(self, seed, prefix):
        self.rng = random.Random(seed)   # Deterministic randomness
        self.prefix = prefix             # Id prefix for this pack
        self.seen = set()                # Stem+passage fingerprints already used
        self.counters = {}               # skill -> next number

    def _next_id(self, skill):
        """Returns the next stable id for a skill, e.g. gen-math-alg-lf-0007."""
        n = self.counters.get(skill, 0) + 1
        self.counters[skill] = n
        return f"{self.prefix}-{skill.lower().replace('.', '-')}-{n:04d}"

    def mc(self, skill, difficulty, stem, correct, distractors, explanation, passage=None):
        """
        Multiple-choice question. `correct` and `distractors` are display strings;
        choices are shuffled and the answer letter computed. Returns None if the
        choices are not 4 distinct values or the stem is a duplicate.
        """
        choices = [correct] + [d for d in distractors if d != correct]
        choices = list(dict.fromkeys(choices))[:4]                # Unique, keep correct first
        if len(choices) < 4:
            return None
        key = (passage or "") + "|" + stem + "|" + "|".join(sorted(choices))
        if key in self.seen:
            return None
        self.seen.add(key)
        self.rng.shuffle(choices)
        return {
            "id": self._next_id(skill),
            "skill": skill,
            "difficulty": difficulty,
            "format": "MultipleChoice",
            "passage": passage,
            "stem": stem,
            "choices": choices,
            "answer": LETTERS[choices.index(correct)],
            "explanation": explanation,
        }

    def spr(self, skill, difficulty, stem, answer, explanation, passage=None):
        """Student-produced (typed numeric) question. Returns None on duplicates."""
        key = (passage or "") + "|" + stem
        if key in self.seen:
            return None
        self.seen.add(key)
        return {
            "id": self._next_id(skill),
            "skill": skill,
            "difficulty": difficulty,
            "format": "StudentProduced",
            "passage": passage,
            "stem": stem,
            "answer": spr_answer(answer),
            "explanation": explanation,
        }


def collect(factory, generators, per_cell, max_tries=400):
    """
    Runs each (skill, difficulty) generator until it yields `per_cell`
    unique questions (or gives up after max_tries attempts).
    generators: dict skill -> dict difficulty -> list of functions(rng) -> question|None
    """
    out = []
    for skill, by_diff in generators.items():
        for diff, funcs in by_diff.items():
            made, tries = 0, 0
            while made < per_cell and tries < max_tries:
                tries += 1
                fn = funcs[tries % len(funcs)]                     # Rotate through the templates
                q = fn(factory, factory.rng, skill, diff)
                if q:
                    out.append(q)
                    made += 1
    return out


def write_pack(path, name, source, questions):
    """Writes a content pack JSON file."""
    pack = {"pack": name, "source": source, "licence": "Original content generated for SatPrep (owned by the project)",
            "questions": questions}
    with open(path, "w", encoding="utf-8") as f:
        json.dump(pack, f, ensure_ascii=False, indent=1)
