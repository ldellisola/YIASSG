using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace MDParser.Utils
{
    public static class Paths
    {
        public static string FormatAsPath(this string p)
        {
            string path;

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                path = p.Replace(@"/",@"\");
            }
            else
            {
                path = p.Replace(@"\", @"/");
            }

            return path;
        }

        public static string FormatAsPath(params string[] vs)
        {
            return System.IO.Path.Combine(vs);
        }
    }
}
