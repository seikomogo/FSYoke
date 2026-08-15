using System;
using System.Drawing;
using System.Windows.Forms;

namespace MouseYoke;

/// <summary>System tray icon: the app's only persistent UI surface, since it otherwise runs with no visible window.</summary>
public sealed class TrayIconManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;

    public event Action? SettingsRequested;
    public event Action? ExitRequested;

    public TrayIconManager()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Settings...", null, (_, _) => SettingsRequested?.Invoke());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitRequested?.Invoke());

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "MouseYoke - inactive",
            ContextMenuStrip = menu,
        };
    }

    public void SetActive(bool active)
    {
        _notifyIcon.Text = active ? "MouseYoke - ACTIVE (yoke square shown)" : "MouseYoke - inactive";
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
