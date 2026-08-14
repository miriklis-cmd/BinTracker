# BinTracker Current Release Notes

## v0.4.0-alpha.19.12.4

### Conversation-to-roadmap reconciliation

This is a documentation/planning-only candidate. No runtime business logic or UI implementation was intentionally changed.

Recovered/clarified requirements include:

- Batch Entry is mostly accepted; remaining work is Esc behaviour, post-entry clear/focus and crash/power-loss draft recovery decision.
- Historical outstanding **as-of-date** reporting.
- Explicit Weekly reporting and quick today/yesterday/current/previous-month periods.
- Customer sorting by outstanding/credit/last movement and lifetime OUT/IN totals.
- Customer Statement generate → view/open → print workflow.
- Dashboard charts/trends.
- Google Workspace email and Texto SMS provider direction.
- Friday-or-earlier reminder policy direction.
- Scheduled automatic production backups.
- PostgreSQL readiness audit before multi-user deployment.
- Post-v1 Customer Portal, barcode scanning and multiple depots.
- Formal manual testing policy.
- Full documentation audit on every meaningful implementation pass.
- Explicit audit-coverage matrix.
- Git/acceptance workflow and truthful build-gate requirements.

### Current execution order

Reports → Batch Entry acceptance cleanup → Dashboard → Email/SMS → normal movement correction/reversal → production backup/recovery → security/audit hardening → PostgreSQL/multi-user/deployment readiness.
