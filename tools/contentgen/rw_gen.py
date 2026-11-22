"""
Reading & Writing generators (original content).

Skills covered here:
  SEC.BOU  Boundaries (punctuation between clauses, intro elements, lists, nonessential elements)
  SEC.FSS  Form, Structure, and Sense (agreement, pronouns, tense, possessives)
  EOI.TRA  Transitions (logical relationship between sentences)
  EOI.SYN  Rhetorical Synthesis (use notes to meet a writing goal)
  CAS.WIC  Words in Context (from the vocabulary bank)
  INI.COE  Command of Evidence - quantitative (use a table to support a claim)

The other R&W skills need real passages and are hand-written in passages.py.
"""
import re

from vocab import WORDS

# =============================================================================
# SEC.BOU - Boundaries
# =============================================================================

# Two independent clauses; the second starts with a pronoun or noun so a comma splice is clearly wrong.
CLAUSE_PAIRS = [
    ("Honeybees communicate the location of food through a pattern of movements called the waggle dance", "the angle of the dance shows the direction of the flowers"),
    ("The city's first public library opened in 1854", "it held only a few thousand books at the time"),
    ("Octopuses can change the color of their skin in less than a second", "they use this ability to hide from predators"),
    ("The architect designed the museum around a central courtyard", "this open space fills the galleries with natural light"),
    ("Marie Tharp spent years plotting depth measurements of the Atlantic floor", "her maps revealed a vast underwater mountain range"),
    ("The desert receives less than ten centimeters of rain each year", "many of its plants store water in thick leaves"),
    ("The orchestra rehearsed the symphony for six weeks", "the musicians wanted every passage to be perfect"),
    ("Early telescopes produced blurry, colored images", "lens makers later learned to combine different types of glass"),
    ("The novel was rejected by eleven publishers", "it went on to sell millions of copies"),
    ("Sea otters wrap themselves in kelp while they sleep", "the kelp keeps them from drifting out to sea"),
    ("The team tested the bridge design in a wind tunnel", "the model swayed dangerously at high speeds"),
    ("Coffee plants grow best at high altitudes near the equator", "the cool nights slow the ripening of the beans"),
    ("The poet wrote most of her work in a small attic room", "she claimed the silence helped her think"),
    ("Glaciers move slowly downhill under their own weight", "some advance only a few centimeters a day"),
    ("The festival began as a small gathering of local farmers", "it now attracts visitors from around the world"),
    ("Arctic terns migrate between the poles each year", "their journey is the longest of any known animal"),
    ("The engineer replaced the steel beams with lighter aluminum ones", "the change reduced the aircraft's weight by twelve percent"),
    ("Many ancient Roman roads are still in use today", "their layered construction made them remarkably durable"),
    ("The researchers collected soil samples from forty farms", "each sample was tested for nitrogen and phosphorus"),
    ("The painter rarely used the color blue", "she preferred warm shades of red and orange"),
    ("Tardigrades can survive extreme cold and heat", "scientists have even exposed them to the vacuum of space"),
    ("The first transatlantic telegraph cable failed after three weeks", "a stronger cable was laid eight years later"),
]

# Introductory clause/phrase + main clause (needs a comma after the intro).
INTRO_SENTENCES = [
    ("Although the experiment failed at first", "the researchers learned valuable lessons from it"),
    ("After the storm passed", "volunteers began clearing the fallen branches"),
    ("Because the river had risen overnight", "the ferry did not run the next morning"),
    ("When the composer finished the final movement", "he sent the score to the orchestra"),
    ("Unlike most birds", "penguins cannot fly"),
    ("Despite its small size", "the hummingbird can fly hundreds of kilometers without stopping"),
    ("Before the invention of the printing press", "books were copied by hand"),
    ("In the early twentieth century", "many families in the region worked on farms"),
    ("While studying the old maps", "the historian noticed a village that no longer exists"),
    ("Since the bridge reopened", "traffic downtown has decreased"),
    ("As the sun set over the valley", "the temperature dropped sharply"),
    ("To protect the fragile pages", "the archivist wore cotton gloves"),
    ("If the seeds are planted too deep", "they may never reach the surface"),
    ("Once the clay had dried", "the potter fired the bowls in the kiln"),
    ("Throughout her long career", "the journalist interviewed hundreds of scientists"),
    ("By the end of the season", "the team had won twelve straight games"),
    ("Hoping to attract more visitors", "the museum extended its weekend hours"),
    ("During the eighteen-month expedition", "the crew recorded thousands of new species"),
]

