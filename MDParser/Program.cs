using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace MDParser
{
    static class Program
    {
        static void Main(string[] args)
        {



            string src = @"C:\Users\luckd\Desktop\test";
            string dest = @"C:\Users\luckd\Desktop\dest";



            // Copy

            DirectoryStructure.Copy(new DirectoryInfo(src), new DirectoryInfo(dest),true);


            // Process

                // This function will create the indexes of courses
                
            List<Metadata> coursesMedatada = new List<Metadata>();

            DirectoryStructure.RunInEveryDirectory(t =>
            {
                var metadataFiles = t.GetFiles( ".courseMetadata");
                if(metadataFiles.Length != 1)
                    return;

                Metadata metadata = JsonSerializer.Deserialize<Metadata>(metadataFiles.First().OpenText().ReadToEnd());
                coursesMedatada.Add(metadata);
                var mdFiles = t.GetFiles("*.md").ToList();
                if (mdFiles.Count != 0)
                {
                    string indexFile = DirectoryStructure.CreateIndex(mdFiles, metadata.CourseName);
                    File.WriteAllText(t.FullName + @"/index.md",indexFile);
                }

            },dest);

            new Pandoc().CreateIndex(coursesMedatada,dest);
                    

                // This function will process all the documents to translate latex and convert links
            DirectoryStructure.RunInAllFiles( t =>
            {
                new Pandoc().ProcessDocument(t);
            }, dest);



            // Export
            DirectoryStructure.RunInAllFiles(async t =>
            {
                await new Pandoc().ConvertDocument(t);
            }, dest);





        }
    }
}
