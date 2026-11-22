"""
Builds SatPrep's original content packs.

    python3 tools/contentgen/build_packs.py

Writes into src/SatPrep/Content/:
    packs/gen-math.json      (generated math, all 19 math skills x 3 difficulties)
    packs/gen-rw.json        (generated Reading & Writing: conventions, transitions, words in context, synthesis, data evidence)
    packs/rw-passages.json   (hand-written passage questions, copied from passages.py)
    vocab.json               (vocabulary bank)

Seeds are fixed, so re-running produces identical files (stable question ids
keep learners' history attached to the same questions).
"""
import json
import os
import sys
from collections import Counter

sys.path.insert(0, os.path.dirname(__file__))

from common import QuestionFactory, collect, write_pack       # noqa: E402
import math_gen                                               # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))   # Repository root
CONTENT = os.path.join(ROOT, "src", "SatPrep", "Content")                     # App content folder
PER_CELL = 16                                                                 # Target questions per skill per difficulty


class PackBuilder:
    """Runs every generator and writes the pack files with a summary."""

    def __init__(self):
        self.summary = Counter()   # (skill, difficulty) -> count

    def build_math(self):
        """Generated math pack."""
        f = QuestionFactory(seed=20261122, prefix="gen-math")
        qs = collect(f, math_gen.GENERATORS, PER_CELL)
        self._write("gen-math.json", "gen-math", "SatPrep math generators (computed answers)", qs)

    def build_rw(self):
        """Generated Reading & Writing pack (optional module, added later in the build)."""
        try:
            import rw_gen  # noqa: WPS433
        except ImportError:
            return
        f = QuestionFactory(seed=20261123, prefix="gen-rw")
        qs = collect(f, rw_gen.GENERATORS, PER_CELL)
        self._write("gen-rw.json", "gen-rw", "SatPrep reading & writing generators", qs)

    def build_passages(self):
        """Hand-written passage questions."""
        try:
            import passages  # noqa: WPS433
        except ImportError:
            return
        self._write("rw-passages.json", "rw-passages", "Hand-written original passages for SatPrep", passages.questions())

    def build_vocab(self):
        """Vocabulary bank."""
        try:
            import vocab  # noqa: WPS433
        except ImportError:
            return
        with open(os.path.join(CONTENT, "vocab.json"), "w", encoding="utf-8") as fh:
            json.dump(vocab.words(), fh, ensure_ascii=False, indent=1)
        print(f"vocab.json: {len(vocab.words())} words")

    def _write(self, file, name, source, qs):
        """Writes one pack and records counts."""
        write_pack(os.path.join(CONTENT, "packs", file), name, source, qs)
        for q in qs:
            self.summary[(q["skill"], q["difficulty"])] += 1
        print(f"{file}: {len(qs)} questions")

    def report(self):
        """Prints questions per skill and difficulty."""
        skills = sorted({s for s, _ in self.summary})
        for s in skills:
            e, mdm, h = (self.summary[(s, d)] for d in ("Easy", "Medium", "Hard"))
            print(f"  {s:8} E{e:3} M{mdm:3} H{h:3}")
        print(f"TOTAL {sum(self.summary.values())}")


if __name__ == "__main__":
    b = PackBuilder()
    b.build_math()
    b.build_rw()
    b.build_passages()
    b.build_vocab()
    b.report()