# Sentence ending in a list introduced after a complete clause (colon needed).
LIST_SENTENCES = [
    ("The recipe calls for three main ingredients", "flour, butter, and sugar"),
    ("The exhibit features work by four painters", "Frida Kahlo, Diego Rivera, Rufino Tamayo, and Leonora Carrington"),
    ("The survey asked about two habits", "sleep and exercise"),
    ("The region is known for three crops", "rice, tea, and citrus fruit"),
    ("The course covers the major branches of science", "physics, chemistry, biology, and earth science"),
    ("The expedition carried only the essentials", "water, food, rope, and a radio"),
    ("The band's sound blends several traditions", "jazz, folk, and West African music"),
    ("The city has three official languages", "English, French, and Spanish"),
    ("The museum's oldest objects come from two sites", "a cave in France and a riverbank in Kenya"),
    ("The festival offers something for every taste", "concerts, films, food stalls, and workshops"),
    ("The report identified two main causes of the delay", "bad weather and supply shortages"),
    ("The garden is divided into four sections", "herbs, vegetables, flowers, and fruit trees"),
    ("The scientist studied three species of frog", "the tree frog, the bullfrog, and the poison dart frog"),
    ("The athlete trained in two sports", "swimming and cycling"),
    ("The library offers three services to residents", "book loans, computer access, and language classes"),
    ("The town was rebuilt using local materials", "stone, timber, and clay"),
]

# Subject, nonessential appositive (opened with a comma), rest of sentence.
APPOSITIVES = [
    ("Ada Lovelace", "a nineteenth-century mathematician", "wrote what many consider the first computer program"),
    ("The okapi", "a relative of the giraffe", "lives in the rainforests of central Africa"),
    ("Chinua Achebe", "the author of Things Fall Apart", "taught at several universities in the United States"),
    ("The Great Barrier Reef", "the largest coral reef system in the world", "can be seen from space"),
    ("Mount Kilimanjaro", "Africa's tallest peak", "is a dormant volcano"),
    ("The axolotl", "a salamander native to Mexico", "can regrow entire limbs"),
    ("Hypatia", "a scholar in ancient Alexandria", "taught mathematics and astronomy"),
    ("The violin", "an instrument with four strings", "developed in Italy in the sixteenth century"),
    ("Machu Picchu", "a fifteenth-century Inca citadel", "sits high in the Andes Mountains"),
    ("The platypus", "one of the few mammals that lay eggs", "is found only in eastern Australia"),
    ("Katherine Johnson", "a mathematician at NASA", "calculated flight paths for early space missions"),
    ("The Sahara", "the largest hot desert on Earth", "covers much of North Africa"),
    ("Basalt", "a dark volcanic rock", "forms when lava cools quickly"),
    ("The cello", "a member of the violin family", "has a deep, warm tone"),
    ("Wangari Maathai", "a Kenyan environmental activist", "founded a movement that planted millions of trees"),
    ("The tuatara", "a reptile found only in New Zealand", "has existed for more than 200 million years"),
    ("Gabriel García Márquez", "a Colombian novelist", "won the Nobel Prize in Literature in 1982"),
]


def last_word(clause):
    """Splits a clause into (text before last word, last word)."""
    head, _, word = clause.rpartition(" ")
    return head, word


def bou_easy(f, r, skill, diff):
    """Comma after an introductory element, or colon before a list."""
    if r.random() < 0.5:
        intro, main = r.choice(INTRO_SENTENCES)
        head, w = last_word(intro)
        passage = f"{head} ______ {main}."
        correct = f"{w},"
        expl = (f"\"{intro}\" is an introductory element that cannot stand alone as a sentence, so it is joined to the main clause with a comma. "
                f"A semicolon or period would leave the introductory element as a fragment, and a colon must follow a complete clause.")
        return f.mc(skill, diff, "Which choice completes the text so that it conforms to the conventions of Standard English?",
                    correct, [f"{w};", f"{w}.", f"{w}:"], expl, passage=passage)
    clause, items = r.choice(LIST_SENTENCES)
    head, w = last_word(clause)
    passage = f"{head} ______ {items}."
    expl = (f"\"{clause}\" is a complete independent clause, and what follows is a list that explains it. A colon is used to introduce "
            f"such a list. A semicolon must join two independent clauses, and a comma or no punctuation runs the list into the clause unclearly.")
    return f.mc(skill, diff, "Which choice completes the text so that it conforms to the conventions of Standard English?",
                f"{w}:", [f"{w};", f"{w},", w], expl, passage=passage)


