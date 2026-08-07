# BinTracker v0.2.0-alpha.6.2 — Security Polish

## Password management
- Users can change their own passwords from Settings > My Profile.
- Current password is required.
- New password must be different and meet the password policy.
- Password changes are audited; passwords are never written to the audit trail.
- Administrators can reset another user's password.
- Reset passwords are temporary and force a password change on the next login.
- New non-initial users also change their administrator-supplied initial password at first login.
- No password expiry is implemented.

## Account lockout
- Five failed password attempts lock an account by default.
- Locked accounts cannot log in until an administrator unlocks them.
- Administrators can unlock accounts from Manage Users.
- Login failures, lockouts, blocked login attempts and unlocks are audited.

## Session information
- Settings > My Profile shows username, role, login time and session ID.

## Tests
- Added password policy tests.
- Added schema regression coverage for the new security fields.
- Schema version is now 6.
