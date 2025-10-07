using System.Runtime.InteropServices;

namespace Ion.Windows;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public class MONITORINFO
{
    /// <summary>
    /// </summary>            
    public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));

    /// <summary>
    /// </summary>            
    public RECT rcMonitor;

    /// <summary>
    /// </summary>            
    public RECT rcWork;

    /// <summary>
    /// </summary>            
    public int dwFlags;
}