def bou_medium(f, r, skill, diff):
    """Joining two independent clauses."""
    a, b = r.choice(CLAUSE_PAIRS)
    head, w = last_word(a)
    passage = f"{head} ______ {b}."
    expl = (f"Both \"{a}\" and \"{b}\" are independent clauses. Two independent clauses can be joined with a semicolon. "
            f"A comma alone creates a comma splice, no punctuation creates a run-on sentence, and \"which\" makes the second part ungrammatical.")
    return f.mc(skill, diff, "Which choice completes the text so that it conforms to the conventions of Standard English?",
                f"{w};", [f"{w},", w, f"{w}, which"], expl, passage=passage)


def bou_hard(f, r, skill, diff):
    """Closing a nonessential element with matching punctuation."""
    subj, appos, rest = r.choice(APPOSITIVES)
    head, w = last_word(appos)
    style = r.choice(["comma", "dash"])
    open_p, close_p = (",", ",") if style == "comma" else (" —", " —")
    passage = f"{subj}{open_p} {head} ______ {rest}."
    correct = f"{w}{close_p.strip() if style == 'comma' else ' —'}".replace(" —", "—") if style == "dash" else f"{w},"
    passage = passage.replace(" — ", "—") if style == "dash" else passage
    if style == "dash":
        passage = f"{subj}—{head} ______ {rest}."
        correct = f"{w}—"
        wrong = [f"{w},", f"{w};", w]
    else:
        wrong = [f"{w}—", f"{w};", w]
    expl = (f"\"{appos}\" is extra (nonessential) information about {subj}. A nonessential element must be set off with matching "
            f"punctuation on both sides: since it opens with a {'comma' if style == 'comma' else 'dash'}, it must close with a "
            f"{'comma' if style == 'comma' else 'dash'}. A semicolon would separate the subject from its verb.")
    return f.mc(skill, diff, "Which choice completes the text so that it conforms to the conventions of Standard English?",
                correct, wrong, expl, passage=passage)


# =============================================================================
# SEC.FSS - Form, Structure, and Sense
# =============================================================================

# (singular subject, plural subject, intervening phrase, rest of sentence in present tense)
AGREEMENT = [
    ("The collection", "The collections", "of rare manuscripts in the university's archives", "available to visiting scholars today"),
    ("The list", "The lists", "of ingredients printed on the side of each package", "required by law"),
    ("The flock", "The flocks", "of geese that spend the winter near the lake", "a common sight in December"),
    ("The study", "The studies", "of sleep patterns among teenagers", "now cited in many health guides"),
    ("The painting", "The paintings", "by the artists in the new exhibit", "currently on loan from a private collector"),
    ("The bridge", "The bridges", "across the wide northern river", "now open to bicycle traffic"),
    ("The report", "The reports", "about the effects of the drought on local farms", "posted on the city's website"),
    ("The array", "The arrays", "of solar panels on the school's roof", "now producing more power than the building uses"),
    ("The box", "The boxes", "of photographs found in the attic", "currently being scanned by volunteers"),
    ("The map", "The maps", "of the old trade routes across the desert", "displayed in the main hall"),
    ("The colony", "The colonies", "of ants living beneath the playground", "surprisingly well organized"),
    ("The set", "The sets", "of tools used by the early printers", "kept in a glass case"),
]

# (sentence before the blank, singular antecedent?, sentence after) for pronoun agreement.
PRONOUNS = [
    ("The city council announced that", True, "would vote on the new park next month."),
    ("The researchers published", False, "findings in a respected journal."),
    ("After months of debate, the committee finally released", True, "recommendations."),
    ("The two companies merged and combined", False, "research teams."),
    ("The museum reopened after renovating", True, "east wing."),
    ("The volunteers were thanked for", False, "hard work during the flood."),
    ("The orchestra will begin", True, "summer tour in June."),
    ("Farmers in the valley rotate", False, "crops to keep the soil healthy."),
    ("The company updated", True, "safety rules after the accident."),
    ("The students presented", False, "projects at the science fair."),
    ("The species protects", True, "young by hiding them in burrows."),
    ("The hikers checked", False, "maps before leaving the trail."),
]

