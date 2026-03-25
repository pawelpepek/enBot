---
name: db
description: Query the enBot lingua.db database. Use when the user asks about records, counts, scores, words, or any data stored in the enBot database.
argument-hint: <SQL query>
---

Run the dbquery tool and display the results:

```
cd "E:\Posprzatane\Coding\Moje\enBot\tools\dbquery" && dotnet run -- $ARGUMENTS
```

If no arguments are provided, the tool defaults to showing the 10 most recent records.

The DB path is resolved automatically from `%APPDATA%\enBot\lingua.db` — no hardcoded username.
The tool copies the DB to a temp file before querying, so it works even while enBot is running.
The connection is read-only — INSERT, UPDATE, DELETE and DROP are all rejected.

## Schema

Table: `PromptEntries`
- `Id` — integer primary key
- `Original` — the raw prompt text
- `Corrected` — AI-corrected version with `**bold**` highlights
- `Score` — grammar score 1–10
- `Complexity` — linguistic complexity 1–10
- `WordCount` — word count of the original
- `ExplanationsJson` — JSON array of correction explanations
- `HookVersion` — pipeline version string
- `ReceivedAt` — timestamp

## Example queries

- `SELECT COUNT(*) FROM PromptEntries` — total records
- `SELECT SUM(WordCount) FROM PromptEntries` — total words
- `SELECT * FROM PromptEntries WHERE Id = 301` — inspect a specific record
- `SELECT Id, Score, Complexity, Original FROM PromptEntries ORDER BY Id DESC LIMIT 10` — recent records
- `PRAGMA table_info(PromptEntries)` — show full schema
