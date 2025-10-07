using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Ion.Windows;

public static class ShellIcon
{
    private const int WM_CLOSE = 0x0010;

    private const int SHGFI_ICON = 0x100;

    private const int SHGFI_SMALLICON = 0x1;

    private const int SHGFI_LARGEICON = 0x0;

    private const int SHIL_JUMBO = 0x4;

    private const int SHIL_EXTRALARGE = 0x2;

    private struct Pair
    {
        public Icon Icon { get; set; }

        public IntPtr HandleToDestroy { set; get; }
    }

    ///

    /*
    static ImageSource SystemIcon(bool Small, ShellApi.CSIDL csidl)
    {
        IntPtr pidlTrash = IntPtr.Zero;
        int hr = SHGetSpecialFolderLocation(IntPtr.Zero, (int)csidl, ref pidlTrash);
        System.Diagnostics.Debug.Assert(hr == 0);

        SHFILEINFO shinfo = new SHFILEINFO();

        uint SHGFI_USEFILEATTRIBUTES = 0x000000010;

        // Get a handle to the large icon
        uint flags;
        uint SHGFI_PIDL = 0x000000008;
        if (!Small)
        {
            flags = SHGFI_PIDL | SHGFI_ICON | SHGFI_LARGEICON | SHGFI_USEFILEATTRIBUTES;
        }
        else
        {
            flags = SHGFI_PIDL | SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES;
        }

        var res = SHGetFileInfo(pidlTrash, 0, ref shinfo, Marshal.SizeOf(shinfo), flags);
        System.Diagnostics.Debug.Assert(res != 0);

        var myIcon = System.Drawing.Icon.FromHandle(shinfo.hIcon);
        Marshal.FreeCoTaskMem(pidlTrash);
        var bs = myIcon.ToImageSource();
        myIcon.Dispose();
        bs.Freeze(); // importantissimo se no fa memory leak
        DestroyIcon(shinfo.hIcon);
        SendMessage(shinfo.hIcon, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        return bs;

    }
    */

    public static ImageSource Convert(Icon i)
    {
        var bitmap = i.ToBitmap();
        var hBitmap = bitmap.GetHbitmap();

        var result = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        if (!DeleteObject(hBitmap))
            throw new Win32Exception();

        return result;
    }

    private static ImageSource GetImage(string filePath, bool Small, bool checkDisk, bool addOverlay)
    {
        SHFILEINFO shinfo = new();

        uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        uint SHGFI_LINKOVERLAY = 0x000008000;

        uint flags;
        if (Small)
        {
            flags = SHGFI_ICON | SHGFI_SMALLICON;
        }
        else
        {
            flags = SHGFI_ICON | SHGFI_LARGEICON;
        }
        if (!checkDisk)
            flags |= SHGFI_USEFILEATTRIBUTES;
        if (addOverlay)
            flags |= SHGFI_LINKOVERLAY;

        var res = SHGetFileInfo(filePath, 0, ref shinfo, Marshal.SizeOf(shinfo), flags);
        if (res == 0)
        {
            throw (new System.IO.FileNotFoundException());
        }

        var myIcon = System.Drawing.Icon.FromHandle(shinfo.hIcon);

        var bs = Convert(myIcon);
        myIcon.Dispose();
        bs.Freeze(); // importantissimo se no fa memory leak
        DestroyIcon(shinfo.hIcon);
        SendMessage(shinfo.hIcon, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        return bs;
    }

    private static ImageSource GetImageLarge(string filePath, bool jumbo, bool checkDisk)
    {
        try
        {
            SHFILEINFO shinfo = new();

            uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
            uint SHGFI_SYSICONINDEX = 0x4000;

            int FILE_ATTRIBUTE_NORMAL = 0x80;

            uint flags;
            flags = SHGFI_SYSICONINDEX;

            if (!checkDisk)  // This does not seem to work. If I try it, a folder icon is always returned.
                flags |= SHGFI_USEFILEATTRIBUTES;

            var res = SHGetFileInfo(filePath, FILE_ATTRIBUTE_NORMAL, ref shinfo, Marshal.SizeOf(shinfo), flags);
            if (res == 0)
            {
                throw (new System.IO.FileNotFoundException());
            }
            var iconIndex = shinfo.iIcon;

            // Get the System IImageList object from the Shell:
            Guid iidImageList = new("46EB5926-582E-4017-9FDF-E8998DAA0950");

            int size = jumbo ? SHIL_JUMBO : SHIL_EXTRALARGE;
            var hres = SHGetImageList(size, ref iidImageList, out IImageList iml);
            // writes iml
            //if (hres == 0)
            //{
            //    throw (new System.Exception("Error SHGetImageList"));
            //}

            IntPtr hIcon = IntPtr.Zero;
            int ILD_TRANSPARENT = 1;
            hres = iml.GetIcon(iconIndex, ILD_TRANSPARENT, ref hIcon);
            //if (hres == 0)
            //{
            //    throw (new System.Exception("Error iml.GetIcon"));
            //}

            var myIcon = System.Drawing.Icon.FromHandle(hIcon);
            var bs = Convert(myIcon);
            myIcon.Dispose();
            bs.Freeze(); // very important to avoid memory leak
            DestroyIcon(hIcon);
            SendMessage(hIcon, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            return bs;
        }
        catch
        {
            return null;
        }
    }

    public static ImageSource GetLarge(string filePath) => GetImageLarge(filePath, true, true);

    /*
    public static ImageSource GetSystem(bool Small, ShellApi.CSIDL Kind)
    {
        return SystemIcon(Small, Kind);
    }
    */

    ///

    [DllImport("user32")]
    private static extern IntPtr SendMessage(IntPtr handle, int Msg, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// SHGetImageList is not exported correctly in XP.  See KB316931
    /// http://support.microsoft.com/default.aspx?scid=kb;EN-US;Q316931
    /// Apparently (and hopefully) ordinal 727 isn't going to change.
    /// </summary> 
    [DllImport("shell32.dll", EntryPoint = "#727")]
    private static extern int SHGetImageList(int iImageList, ref Guid riid, out IImageList ppv);

    /// <summary>
    /// The signature of SHGetFileInfo (located in Shell32.dll)
    /// </summary>
    [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHGetFileInfo(string pszPath, int dwFileAttributes, ref SHFILEINFO psfi, int cbFileInfo, uint uFlags);

    [DllImport("Shell32.dll")]
    private static extern int SHGetFileInfo(IntPtr pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, int cbFileInfo, uint uFlags);

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern int SHGetSpecialFolderLocation(IntPtr hwndOwner, int nFolder, ref IntPtr ppidl);

    [DllImport("user32")]
    private static extern int DestroyIcon(IntPtr hIcon);

    [DllImport("gdi32.dll")]
    internal static extern bool DeleteObject(IntPtr hObject);
}