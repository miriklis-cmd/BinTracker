# BinTracker v0.4.0-alpha.10.3 — Legacy Buyer Identity / Container Hints

- Fixed legacy Buyer values such as `(Bulk) Clamms` being treated as separate customers.
- Added legacy Buyer parsing:
  - `(Bulk) Clamms` -> customer `Clamms`, container hint `Bulk`
  - `(Y) Barwon` -> customer `Barwon`, container hint `Y`
- Review groups prefixed and unprefixed variants under the same customer identity.
- Review now shows `Container hint(s)` and `Legacy variant(s)` columns.
- Snapshot rows also retain the parsed container hint for the future container-mapping stage.
- Widened Review status so `Existing — match` is fully visible.
- No database import is enabled yet.
