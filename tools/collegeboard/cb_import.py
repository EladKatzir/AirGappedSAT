"""
College Board SAT Suite Educator Question Bank -> SatPrep question pack.

    python3 tools/collegeboard/cb_import.py fetch     # download (cached, resumable, polite)
    python3 tools/collegeboard/cb_import.py convert   # build src/SatPrep/Content/packs/collegeboard-sat.json
    python3 tools/collegeboard/cb_import.py all       # both

Uses the same public JSON endpoints the question bank website calls
(satsuiteeducatorquestionbank.collegeboard.org). Run it on a machine WITH
internet; the resulting pack is then carried to the air-gapped server inside
the release (or uploaded on the Content page).

Fetch details:
  * one list request per section (SAT, Reading and Writing / Math)
  * one detail request per question, about 1 per second, cached on disk
    (re-running only downloads what is missing)
  * any <img> pointing to the internet is downloaded and embedded as a data: URI,
    so the questions display fully offline
"""
import base64
import json
import os
import re
import sys
import time
import urllib.request
from html import unescape

API = "https://qbank-api.collegeboard.org/msreportingquestionbank-prod/questionbank"   # Public endpoints used by the site
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))          # Repository root
CACHE = os.path.join(ROOT, "tools", "collegeboard", "cache")                          # Raw downloads (git-ignored)
PACK = os.path.join(ROOT, "src", "SatPrep", "Content", "packs", "collegeboard-sat.json")  # Output pack
SAT_EVENT = 99                                                                        # "SAT" in the site's assessment list
SECTIONS = {1: "INI,CAS,EOI,SEC", 2: "H,P,Q,S"}                                       # test id -> domain codes
DELAY = 0.4                                                                           # Seconds between detail requests
UA = "Mozilla/5.0 (SatPrep offline study importer)"                                   # Polite, identifiable client

# Official skill names (site text, lower-cased) -> SatPrep skill codes (Data/SatTaxonomy.cs)
SKILLS = {
    "central ideas and details": "INI.CID", "inferences": "INI.INF", "command of evidence": "INI.COE",
    "words in context": "CAS.WIC", "text structure and purpose": "CAS.TSP", "cross-text connections": "CAS.CTC",
    "rhetorical synthesis": "EOI.SYN", "transitions": "EOI.TRA",
    "boundaries": "SEC.BOU", "form, structure, and sense": "SEC.FSS",
    "linear equations in one variable": "ALG.LE1", "linear functions": "ALG.LF",
    "linear equations in two variables": "ALG.LE2", "systems of two linear equations in two variables": "ALG.SYS",
    "linear inequalities in one or two variables": "ALG.INE",
    "nonlinear functions": "ADV.NLF", "equivalent expressions": "ADV.EQX",
    "nonlinear equations in one variable and systems of equations in two variables": "ADV.NLE",
    "ratios, rates, proportional relationships, and units": "PSD.RAT", "percentages": "PSD.PCT",
    "one-variable data: distributions and measures of center and spread": "PSD.OVD",
    "two-variable data: models and scatterplots": "PSD.TVD", "probability and conditional probability": "PSD.PRB",
    "inference from sample statistics and margin of error": "PSD.INF",
    "evaluating statistical claims: observational studies and experiments": "PSD.ESC",
    "area and volume": "GEO.AV", "lines, angles, and triangles": "GEO.LAT",
    "right triangles and trigonometry": "GEO.RTT", "circles": "GEO.CIR",
}
DIFFICULTY = {"E": "Easy", "M": "Medium", "H": "Hard"}


class Http:
    """Tiny HTTP helper with retries (standard library only)."""

    @staticmethod
    def post_json(url, body, tries=4):
        """POSTs JSON and returns the parsed reply."""
        data = json.dumps(body).encode()
        for attempt in range(tries):
            try:
                req = urllib.request.Request(url, data=data, headers={"Content-Type": "application/json", "User-Agent": UA})
                with urllib.request.urlopen(req, timeout=60) as r:
                    return json.loads(r.read().decode("utf-8"))
            except Exception as ex:                     # noqa: BLE001 - network errors of any kind
                if attempt == tries - 1:
                    raise
                print(f"    retry {attempt + 1} after error: {ex}")
                time.sleep(3 * (attempt + 1))

    @staticmethod
    def get_bytes(url):
        """Downloads raw bytes (images)."""
        req = urllib.request.Request(url, headers={"User-Agent": UA})
        with urllib.request.urlopen(req, timeout=60) as r:
            return r.read(), r.headers.get_content_type()