# (year phrase, subject, base verb, past form, rest)
TENSE = [
    ("In 1928,", "Alexander Fleming", "notice", "noticed", "that mold had killed bacteria in one of his dishes."),
    ("In 1953,", "Rosalind Franklin's X-ray images", "help", "helped", "scientists determine the structure of DNA."),
    ("In 1869,", "workers", "complete", "completed", "the first railroad across North America."),
    ("In 1903,", "the Wright brothers", "make", "made", "their first powered flight."),
    ("In 1969,", "astronauts", "walk", "walked", "on the Moon for the first time."),
    ("In 1847,", "the composer", "write", "wrote", "his most famous piano sonata."),
    ("In 1912,", "the geologist Alfred Wegener", "propose", "proposed", "that the continents had once been joined."),
    ("In 1990,", "engineers", "launch", "launched", "the Hubble Space Telescope."),
    ("In 1665,", "Robert Hooke", "publish", "published", "detailed drawings of what he saw through a microscope."),
    ("Last spring,", "the town", "plant", "planted", "two hundred trees along the river."),
    ("In 1955,", "Jonas Salk's team", "announce", "announced", "that their polio vaccine was safe."),
    ("In 1922,", "archaeologists", "discover", "discovered", "the tomb of Tutankhamun."),
]


PLURAL_SUBJECTS = {"Rosalind Franklin's X-ray images", "workers", "the Wright brothers", "astronauts", "engineers", "archaeologists"}
PARTICIPLES = {"write": "written", "make": "made"}  # Irregular past participles (others equal the past form)


def third(verb):
    """Present tense third-person singular of a simple verb."""
    if verb.endswith(("s", "sh", "ch", "x")):
        return verb + "es"
    if verb.endswith("y") and verb[-2] not in "aeiou":
        return verb[:-1] + "ies"
    return verb + "s"


def fss_easy(f, r, skill, diff):
    """Verb tense with a past time marker, or possessive nouns."""
    if r.random() < 0.6:
        when, subj, base, past, rest = r.choice(TENSE)
        plural = subj in PLURAL_SUBJECTS                               # Drives present-tense and "has/have" forms
        present = base if plural else third(base)
        perfect = f"{'have' if plural else 'has'} {PARTICIPLES.get(base, past)}"
        passage = f"{when} {subj} ______ {rest}"
        expl = (f"The phrase \"{when.rstrip(',')}\" places the action at a specific time in the past, so the simple past tense \"{past}\" is correct. "
                f"The present (\"{present}\"), future (\"will {base}\"), and present perfect (\"{perfect}\") forms do not fit a completed event at a stated past time.")
        return f.mc(skill, diff, "Which choice completes the text so that it conforms to the conventions of Standard English?",
                    past, [present, f"will {base}", perfect], expl, passage=passage)
    animal = r.choice([("bird", "wings"), ("tree", "roots"), ("student", "notebooks"), ("bee", "stingers"), ("player", "jerseys"), ("fox", "dens")])
    sing = r.random() < 0.5
    noun = animal[0]
    plural_noun = noun + ("es" if noun.endswith("x") else "s")
    if sing:
        passage = f"Researchers measured each ______ {animal[1]} at the start of the study."
        correct, wrong = f"{noun}'s", [f"{plural_noun}'", plural_noun, f"{plural_noun}'s"]
        expl = f"\"Each\" refers to one {noun}, and the {animal[1]} belong to it, so the singular possessive \"{noun}'s\" is needed."
    else:
        passage = f"Researchers measured all of the ______ {animal[1]} at the start of the study."
        correct, wrong = f"{plural_noun}'", [f"{noun}'s", plural_noun, f"{plural_noun}'s"]
        expl = f"\"All of the\" refers to many {plural_noun}, and the {animal[1]} belong to them, so the plural possessive \"{plural_noun}'\" is needed."
    return f.mc(skill, diff, "Which choice completes the text so that it conforms to the conventions of Standard English?",
                correct, wrong, expl, passage=passage)


def fss_medium(f, r, skill, diff):
    """Pronoun-antecedent agreement."""
    before, sing, after = r.choice(PRONOUNS)
    if after.startswith("would"):
        correct = "it" if sing else "they"
        wrong = ["they" if sing else "it", "its", "their"]
        expl = (f"The pronoun refers back to the subject of the sentence, which is {'singular' if sing else 'plural'}, so "
                f"\"{correct}\" is needed. Possessive forms do not fit before a verb.")
    else:
        correct = "its" if sing else "their"
        wrong = ["their" if sing else "its", "it's", "they're"]
        expl = (f"The blank shows possession by the subject, which is {'singular (a single group or thing)' if sing else 'plural'}, "
                f"so the {'singular' if sing else 'plural'} possessive \"{correct}\" is correct. \"It's\" means \"it is\" and \"they're\" means \"they are.\"")
    passage = f"{before} ______ {after}"
    return f.mc(skill, diff, "Which choice completes the text so that it conforms to the conventions of Standard English?",
                correct, wrong, expl, passage=passage)


