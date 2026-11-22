# Question packs

Every `*.json` file in this folder is imported when the app starts. Import is
idempotent: questions are matched on `id`, so editing a pack and restarting
updates the questions instead of duplicating them.

## Format

```json
{
  "pack": "short-pack-name",
  "source": "Who wrote it / where it came from",
  "licence": "Licence or written-permission reference",
  "questions": [
    {
      "id": "globally-unique-id",
      "skill": "ALG.LF",
      "difficulty": "Easy | Medium | Hard",
      "format": "MultipleChoice | StudentProduced",
      "passage": "optional text, Markdown + $LaTeX$",
      "stem": "question prompt, Markdown + $LaTeX$",
      "choices": ["A text", "B text", "C text", "D text"],
      "answer": "B   (or for StudentProduced: accepted answers separated by |, e.g. 3/2|1.5)",
      "explanation": "full worked explanation"
    }
  ]
}
```

Skill codes come from `Data/SatTaxonomy.cs` (8 domains, 29 skills).
Rows that are invalid are skipped and logged; they never stop the app.

**Only add content we have the right to use.** See `docs/CONTENT_SOURCES.md`.
