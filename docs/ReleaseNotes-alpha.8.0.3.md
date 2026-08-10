# BinTracker v0.2.0-alpha.8.0.3 — Single Entry Reset

After a successful Single Entry save, the entire entry form now resets:

- Movement date -> today
- Direction -> Returned (IN)
- Customer code -> blank
- Resolved customer summary -> blank
- Container type -> first available type
- Quantity -> blank
- Reference -> blank
- Notes -> blank
- Customer-position preview -> cleared
- Focus -> Customer code

The successful save status remains visible so the operator still has confirmation that the previous movement was accepted.