def fss_hard(f, r, skill, diff):
    """Subject-verb agreement across an intervening phrase."""
    sg, pl, phrase, rest = r.choice(AGREEMENT)
    singular = r.random() < 0.5
    subj = sg if singular else pl
    passage = f"{subj} {phrase} ______ {rest}."
    correct = "is" if singular else "are"
    wrong = ["are", "were", "have been"] if singular else ["is", "was", "has been"]
    noun = subj.split()[-1]
    expl = (f"The subject is \"{subj}\" ({'singular' if singular else 'plural'}); the phrase \"{phrase}\" only describes it. "
            f"The verb must agree with \"{noun}\", and the present-tense context calls for \"{correct}\".")
    return f.mc(skill, diff, "Which choice completes the text so that it conforms to the conventions of Standard English?",
                correct, wrong, expl, passage=passage)


# =============================================================================
# EOI.TRA - Transitions
# =============================================================================

TRANSITIONS = {
    "contrast": ["However,", "Nevertheless,", "Still,", "By contrast,"],
    "addition": ["Moreover,", "Additionally,", "Furthermore,", "In addition,"],
    "result": ["Therefore,", "As a result,", "Consequently,", "Thus,"],
    "example": ["For example,", "For instance,"],
    "sequence": ["Later,", "Afterward,", "Finally,", "Next,"],
    "similarity": ["Similarly,", "Likewise,"],
}

# (first sentence(s), second sentence without transition, relationship, difficulty)
TRA_ITEMS = [
    ("Most spiders spin webs to catch their prey.", "jumping spiders hunt by stalking and pouncing on insects.", "contrast", "Easy"),
    ("The new bus line cut commuting times in half.", "it reduced traffic on the city's main avenue.", "addition", "Medium"),
    ("The bakery's oven broke on Friday morning.", "no bread was available that weekend.", "result", "Easy"),
    ("Many common foods were first cultivated in the Americas.", "tomatoes, potatoes, and corn all originated there.", "example", "Easy"),
    ("The scientist first dried the leaf samples in an oven.", "she ground them into a fine powder for testing.", "sequence", "Easy"),
    ("Bats use echolocation to find insects in the dark.", "dolphins use echoes to locate fish in murky water.", "similarity", "Medium"),
    ("The critics praised the film's stunning photography.", "audiences stayed away, and it earned little at the box office.", "contrast", "Medium"),
    ("The soil in the valley is rich in minerals.", "farmers there produce some of the region's largest harvests.", "result", "Medium"),
    ("The museum offers free guided tours every morning.", "it hosts lectures by visiting historians each month.", "addition", "Easy"),
    ("Some birds are remarkably good at solving puzzles.", "crows have been observed bending wire into hooks to retrieve food.", "example", "Medium"),
    ("The first version of the engine overheated after a few minutes.", "engineers redesigned the cooling system, and the problem disappeared.", "sequence", "Medium"),
    ("In Japan, bowing is a common way to greet someone.", "in Thailand people often greet each other with a slight bow and pressed palms.", "similarity", "Hard"),
    ("The author's early novels were long and full of detail.", "her later work is spare, with short chapters and little description.", "contrast", "Medium"),
    ("Volunteers removed more than two tons of trash from the beach.", "nesting sea turtles returned to the area the following year.", "result", "Hard"),
    ("Regular exercise strengthens the heart.", "it has been shown to improve mood and sleep.", "addition", "Medium"),
    ("Several inventions were discovered by accident.", "the microwave oven was developed after an engineer noticed a candy bar melting near radar equipment.", "example", "Medium"),
    ("The team had trained hard all season and was favored to win.", "it lost the championship game by a single point.", "contrast", "Easy"),
    ("The river's water level dropped sharply during the drought.", "cargo ships were unable to travel upstream for weeks.", "result", "Easy"),
    ("Researchers first mapped the cave using laser scanners.", "they used the map to plan a safe route for divers.", "sequence", "Medium"),
    ("Monarch butterflies travel thousands of kilometers each fall.", "some dragonflies migrate across entire oceans.", "similarity", "Hard"),
    ("Solar panels have become far cheaper over the past decade.", "they now last longer and convert more sunlight into electricity.", "addition", "Hard"),
    ("The theory was widely accepted for nearly a century.", "new evidence from deep-sea drilling showed it to be incomplete.", "contrast", "Medium"),
    ("The city added protected bike lanes on twelve streets.", "the number of residents cycling to work doubled within two years.", "result", "Medium"),
    ("Many plants have developed clever ways to spread their seeds.", "dandelion seeds have tiny parachutes that carry them on the wind.", "example", "Easy"),
    ("The violinist practiced the difficult passage for months.", "she performed it flawlessly at the competition.", "sequence", "Hard"),
    ("Ancient Egyptians used papyrus reeds to make writing material.", "people in ancient China made paper from mulberry bark and hemp.", "similarity", "Hard"),
    ("Most metals expand when heated.", "water expands when it freezes, which is why ice floats.", "contrast", "Hard"),
    ("The company switched to recycled packaging.", "it reduced its plastic waste by forty percent in one year.", "result", "Medium"),
    ("The park offers hiking trails for all skill levels.", "it has campsites, a lake for swimming, and a visitor center.", "addition", "Easy"),
    ("Some animals change color with the seasons.", "the Arctic fox turns from brown in summer to white in winter.", "example", "Medium"),
    ("The playwright's first drafts were often very long.", "she cut them down to about half their original length.", "sequence", "Medium"),
    ("Honeybees perform dances to share the location of flowers.", "some ants leave chemical trails to guide nestmates to food.", "similarity", "Medium"),
    ("Early critics dismissed the painter's work as unfinished and crude.", "today her canvases hang in major museums around the world.", "contrast", "Hard"),
    ("The library extended its hours on weekdays.", "more students began using it to study in the evenings.", "result", "Hard"),
    ("The new vaccine is highly effective.", "it can be stored at ordinary refrigerator temperatures, making it easy to distribute.", "addition", "Hard"),
    ("Many common words in English came from other languages.", "\"ketchup\" likely comes from a word used in southern China.", "example", "Hard"),
]