class Fetcher:
    """Downloads question lists and details into the cache folder."""

    def __init__(self):
        os.makedirs(os.path.join(CACHE, "items"), exist_ok=True)

    def run(self):
        """Fetches both sections."""
        for test, domains in SECTIONS.items():
            lst = self.fetch_list(test, domains)
            print(f"Section {test}: {len(lst)} questions listed")
            for n, meta in enumerate(lst, 1):
                self.fetch_item(meta, n, len(lst))

    def fetch_list(self, test, domains):
        """Question metadata list for one section (cached)."""
        path = os.path.join(CACHE, f"list-{test}.json")
        if not os.path.exists(path):
            data = Http.post_json(f"{API}/digital/get-questions", {"asmtEventId": SAT_EVENT, "test": test, "domain": domains})
            with open(path, "w", encoding="utf-8") as f:
                json.dump(data, f)
        with open(path, encoding="utf-8") as f:
            return json.load(f)

    def fetch_item(self, meta, n, total):
        """Detail for one question (cached); images embedded."""
        key = meta.get("external_id") or meta.get("ibn")
        if not key:
            return
        path = os.path.join(CACHE, "items", f"{meta['questionId']}.json")
        if os.path.exists(path):
            return
        print(f"  [{n}/{total}] {meta['questionId']}")
        detail = Http.post_json(f"{API}/digital/get-question", {"external_id": key})
        detail = json.loads(Images.embed(json.dumps(detail)))
        with open(path, "w", encoding="utf-8") as f:
            json.dump({"meta": meta, "detail": detail}, f)
        time.sleep(DELAY)


class Images:
    """Replaces remote image URLs with embedded data: URIs."""

    SRC = re.compile(r'src=\\?"(https?://[^"\\]+)\\?"')    # Matches inside JSON-encoded HTML too
    cache = {}                                              # url -> data URI

    @classmethod
    def embed(cls, text):
        """Returns text with every remote image embedded."""
        def repl(m):
            url = m.group(1)
            if url not in cls.cache:
                try:
                    raw, ctype = Http.get_bytes(url)
                    cls.cache[url] = f"data:{ctype};base64,{base64.b64encode(raw).decode()}"
                except Exception as ex:                    # noqa: BLE001
                    print(f"    image failed {url}: {ex}")
                    cls.cache[url] = ""
            quote = '\\"' if m.group(0).startswith('src=\\"') else '"'
            return f"src={quote}{cls.cache[url]}{quote}"
        return cls.SRC.sub(repl, text)


class SprFromRationale:
    """
    Older student-produced items have no answer field; the rationale starts
    "The correct answer is 7" or shows the number as an image whose alt text
    reads e.g. "three halves" / "negative 2". This recovers accepted answers.
    """

    UNITS = {"zero": 0, "one": 1, "two": 2, "three": 3, "four": 4, "five": 5, "six": 6, "seven": 7, "eight": 8, "nine": 9,
             "ten": 10, "eleven": 11, "twelve": 12, "thirteen": 13, "fourteen": 14, "fifteen": 15, "sixteen": 16,
             "seventeen": 17, "eighteen": 18, "nineteen": 19, "twenty": 20, "thirty": 30, "forty": 40, "fifty": 50}
    DENOMS = {"half": 2, "halves": 2, "third": 3, "thirds": 3, "fourth": 4, "fourths": 4, "quarter": 4, "quarters": 4,
              "fifth": 5, "fifths": 5, "sixth": 6, "sixths": 6, "seventh": 7, "sevenths": 7, "eighth": 8, "eighths": 8,
              "ninth": 9, "ninths": 9, "tenth": 10, "tenths": 10, "eleventh": 11, "elevenths": 11, "twelfth": 12, "twelfths": 12}

    @classmethod
    def extract(cls, rationale):
        """Returns 'a|b' accepted answers, or '' when the rationale doesn't state one clearly."""
        html = re.sub(r'<img[^>]*alt="([^"]*)"[^>]*>', lambda m: f" {cls.words_to_number(m.group(1)) or m.group(1)} ", rationale)
        text = unescape(re.sub(r"<[^>]+>", " ", html))
        text = re.sub(r"\s+", " ", text).replace("\u2212", "-").replace("\u2013", "-")
        m = re.search(r"correct answer is (.+?)(?:\. [A-Z]|\.$|$)", text, re.IGNORECASE)
        if not m:
            return ""
        found = re.findall(r"-?\d*\.?\d+(?:/\d+)?", m.group(1).replace(",", ""))
        return "|".join(dict.fromkeys(found[:4]))

    @classmethod
    def words_to_number(cls, alt):
        """'three halves' -> '3/2', 'negative 7' -> '-7', '12' -> '12'; None if not a number phrase."""
        words = alt.lower().replace("-", " ").split()
        if not words:
            return None
        sign = ""
        if words[0] in ("negative", "minus"):
            sign, words = "-", words[1:]
        if len(words) == 1 and re.fullmatch(r"\d*\.?\d+", words[0]):
            return sign + words[0]
        num, i = 0, 0
        while i < len(words) and (words[i] in cls.UNITS or words[i].isdigit()):
            num += cls.UNITS.get(words[i], int(words[i]) if words[i].isdigit() else 0)
            i += 1
        if i == 0:
            return None
        rest = [w for w in words[i:] if w not in ("over", "and")]
        if not rest:
            return f"{sign}{num}"
        if rest[0] in cls.DENOMS:
            return f"{sign}{num}/{cls.DENOMS[rest[0]]}"
        if len(words) > i + 1 and words[i] == "over" and words[i + 1] in cls.UNITS:
            return f"{sign}{num}/{cls.UNITS[words[i + 1]]}"
        return None


