using System;
using System.IO;


namespace MDParser
{
    class Program
    {
        static void Main(string[] args)
        {

            string src = @"C:\Users\luckd\Documents\Facultad\ITBA";
            string dest = @"C:\Users\luckd\Downloads\test";

            var p = new Pandoc();

            DirectoryStructure.Copy(new DirectoryInfo(src), new DirectoryInfo(dest));


            DirectoryStructure.RunInEveryDirectory(async t =>
            {
                await new Pandoc().ConvertDocument(t);
            }, dest);





        }
    }
}
