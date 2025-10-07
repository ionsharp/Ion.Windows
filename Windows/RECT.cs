using System.Runtime.InteropServices;
using System.Windows;

namespace Ion.Windows;

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public struct RECT
{
    /// <summary> Win32 </summary>
    public int left;

    /// <summary> Win32 </summary>
    public int top;

    /// <summary> Win32 </summary>
    public int right;

    /// <summary> Win32 </summary>
    public int bottom;

    /// <summary> Win32 </summary>
    public static readonly RECT Empty;

    /// <summary> Win32 </summary>
    public readonly int Width
    {
        get { return Math.Abs(right - left); }  // Abs needed for BIDI OS
    }

    /// <summary> Win32 </summary>
    public readonly int Height
    {
        get { return bottom - top; }
    }

    /// <summary> Win32 </summary>
    public RECT(int left, int top, int right, int bottom)
    {
        this.left = left;
        this.top = top;
        this.right = right;
        this.bottom = bottom;
    }

    /// <summary> Win32 </summary>
    public RECT(RECT rcSrc)
    {
        left = rcSrc.left;
        top = rcSrc.top;
        right = rcSrc.right;
        bottom = rcSrc.bottom;
    }

    /// <summary> Win32 </summary>
    public readonly bool IsEmpty
    {
        get
        {
            // BUGBUG : On Bidi OS (hebrew arabic) left > right
            return left >= right || top >= bottom;
        }
    }

    /// <summary> Return a user friendly representation of this struct </summary>
    public override readonly string ToString()
    {
        if (this == RECT.Empty) { return "RECT {Empty}"; }
        return "RECT { left : " + left + " / top : " + top + " / right : " + right + " / bottom : " + bottom + " }";
    }

    /// <summary> Determine if 2 RECT are equal (deep compare) </summary>
    public override readonly bool Equals(object obj)
    {
        if (obj is not Rect) { return false; }
        return (this == (RECT)obj);
    }

    /// <summary>Return the HashCode for this struct (not garanteed to be unique)</summary>
    public override readonly int GetHashCode()
    {
        return left.GetHashCode() + top.GetHashCode() + right.GetHashCode() + bottom.GetHashCode();
    }

    /// <summary> Determine if 2 RECT are equal (deep compare)</summary>
    public static bool operator ==(RECT rect1, RECT rect2)
    {
        return (rect1.left == rect2.left && rect1.top == rect2.top && rect1.right == rect2.right && rect1.bottom == rect2.bottom);
    }

    /// <summary> Determine if 2 RECT are different(deep compare)</summary>
    public static bool operator !=(RECT rect1, RECT rect2)
    {
        return !(rect1 == rect2);
    }
}