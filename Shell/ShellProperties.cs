using System.Collections.Specialized;
using System.IO;
using System.Runtime.InteropServices;

namespace Ion.Windows;

public static class ShellProperties
{
    private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

    private const uint SHGFI_TYPENAME = 0x000000400;

    private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;

    ///

    private static MemoryStream GetShellIds(StringCollection filePaths)
    {
        //Convert list of paths into a list of PIDLs.
        var pos = 0;
        var pidls = new byte[filePaths.Count][];
        foreach (var filename in filePaths)
        {
            //Get PIDL based on name
            var pidl = ILCreateFromPath(filename);
            var pidlSize = ILGetSize(pidl);
            //Copy over to our managed array
            pidls[pos] = new byte[pidlSize];
            Marshal.Copy(pidl, pidls[pos++], 0, pidlSize);
            ILFree(pidl);
        }

        //Determine where in CIDL we will start pumping PIDLs
        var pidlOffset = 4 * (filePaths.Count + 2);
        //Start the CIDL stream
        var result = new MemoryStream();

        var writer = new BinaryWriter(result);
        writer.Write(filePaths.Count); //Initialize CIDL witha count of files
        writer.Write(pidlOffset); //Calcualte and write relative offsets of every pidl starting with root

        pidlOffset += 4; //Root is 4 bytes
        foreach (var pidl in pidls)
        {
            writer.Write(pidlOffset);
            pidlOffset += pidl.Length;
        }

        //Write the root PIDL (0) followed by all PIDLs
        writer.Write(0);
        foreach (var pidl in pidls)
            writer.Write(pidl);

        //Stream now contains the CIDL
        return result;
    }

    public static string GetDescription(string filePath)
    {
        if (IntPtr.Zero != SHGetFileInfo(filePath, FILE_ATTRIBUTE_NORMAL, out SHFILEINFO shfi, (uint)Marshal.SizeOf(typeof(SHFILEINFO)), SHGFI_USEFILEATTRIBUTES | SHGFI_TYPENAME))
            return shfi.szTypeName;

        return null;
    }

    public static int Show(string filePath) => Show([filePath]);

    public static int Show(IEnumerable<string> filePaths)
    {
        var files = new StringCollection();
        foreach (var i in filePaths)
            files.Add(i);

        var data = new System.Windows.DataObject();
        data.SetFileDropList(files);
        data.SetData("Preferred DropEffect", new MemoryStream([5, 0, 0, 0], true));
        data.SetData("Shell IDList Array", GetShellIds(files), true);

        return SHMultiFileProperties(data, 0);
    }

    ///

    [DllImport("shell32")]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, uint cbFileInfo, uint flags);

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern int SHMultiFileProperties(System.Windows.IDataObject pdtobj, int flags);

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr ILCreateFromPath(string path);

    [DllImport("shell32.dll", CharSet = CharSet.None)]
    private static extern void ILFree(IntPtr pidl);

    [DllImport("shell32.dll", CharSet = CharSet.None)]
    private static extern int ILGetSize(IntPtr pidl);
}