RELATION_NAMES = {"contrast": "a contrast", "addition": "an additional point", "result": "a result",
                  "example": "an example", "sequence": "the next step in a sequence", "similarity": "a similar case"}


def tra_any(f, r, skill, diff):
    """Choose the logical transition."""
    items = [t for t in TRA_ITEMS if t[3] == diff]
    s1, s2, rel, _ = r.choice(items)
    correct = r.choice(TRANSITIONS[rel])
    others = [k for k in TRANSITIONS if k != rel and not (rel == "addition" and k == "similarity") and not (rel == "similarity" and k == "addition")]
    r.shuffle(others)
    wrong = [r.choice(TRANSITIONS[k]) for k in others[:3]]
    passage = f"{s1} ______ {s2}"
    expl = (f"The second sentence presents {RELATION_NAMES[rel]} in relation to the first, so \"{correct}\" is the logical transition. "
            f"The other choices signal a different relationship ({', '.join(w.rstrip(',') for w in wrong)}).")
    return f.mc(skill, diff, "Which choice completes the text with the most logical transition?", correct, wrong, expl, passage=passage)


# =============================================================================
# EOI.SYN - Rhetorical Synthesis
# =============================================================================

# Two subjects with comparable attributes (attribute name -> (value A, value B)).
SYN_SETS = [
    ("the Hubble Space Telescope", "the James Webb Space Telescope", "telescope",
     {"launch year": ("1990", "2021"), "light observed": ("mainly visible and ultraviolet light", "mainly infrared light"),
      "location": ("orbits about 540 kilometers above Earth", "orbits about 1.5 million kilometers from Earth")}),
    ("the African elephant", "the Asian elephant", "elephant",
     {"ear size": ("has large, fan-shaped ears", "has smaller, rounded ears"), "tusks": ("both males and females usually have tusks", "usually only males have tusks"),
      "habitat": ("lives in savannas and forests in Africa", "lives in forests in South and Southeast Asia")}),
    ("Venus", "Mars", "planet",
     {"surface temperature": ("averages about 465 degrees Celsius", "averages about minus 60 degrees Celsius"),
      "atmosphere": ("has a thick atmosphere of carbon dioxide", "has a thin atmosphere of carbon dioxide"),
      "day length": ("rotates once every 243 Earth days", "rotates once every 24.6 hours")}),
    ("the violin", "the cello", "instrument",
     {"size": ("is held under the chin", "rests on the floor between the player's knees"), "range": ("plays high notes", "plays low notes"),
      "strings": ("has four strings", "has four strings")}),
    ("the Amazon River", "the Nile River", "river",
     {"length": ("is about 6,400 kilometers long", "is about 6,650 kilometers long"), "discharge": ("carries more water than any other river", "carries far less water than the Amazon"),
      "direction": ("flows east into the Atlantic Ocean", "flows north into the Mediterranean Sea")}),
    ("the novel Frankenstein", "the novel Dracula", "novel",
     {"author": ("was written by Mary Shelley", "was written by Bram Stoker"), "year": ("was published in 1818", "was published in 1897"),
      "form": ("is told partly through letters", "is told through letters and diary entries")}),
    ("honeybees", "bumblebees", "bee",
     {"colony size": ("live in colonies of tens of thousands", "live in colonies of a few hundred"), "honey": ("store large amounts of honey", "store only small amounts of honey"),
      "pollination": ("pollinate many crops", "pollinate many crops")}),
    ("the Eiffel Tower", "the Statue of Liberty", "monument",
     {"completion": ("was completed in 1889", "was completed in 1886"), "material": ("is made of wrought iron", "has a copper skin over an iron frame"),
      "designer": ("was designed by Gustave Eiffel's company", "had an internal frame designed by Gustave Eiffel's company")}),
]


