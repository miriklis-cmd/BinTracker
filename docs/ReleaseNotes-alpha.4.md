# BinTracker v0.2.0-alpha.4

## Fixes

- Fixed Customer Statement Period dialog clipping at 150% Windows DPI scaling.
- Moved Generate PDF and Cancel into a fixed bottom action bar.
- Fixed `ThreadStateException` when opening `SaveFileDialog`.
- Changed WinForms startup from `async Task Main` to synchronous `[STAThread] void Main`.
- Startup/database/authentication async work is completed synchronously before the UI message loop begins, preserving STA/OLE requirements.
