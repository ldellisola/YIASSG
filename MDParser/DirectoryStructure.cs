using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace MDParser
{
    public class DirectoryStructure
    {
        private static string[] excludedExtensions = {".avi"};
        public static void Copy(DirectoryInfo src, DirectoryInfo dest, bool overwriteFiles = true)
        {
            if (src.Exists)
            {

                if (!dest.Exists)
                {
                    dest.Create();
                }

                int srcLenght = src.FullName.Length;

                var files = src.GetFiles().Where(t=>!t.Attributes.HasFlag(FileAttributes.Hidden)).ToList();


                files.ForEach((t) =>
                {
                    try
                    {
                        File.Copy(t.FullName, dest.FullName + @"/" + t.Name, overwriteFiles);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("The File {0} couldn't be copied. Error MSG: {1}",t.FullName,e.Message);
                    }
                });

                var directories = src.GetDirectories().Where(t => !t.Attributes.HasFlag(FileAttributes.Hidden)).ToList();

                directories.ForEach((t) =>
                {
                    var newDest = dest.CreateSubdirectory(t.Name);
                    Copy(t,newDest, overwriteFiles);
                });


            }
        }

        public static void RunInEveryDirectory(Action<string> convert, string dir)
        {

            var files = Directory.GetFiles(dir, "*.md");
            Task[]tasks = new Task[files.Length];

            for(int i = 0; i < files.Length; i++)
            {
                var i1 = i;
                tasks[i] = (Task.Factory.StartNew(() => { convert(files[i1]); return files[i1];
                }).ContinueWith(t=>Console.WriteLine(t.Result)));
            }

            var directories = Directory.GetDirectories(dir).ToList();

            Task.WaitAll(tasks);

            directories.ForEach((t) =>
            {
                RunInEveryDirectory(convert,t);
            });

        }
    }
}
