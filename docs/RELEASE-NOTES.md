# BinTracker Current Release Notes

## v0.4.0-alpha.22.3.2

### Application icon / sidebar branding consistency

Fixed BinTracker branding before and after login.

- Added common `BinTrackerForm` base class.
- Login, Main, report breakouts, import/admin dialogs and nested WinForms dialogs now inherit the BinTracker executable icon automatically.
- Login therefore presents the BinTracker icon in the title bar/taskbar before authentication.
- Removed ad-hoc icon loading from Main/Weekly.
- Rebuilt sidebar product branding as a two-column layout so the logo cannot be hidden beneath the BinTracker wordmark.
- Product branding remains separate from future Business Information/customer branding.

### Mandatory full audit

All Markdown/current-state documentation and roadmap coverage were reviewed and reconciled.

### Test requirement

**Full smoke test** because shared Form inheritance and application-shell branding changed.
