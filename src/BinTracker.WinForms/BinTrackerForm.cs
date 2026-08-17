namespace BinTracker.WinForms;

/// <summary>
/// Common BinTracker window base.  Application branding belongs here so
/// Login, report breakouts, admin dialogs and future windows all receive the
/// same executable/title-bar/taskbar icon without per-form wiring.
/// </summary>
public abstract class BinTrackerForm : Form
{
    protected BinTrackerForm()
    {
        try
        {
            Icon =
                Icon.ExtractAssociatedIcon(Application.ExecutablePath)
                ?? SystemIcons.Application;
        }
        catch
        {
            // Icon failure must never prevent a BinTracker window from opening.
            Icon = SystemIcons.Application;
        }
    }
}