class Converter:
    """Turns cached items into a SatPrep content pack."""

    def __init__(self):
        self.problems = []   # Items that could not be converted, with the reason

    def run(self):
        """Converts every cached item and writes the pack."""
        folder = os.path.join(CACHE, "items")
        files = sorted(os.listdir(folder)) if os.path.isdir(folder) else []
        questions = []
        for name in files:
            with open(os.path.join(folder, name), encoding="utf-8") as f:
                raw = json.load(f)
            q = self.convert(raw["meta"], raw["detail"])
            if q:
                questions.append(q)
        pack = {
            "pack": "collegeboard-sat",
            "source": "College Board SAT Suite Educator Question Bank (official SAT questions)",
            "licence": "Owner-authorized personal study use; see docs/CONTENT_SOURCES.md",
            "questions": questions,
        }
        with open(PACK, "w", encoding="utf-8") as f:
            json.dump(pack, f, ensure_ascii=False)
        print(f"Wrote {len(questions)} questions to {PACK} ({len(self.problems)} skipped)")
        for p in self.problems[:20]:
            print("  skipped:", p)

    def convert(self, meta, detail):
        """One cached item -> pack question dict (or None)."""
        qid = meta.get("questionId")
        skill = SKILLS.get((meta.get("skill_desc") or "").strip().lower())
        if not skill:
            self.problems.append(f"{qid}: unknown skill '{meta.get('skill_desc')}'")
            return None
        difficulty = DIFFICULTY.get(meta.get("difficulty"), "Medium")

        item = detail[0] if isinstance(detail, list) and detail else detail     # Some replies are wrapped in a list
        if not isinstance(item, dict):
            self.problems.append(f"{qid}: empty detail")
            return None
        parsed = self.modern(item) if "stem" in item or "answerOptions" in item else self.legacy(item)
        if not parsed:
            self.problems.append(f"{qid}: unrecognised detail format ({', '.join(list(item)[:8])})")
            return None
        passage, stem, choices, answer, rationale = parsed
        if not stem or not answer:
            self.problems.append(f"{qid}: missing stem or answer")
            return None

        q = {
            "id": f"cb-{qid}",
            "skill": skill,
            "difficulty": difficulty,
            "format": "MultipleChoice" if choices else "StudentProduced",
            "html": True,
            "passage": passage or None,
            "stem": stem,
            "answer": answer,
            "explanation": rationale or "",
        }
        if choices:
            if len(choices) != 4:
                self.problems.append(f"{qid}: {len(choices)} choices")
                return None
            q["choices"] = choices
        return q

    @staticmethod
    def modern(item):
        """Current format: stem, stimulus, answerOptions[{id, content}], correct_answer[], rationale."""
        options = item.get("answerOptions") or []
        choices = [o.get("content", "") for o in options]
        correct = item.get("correct_answer") or item.get("keys") or []
        if isinstance(correct, str):
            correct = [correct]
        if choices:
            letter = None
            for c in correct:
                c = str(c).strip()
                if c.upper() in ("A", "B", "C", "D"):
                    letter = c.upper()
                    break
                ids = [str(o.get("id")) for o in options]          # Some items key the answer by option id
                if c in ids:
                    letter = "ABCD"[ids.index(c)]
                    break
            answer = letter
        else:
            answer = "|".join(Converter.clean_spr(c) for c in correct if str(c).strip())
        return item.get("stimulus") or "", item.get("stem") or "", choices, answer, item.get("rationale") or ""

    @staticmethod
    def legacy(item):
        """Older digitized items: prompt/body + answer{choices{a..d}, correct_choice, rationale}."""
        ans = item.get("answer") or {}
        if not isinstance(ans, dict):
            return None
        stem = item.get("prompt") or item.get("body") or ""
        passage = item.get("body") if item.get("prompt") else ""
        raw_choices = ans.get("choices") or {}
        choices = [raw_choices[k].get("body", "") for k in sorted(raw_choices)] if isinstance(raw_choices, dict) else []
        if choices:
            cc = str(ans.get("correct_choice") or "").strip().upper()
            answer = cc if cc in ("A", "B", "C", "D") else None
        else:
            raw = str(ans.get("correct_choice") or "")
            answer = "|".join(Converter.clean_spr(x) for x in re.split(r"[,;]| or ", raw) if x.strip()) if raw.strip() \
                else SprFromRationale.extract(ans.get("rationale") or "")      # Older items state the answer only in the rationale
        return passage, stem, choices, answer, ans.get("rationale") or ""

    @staticmethod
    def clean_spr(value):
        """Plain-text student-produced answer ("<p>3/2</p>" -> "3/2", ".5" kept)."""
        text = unescape(re.sub(r"<[^>]+>", "", str(value))).strip()
        return text.replace(" ", "").replace("\u2212", "-")


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else "all"
    if cmd in ("fetch", "all"):
        Fetcher().run()
    if cmd in ("convert", "all"):
        Converter().run()
