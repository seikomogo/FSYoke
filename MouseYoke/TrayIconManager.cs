using System;
using System.Drawing;
using System.Reflection;
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
            Icon = LoadAppIcon(),
            Visible = true,
            Text = "MouseYoke - inactive",
            ContextMenuStrip = menu,
        };
    }

    /// <summary>Pulls the app's own icon back out of the running exe (embedded there via ApplicationIcon at compile time), so there's no separate loose .ico file to ship or lose track of.</summary>
    private static Icon LoadAppIcon()
    {
        var exePath = Assembly.GetExecutingAssembly().Location;
        if (string.IsNullOrEmpty(exePath))
        {
            // Single-file publish reports an empty Location; fall back to the actual process path.
            exePath = Environment.ProcessPath ?? string.Empty;
        }

        return Icon.ExtractAssociatedIcon(exePath) ?? SystemIcons.Application;
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