def syn_any(f, r, skill, diff):
    """Use notes to emphasize a difference or similarity."""
    a, b, kind, attrs = r.choice(SYN_SETS)
    keys = list(attrs)
    same = [k for k in keys if attrs[k][0] == attrs[k][1]]
    diff_keys = [k for k in keys if attrs[k][0] != attrs[k][1]]
    notes = []
    for k in keys:
        notes.append(f"{a[0].upper() + a[1:]} {attrs[k][0]}.")
        notes.append(f"{b[0].upper() + b[1:]} {attrs[k][1]}.")
    notes_md = "While researching a topic, a student has taken the following notes:\n\n" + "\n".join(f"- {n}" for n in notes)
    cap = lambda s: s[0].upper() + s[1:]
    if diff == "Hard" and same:
        k = r.choice(same)
        other = r.choice(diff_keys)
        goal = f"emphasize a similarity between {a} and {b}"
        correct = f"Both {a} and {b} share a feature: each {attrs[k][0].replace('has ', 'has ').replace('pollinate', 'pollinates')}."
        correct = f"Like {a}, {b} {attrs[k][1]}."
        wrong = [f"While {a} {attrs[other][0]}, {b} {attrs[other][1]}.", f"{cap(a)} {attrs[other][0]}.", f"{cap(b)} {attrs[other][1]}."]
        expl = f"Only the correct choice states a feature the two have in common ({k}). The other choices describe differences or only one subject."
    else:
        k = r.choice(diff_keys)
        other = r.choice([x for x in keys if x != k])
        goal = f"emphasize a difference in {k} between {a} and {b}"
        correct = f"While {a} {attrs[k][0]}, {b} {attrs[k][1]}."
        wrong = [f"{cap(a)} {attrs[k][0]}.", f"While {a} {attrs[other][0]}, {b} {attrs[other][1]}." if attrs[other][0] != attrs[other][1] else f"{cap(b)} {attrs[other][1]}.",
                 f"{cap(a)} and {b} are both well known."]
        expl = (f"The goal is to contrast {k}. The correct choice mentions both subjects and the {k} of each. The others describe only one subject, "
                f"compare a different feature, or make no comparison.")
    stem = f"The student wants to {goal}. Which choice most effectively uses relevant information from the notes to accomplish this goal?"
    return f.mc(skill, diff, stem, correct, wrong, expl, passage=notes_md)


# =============================================================================
# CAS.WIC - Words in Context (from the vocabulary bank)
# =============================================================================

