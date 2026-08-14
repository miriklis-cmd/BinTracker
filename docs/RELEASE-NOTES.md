# BinTracker Current Release Notes

## v0.4.0-alpha.19.12.3

### Customer unsaved-change protection

A full code/doc audit confirmed Customers had **not** yet received the unsaved-change protection previously assumed. The roadmap and Known Issues were correct; the earlier conversational claim was not.

Customers now tracks every editable field and prompts **Save / Discard / Cancel** before changes can be lost through:

- selecting another customer;
- changing Search/filter state;
- starting New Customer;
- navigating to another BinTracker page;
- Logout;
- closing BinTracker.

Save persists first, Discard intentionally leaves without saving, and Cancel keeps the current editor/changes.

### Explicit unsaved-change wording

The shared unsaved-change dialog now uses buttons labelled **Save**, **Discard** and **Cancel**. Container Types uses this dialog instead of ambiguous Yes / No / Cancel wording.

### Import History

The provenance metadata line no longer wraps; Completed remains on the same line and the label ellipsizes only if the window is genuinely too narrow.

### Roadmap/docs audit

Customer dirty-state protection is moved from outstanding to complete. Its Known Issue is removed. The current priority order is now Reports → Dashboard → Email/SMS reminder plumbing → remaining importer failure-detail/cosmetics → packaging/production acceptance. Roadmap, Known Issues, Technical Debt, Test Checklist, Functional Specification, Business Rules, Testing, Master Data and README were reconciled.
