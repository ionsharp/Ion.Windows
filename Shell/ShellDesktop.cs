using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Text;

namespace Ion.Windows;

public static class ShellDesktop
{
    public enum StretchMode { Centered, Stretched, Tiled }

    private const int SPI_SETDESKWALLPAPER = 20;

    private const int SPIF_UPDATEINIFILE = 0x01;

    private const int SPIF_SENDWININICHANGE = 0x02;

    /// <summary>Get the current background image of the desktop.</summary>
    public static string Background
    {
        get
        {
            var result = string.Empty;
            var key = Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop", false);
            if (key != null)
            {
                result = key.GetValue("WallPaper").ToString();
                key.Close();
            }
            return result;
        }
    }

    /// <summary>Get the current background stretch mode of the desktop.</summary>
    public static StretchMode BackgroundStretchMode
    {
        get
        {
            var Key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);

            var a = Key.GetValue(@"WallpaperStyle").ToString();
            var b = Key.GetValue(@"TileWallpaper").ToString();

            if (a == "1" && b == "0")
                return StretchMode.Centered;

            if (a == "2" && b == "0")
                return StretchMode.Stretched;

            if (a == "1" && b == "1")
                return StretchMode.Tiled;

            return StretchMode.Centered;
        }
    }

    /// <summary>Get if the desktop is active.</summary>
    public static bool IsActive()
    {
        try
        {
            var handle = GetForegroundWindow();

            const int maxChars = 256;
            var className = new StringBuilder(maxChars);
            if (GetClassName(handle.ToInt32(), className, maxChars) > 0)
            {
                var cName = className.ToString();
                if (cName == "Progman" || cName == "WorkerW")
                    return true;
            }
        }
        catch { }
        return false;
    }

    /// <summary>Set the background image of the desktop.</summary>
    public static bool SetBackground(string path, StretchMode stretchMode = StretchMode.Centered)
    {
        try
        {
            var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
            switch (stretchMode)
            {
                case StretchMode.Centered:
                    key.SetValue(@"WallpaperStyle", "1");
                    key.SetValue(@"TileWallpaper", "0");
                    break;
                case StretchMode.Stretched:
                    key.SetValue(@"WallpaperStyle", "2");
                    key.SetValue(@"TileWallpaper", "0");
                    break;
                case StretchMode.Tiled:
                    key.SetValue(@"WallpaperStyle", "1");
                    key.SetValue(@"TileWallpaper", "1");
                    break;
            }
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
        }
        catch
        {
            return false;
        }
        return true;
    }

    [DllImport("user32.dll")]
    private static extern int GetClassName(int hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
}