def wic_any(f, r, skill, diff):
    """Blank a vocab word in its example sentence; distractors are other words of the same part of speech."""
    tier = {"Easy": 1, "Medium": 2, "Hard": 3}[diff]
    pool = [w for w in WORDS if w[4] == tier]
    word, pos, definition, example, _ = r.choice(pool)
    pattern = re.compile(rf"\b{re.escape(word)}\b", re.IGNORECASE)
    if not pattern.search(example):
        return None
    passage = pattern.sub("______", example, count=1)
    same_pos = [w for w in WORDS if w[1] == pos and w[0] != word and abs(w[4] - tier) <= 1]
    if len(same_pos) < 3:
        return None
    wrong = [w[0] for w in r.sample(same_pos, 3)]
    expl = (f"\"{word}\" means \"{definition},\" which fits the context: {example} "
            f"The other words ({', '.join(wrong)}) do not make sense in this sentence.")
    return f.mc(skill, diff, "Which choice completes the text with the most logical and precise word or phrase?",
                word, wrong, expl, passage=passage)


# =============================================================================
# INI.COE - Command of Evidence (quantitative)
# =============================================================================

COE_SETS = [
    ("bird species", "Number of birds counted", ["Robins", "Sparrows", "Finches", "Wrens"], "ecologist", "species", "birds"),
    ("cities", "Number of public charging stations", ["Lakeview", "Port Ellis", "Granton", "Riverside"], "urban planner", "city", "stations"),
    ("crops", "Harvest (tons)", ["Wheat", "Barley", "Oats", "Rye"], "agricultural scientist", "crop", "tons"),
    ("libraries", "Books borrowed (thousands)", ["Central", "Eastside", "Hillcrest", "Northgate"], "librarian", "library", "thousand books"),
    ("schools", "Students in the robotics club", ["Adams High", "Baker High", "Carver High", "Dalton High"], "teacher", "school", "students"),
]


def coe_any(f, r, skill, diff):
    """Which choice uses data from the table to support the claim about the greatest increase?"""
    kind, measure, names, person, single, unit = r.choice(COE_SETS)
    article = "An" if person[0] in "aeiou" else "A"                  # "An ecologist" / "A teacher"
    y1, y2 = r.choice([(2015, 2020), (2018, 2023), (2010, 2020)])
    before = [r.randint(20, 90) for _ in names]
    gains = r.sample(range(4, 40), len(names))                     # Distinct positive gains (no ties)
    after = [b + g for b, g in zip(before, gains)]
    best = max(range(len(names)), key=lambda i: gains[i])
    other = r.choice([i for i in range(len(names)) if i != best])
    highest_after = max(range(len(names)), key=lambda i: after[i])
    table = f"| {single.capitalize()} | {y1} | {y2} |\n|---|---|---|\n" + "\n".join(
        f"| {n} | {b} | {a} |" for n, b, a in zip(names, before, after))
    passage = (f"{measure} for four {kind}:\n\n{table}\n\n{article} {person} claims that, of the four {kind}, {names[best]} showed the greatest "
               f"increase in {measure.lower()} between {y1} and {y2}.")
    correct = (f"{names[best][0].upper() + names[best][1:]} increased from {before[best]} to {after[best]}, a gain of {gains[best]}, "
               f"which is larger than the gain for any other {single}.")
    wrong = [
        f"{names[other][0].upper() + names[other][1:]} increased from {before[other]} to {after[other]}.",
        f"{names[highest_after][0].upper() + names[highest_after][1:]} had the highest value in {y2}, at {after[highest_after]}." if highest_after != best
        else f"{names[best][0].upper() + names[best][1:]} had the highest value in {y2}, at {after[best]}.",
        f"All four {kind} had higher values in {y2} than in {y1}.",
    ]
    expl = (f"The claim is about the size of the increase, so the evidence must compare gains. {names[best]} gained {gains[best]}, more than "
            f"any other ({', '.join(f'{n}: {g}' for n, g in zip(names, gains) if n != names[best])}). The other choices are accurate or "
            f"partly accurate but do not show that {names[best]} had the greatest increase.")
    return f.mc(skill, diff, "Which choice most effectively uses data from the table to support the claim?", correct, wrong, expl, passage=passage)


# =============================================================================
# Registry
# =============================================================================

GENERATORS = {
    "SEC.BOU": {"Easy": [bou_easy], "Medium": [bou_medium], "Hard": [bou_hard]},
    "SEC.FSS": {"Easy": [fss_easy], "Medium": [fss_medium], "Hard": [fss_hard]},
    "EOI.TRA": {"Easy": [tra_any], "Medium": [tra_any], "Hard": [tra_any]},
    "EOI.SYN": {"Medium": [syn_any], "Hard": [syn_any]},
    "CAS.WIC": {"Easy": [wic_any], "Medium": [wic_any], "Hard": [wic_any]},
    "INI.COE": {"Medium": [coe_any], "Hard": [coe_any]},
}
