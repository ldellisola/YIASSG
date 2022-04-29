using System;
using System.Linq;


namespace YIASSG.Utils;

public static class Paths
{
    private static readonly char WindowsPathSeparator = '\\';
    private static readonly char UnixPathSeparator = '/';

    /// <summary>
    /// It formats a string as a path in the current platform's format.
    /// Windows uses '\' while Unix OS' uses '/'
    /// </summary>
    /// <param name="p"> string containing the path</param>
    /// <returns>A properly formatted path for the current OS</returns>
    public static string FormatAsPath(this string p)
    {
        return Environment.OSVersion.Platform == PlatformID.Win32NT
            ? p.Replace(UnixPathSeparator, WindowsPathSeparator)
            : p.Replace(WindowsPathSeparator, UnixPathSeparator);
    }

    /// <summary>
    /// It creates a path in the current platform format
    /// </summary>
    /// <param name="vs"> strings to concatenate</param>
    /// <returns>A formatted path</returns>
    public static string FormatAsPath(params string[] vs)
    {
        return System.IO.Path.Combine(vs);
    }

    /// <summary>
    /// It spits the directory and the file from a string
    /// </summary>
    /// <param name="p">path to the resource</param>
    /// <returns>It returns a pair of Directory and file</returns>
    public static (string directory, string? file) SplitDirectoryFromFile(this string p)
    {

        var path = Environment.OSVersion.Platform == PlatformID.Win32NT
            ? p.FormatAsPath().Split(WindowsPathSeparator)
            : p.FormatAsPath().Split(UnixPathSeparator);

        var file = path.LastOrDefault();

        if (path.Length == 1)
            return (string.Empty, file);

        var directory = Environment.OSVersion.Platform == PlatformID.Win32NT
            ? string.Join(WindowsPathSeparator, path.Take(path.Length - 1))
            : string.Join(UnixPathSeparator, path.Take(path.Length - 1));

        
        return (directory, file);
    }
}