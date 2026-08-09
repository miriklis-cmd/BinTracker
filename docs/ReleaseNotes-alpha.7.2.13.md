# BinTracker v0.2.0-alpha.7.2.13 — Eye & Logout Rendering Fix

## Password eye
- Removed bitmap/resource rendering from the eye control.
- The eye is now drawn directly with anti-aliased WinForms graphics.
- This eliminates the missing/thin-line rendering seen on the Login screen.
- The eye remains integrated inside the password field.

## Logout
- Removed image-based Logout rendering.
- Logout is now a custom DPI-safe control that directly draws the door/arrow icon and the full `Logout` caption.
- This eliminates clipping and tiny/garbled icon rendering at laptop display scaling.

## Navigation
- Left navigation icons are unchanged in this patch.
