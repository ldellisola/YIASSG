﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace YIASSG.Utils;

public static class Paths
{
    private static readonly char WindowsPathSeparator = '\\';
    private static readonly char UnixPathSeparator = '/';

    public static string FormatAsPath(this string p)
    {
        return Environment.OSVersion.Platform == PlatformID.Win32NT
            ? p.Replace(UnixPathSeparator, WindowsPathSeparator)
            : p.Replace(WindowsPathSeparator, UnixPathSeparator);
    }

    public static string FormatAsPath(params string[] vs)
    {
        return System.IO.Path.Combine(vs);
    }

    /// <summary>
    /// It spits the directory and the file from a string
    /// </summary>
    /// <param name="p">path to the resource</param>
    /// <returns>It returns a pair of Directory and file</returns>
    public static (string, string) SplitDirectoryFromFile(this string p)
    {
        var path = Environment.OSVersion.Platform == PlatformID.Win32NT
            ? p.FormatAsPath().Split(WindowsPathSeparator)
            : p.FormatAsPath().Split(UnixPathSeparator);

        var file = path.LastOrDefault();

        if (path.Length == 1)
            return ("", file);

        var directory = Environment.OSVersion.Platform == PlatformID.Win32NT
            ? string.Join(WindowsPathSeparator, path.Take(path.Length - 1))
            : string.Join(UnixPathSeparator, path.Take(path.Length - 1));

        return (directory, file);
    }
}