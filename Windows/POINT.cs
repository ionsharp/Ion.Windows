using System.Runtime.InteropServices;

namespace Ion.Windows;

/// <summary>
/// Construct a point of coordinates (x,y).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct POINT(int x, int y)
{
    /// <summary>
    /// x coordinate of point.
    /// </summary>
    public int x = x;
    /// <summary>
    /// y coordinate of point.
    /// </summary>
    public int y = y;
}