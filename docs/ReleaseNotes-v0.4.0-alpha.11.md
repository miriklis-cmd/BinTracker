# BinTracker v0.4.0-alpha.11 — Normalized Customer Matching

- Added `CustomerNameNormalizer`.
- Customer matching now progresses from exact code to case-insensitive code to conservative spacing/punctuation-normalized matching.
- `S & J`, `S&J` and `(Bulk) S&J` can now resolve to the same customer when the match is unambiguous.
- Ambiguous normalized names are deliberately not auto-matched.
- Added match reasons to Review.
- Added `LegacyContainerHintResolver`.
- Confirmed legacy aliases:
  - `(Y)` = Yellow Bin
  - `(Bulk)` = Bulk Bin
- Container Name and ShortCode can also resolve legacy prefix hints.
- Review shows resolved container names rather than raw `Y`/`Bulk`.
- Reworked Review grid to fit the available width without a horizontal scrollbar at normal sizing.
- Legacy variants remain available as row tooltips instead of consuming a very wide column.
- Import remains disabled.
